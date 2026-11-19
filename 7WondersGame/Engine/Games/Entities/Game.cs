using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Common.Settings;
using _7WondersGame.Cli.Results;
using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Games.Dtos;
using _7WondersGame.Engine.Games.Enums;
using _7WondersGame.Engine.Games.Knowledge;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Moves.Exceptions;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Randomness.Services;
using _7WondersGame.Engine.Rules.Services;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGame.Engine.Wonders.Enums;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace _7WondersGame.Engine.Games.Entities;

/// <summary>
/// One game: the players' state, the decks and the discard pile, and the turn rules as a state machine
/// (<see cref="PendingPlayers"/>, <see cref="Submit"/>). <see cref="Loop"/> plays it to the end with the
/// <see cref="Agents"/>; a search agent plays its own <see cref="DeepCopy"/>.
/// </summary>
public class Game
{
    /// <summary>The players' cities and hands, by seat (Players[seat].Seat == seat).</summary>
    public List<PlayerState> Players { get; set; }

    /// <summary>
    /// Who decides each seat's moves, by seat. <see cref="SetUpRandomGame"/> makes one player per agent.
    /// </summary>
    public List<IAgent> Agents { get; set; } = new();
    int PlayerCount { get; set; } = 3;

    /// <summary>The era being played: 1, 2 or 3 (4 once the game is over).</summary>
    public int Era { get; set; } = 1;

    /// <summary>
    /// 0-20, <see cref="TurnsPerEra"/> per era (the last is the 7th card slot); 7 and 14 start eras 2 and 3.
    /// </summary>
    public int Turn { get; set; } = 0;

    /// <summary>Cards in each hand at the start of an era.</summary>
    public const int HandSize = 7;
    /// <summary>Six turns with a choice, then the 7th card slot (Babylon B, otherwise skipped).</summary>
    public const int TurnsPerEra = HandSize;
    /// <summary>The game has three eras, each with its own deck.</summary>
    public const int LastEra = 3;

    /// <summary>Turns 6, 13 and 20: the last card of the hand, for Babylon B.</summary>
    public static bool IsSeventhCardTurn(int turn)
    {
        int turnInEra = turn % TurnsPerEra;
        return turnInEra == TurnsPerEra - 1;
    }

    /// <summary>Turns 7, 14 and 21: an era is over.</summary>
    public static bool IsEraEnd(int turn)
    {
        return turn % TurnsPerEra == 0;
    }

    /// <summary>The cards still to be dealt, one deck per era (index 0 is era I).</summary>
    public List<Card>[] Decks { get; set; }

    /// <summary>The discard pile: discarded cards and the last card of each hand at an era's end.</summary>
    public List<Card> DiscardPile { get; set; }

    // randomness: everything random in a game comes from its seed
    /// <summary>The seed every random draw of this game comes from.</summary>
    public ulong Seed { get; private set; }
    private readonly Random _random;
    private readonly Dictionary<int, Random> _agentRandoms = new();
    private ulong _copyCount = 0;

    // what each seat knows about the hidden cards; null in a copy (see DeepCopy)
    private KnowledgeTracker? _knowledge;

    /// <summary>
    /// What the seat knows about the cards it can't see (the cards it saw passing by, its own discards...), for a
    /// search that must not use the rest. Null in a <see cref="DeepCopy"/>, which doesn't keep track.
    /// </summary>
    public SeatKnowledge? KnowledgeOf(int seat)
    {
        if (_knowledge == null)
        {
            return null;
        }
        return _knowledge.Of(seat);
    }

    /// <summary>
    /// Step-by-step trace of this game: Debug for the playout, Verbose for detail.
    /// Silent unless the runner sets it. <see cref="DeepCopy"/> never copies it, so search copies and rollouts stay
    /// silent. A trace line must not change the game: no pay-and-roll-back checks, no random draws.
    /// </summary>
    public ILogger Trace { get; set; } = Logger.None;

    private bool Tracing
    {
        get
        {
            return Trace.IsEnabled(LogEventLevel.Debug);
        }
    }

    private static string Names(IEnumerable<Card> cards)
    {
        return string.Join(", ", cards.Select(card => card.Name));
    }

    /// <summary>
    /// A game whose random draws (deal, wonders, agents' choices) all come from <paramref name="seed"/>.
    /// </summary>
    public Game(ulong seed)
    {
        Seed = seed;
        _random = Seeding.CreateRandom(Seeding.Nth(seed, Seeding.GameStream));

        Players = new();
        Decks = new List<Card>[LastEra];
        for (int deckIndex = 0; deckIndex < Decks.Length; ++deckIndex)
        {
            Decks[deckIndex] = new List<Card>();
        }
        DiscardPile = new();
    }

    /// <summary>
    /// The random stream for the player in this seat. Agents draw all their random choices from it.
    /// </summary>
    public Random AgentRandom(int seat)
    {
        if (!_agentRandoms.TryGetValue(seat, out Random? agentRandom))
        {
            agentRandom = Seeding.CreateRandom(Seeding.AgentSeed(Seed, seat));
            _agentRandoms[seat] = agentRandom;
        }
        return agentRandom;
    }

    /// <summary>
    /// Copies the game state. The copy gets its own seed (derived from this game's seed and a copy counter),
    /// so searching in copies never uses up this game's random streams. The copy doesn't keep track of what each
    /// seat knows (<see cref="KnowledgeOf"/> is null): a search copy only needs the state.
    /// </summary>
    public Game DeepCopy()
    {
        _copyCount++;
        ulong copySeed = Seeding.Nth(Seed, Seeding.CopyStreamBase + _copyCount);
        Game copy = new(copySeed);

        copy.Players = PlayerState.CloneTable(Players);
        // the same agents; a search may swap in others (for its rollouts, say) without touching this game
        copy.Agents = new List<IAgent>(Agents);
        copy.PlayerCount = this.PlayerCount;

        copy.Era = this.Era;
        copy.Turn = this.Turn;

        for (int deckIndex = 0; deckIndex < Decks.Length; deckIndex++)
        {
            copy.Decks[deckIndex] = new List<Card>(this.Decks[deckIndex]);
        }
        copy.DiscardPile = new List<Card>(this.DiscardPile);

        copy.Phase = this.Phase;
        copy._hasStarted = this._hasStarted;
        if (this._hasStarted)
        {
            copy._waiting = (bool[])this._waiting.Clone();
        }
        copy._chosen = (Move?[])this._chosen.Clone();

        return copy;
    }


    // ============ Game creation related ============
    /// <summary>
    /// Sets up a random game for <paramref name="playerCount"/> players: the players, the decks and the wonders.
    /// </summary>
    public void SetUpRandomGame(int playerCount)
    {
        CreatePlayers(playerCount);

        CreateDecks();
        GiveCards();
        GiveWonders();
    }


    /// <summary>
    /// Sets up a game from a dataset deal (the decks' card ids and the wonder ids, in order): one player per wonder.
    /// </summary>
    public void SetUpGameFromDataset(List<int>[] gameDataset)
    {
        // after the three decks comes the wonder of each seat
        List<int> wonderIds = gameDataset[LastEra];
        CreatePlayers(wonderIds.Count);

        for (int deckIndex = 0; deckIndex < Decks.Length; deckIndex++)
        {
            foreach (int cardId in gameDataset[deckIndex])
            {
                Decks[deckIndex].Add(CardsDataset.GameCards[(CardId)cardId]);
            }
        }
        GiveCards();

        for (int seat = 0; seat < Players.Count; seat++)
        {
            AssignWonder(Players[seat], wonderIds[seat]);
        }
    }

    private void CreatePlayers(int playerCount)
    {
        PlayerCount = playerCount;
        Players = new List<PlayerState>(PlayerCount);
        for (int seat = 0; seat < PlayerCount; seat++)
        {
            Players.Add(new PlayerState(seat));
        }
        SetNeighbours();
        _knowledge = new KnowledgeTracker(PlayerCount);
    }

    /// <summary>
    /// Seats the players in a circle: the west neighbour has the seat before, the east one the seat after.
    /// </summary>
    public void SetNeighbours()
    {
        for (int seat = 0; seat < PlayerCount; seat++)
        {
            int westSeat = SeatToTheWest(seat);
            int eastSeat = SeatToTheEast(seat);
            Players[seat].WestNeighbour = Players[westSeat];
            Players[seat].EastNeighbour = Players[eastSeat];
        }
    }

    /// <summary>The seat before, or the last seat for seat 0.</summary>
    private int SeatToTheWest(int seat)
    {
        if (seat == 0)
        {
            return PlayerCount - 1;
        }
        return seat - 1;
    }

    /// <summary>The seat after, or seat 0 for the last seat.</summary>
    private int SeatToTheEast(int seat)
    {
        int lastSeat = PlayerCount - 1;
        if (seat == lastSeat)
        {
            return 0;
        }
        return seat + 1;
    }

    /// <summary>
    /// Fills and shuffles the three decks: each card in the number of copies for this player count, plus
    /// players + 2 guilds drawn at random into the era III deck.
    /// </summary>
    public void CreateDecks()
    {
        foreach (KeyValuePair<CardId, Card> card in CardsDataset.Cards)
        {
            List<Card> deck = DeckOfEra(card.Value.Era);
            for (int copyIndex = 0; copyIndex < card.Value.CopiesFor(PlayerCount); copyIndex++)
            {
                deck.Add(card.Value);
            }
        }

        List<Card> guildCards = Shuffled(CardsDataset.GuildCards.Values);
        int guildsInPlay = PlayerCount + 2;
        for (int guildIndex = 0; guildIndex < guildsInPlay; guildIndex++)
        {
            DeckOfEra(LastEra).Add(guildCards[guildIndex]);
        }

        for (int deckIndex = 0; deckIndex < Decks.Length; deckIndex++)
        {
            Decks[deckIndex] = Shuffled(Decks[deckIndex]);
        }
    }

    /// <summary>A new list of the cards in random order (one draw from the game's stream per card).</summary>
    private List<Card> Shuffled(IEnumerable<Card> cards)
    {
        return cards.OrderBy(card => _random.Next()).ToList();
    }

    private List<Card> DeckOfEra(int era)
    {
        int deckIndex = era - 1;
        return Decks[deckIndex];
    }

    /// <summary>Deals each player a hand of <see cref="HandSize"/> cards from the top of this era's deck.</summary>
    private void GiveCards()
    {
        List<Card> deck = DeckOfEra(Era);
        int deckPosition = 0;

        foreach (PlayerState player in Players)
        {
            List<Card> hand = new(HandSize);
            for (int handPosition = 0; handPosition < HandSize; handPosition++)
            {
                hand.Add(deck[deckPosition]);
                deckPosition++;
            }
            player.Hand = hand;
        }
        if (_knowledge != null)
        {
            _knowledge.Dealt(Players);
        }
    }

    /// <summary>
    /// Give a player a wonder board and its starting resource. Used by both setup paths.
    /// </summary>
    private void AssignWonder(PlayerState player, int wonderId)
    {
        player.Wonder = new WonderBoardProgress((WonderId)wonderId);
        player.Resources[player.Wonder.Production] += 1;
    }

    /// <summary>Gives each player a random board, a different one each, on a random side.</summary>
    private void GiveWonders()
    {
        Random random = _random;
        bool[] available = new bool[WonderBoardProgress.BoardCount];
        for (int boardIndex = 0; boardIndex < WonderBoardProgress.BoardCount; boardIndex++)
        {
            available[boardIndex] = true;
        }

        foreach (PlayerState player in Players)
        {
            while (true)
            {
                // a board and a side
                int boardAndSide = random.Next(2 * WonderBoardProgress.BoardCount);
                int board = WonderBoardProgress.BoardOf(boardAndSide);
                if (available[board])
                {
                    AssignWonder(player, boardAndSide);
                    available[board] = false;
                    break;
                }
            }
        }
    }



    // ============ Turn state machine ============
    // The turn rules live here once. Loop and search agents all go through PendingPlayers and Submit.

    /// <summary>What the game is waiting for.</summary>
    public Phase Phase { get; private set; } = Phase.ChooseMoves;

    // whether the first phase has started (and _waiting and _chosen have one entry per seat)
    private bool _hasStarted;
    // who still has to decide in this phase
    private bool[] _waiting = Array.Empty<bool>();
    // ChooseMoves: the moves already in, held until everyone has chosen
    private Move?[] _chosen = Array.Empty<Move?>();

    /// <summary>The seats that must decide now, in seat order. Empty once the game is over.</summary>
    public IReadOnlyList<int> PendingPlayers()
    {
        StartIfNeeded();
        List<int> pending = new();
        for (int seat = 0; seat < _waiting.Length; seat++)
        {
            if (_waiting[seat])
            {
                pending.Add(seat);
            }
        }
        return pending;
    }

    /// <summary>Must the seat decide now?</summary>
    private bool IsPending(int seat)
    {
        StartIfNeeded();
        bool isSeat = seat >= 0 && seat < _waiting.Length;
        return isSeat && _waiting[seat];
    }

    /// <summary>Has anyone still to decide in this phase?</summary>
    private bool AnyoneWaiting()
    {
        foreach (bool waiting in _waiting)
        {
            if (waiting)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>True when some players' moves for this turn are in and the others' aren't yet.</summary>
    public bool SomeMovesSubmitted
    {
        get
        {
            foreach (Move? chosenMove in _chosen)
            {
                if (chosenMove != null)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// A pending player's decision. The game moves on when everyone pending has decided:
    /// <list type="bullet">
    /// <item><see cref="Phase.ChooseMoves"/>: the move is held until all moves are in, then they all happen.</item>
    /// <item><see cref="Phase.SeventhCard"/>: a normal move with the last card of the hand.</item>
    /// <item><see cref="Phase.DiscardPileBuild"/>: a Build of a discard-pile card, or Skip to build nothing.</item>
    /// </list>
    /// Throws if the seat has nothing to decide or the rules don't allow the move; the state is then unchanged.
    /// </summary>
    public void Submit(int seat, Move move)
    {
        if (!IsPending(seat))
        {
            throw new InvalidOperationException(
                $"Seat {seat} has nothing to decide now ({Phase}, turn {Turn}, era {Era}, game seed {Seed}).");
        }
        PlayerState player = Players[seat];

        switch (Phase)
        {
            case Phase.ChooseMoves:
                SubmitChosenMove(seat, move);
                break;
            case Phase.SeventhCard:
                SubmitSeventhCardMove(player, move);
                break;
            case Phase.DiscardPileBuild:
                SubmitDiscardPileBuild(player, move);
                break;
        }
    }

    /// <summary>
    /// Holds the move until every player's is in; then all of them happen at the same time and the turn ends.
    /// </summary>
    private void SubmitChosenMove(int seat, Move move)
    {
        _chosen[seat] = move;
        _waiting[seat] = false;
        if (AnyoneWaiting())
        {
            return;
        }

        try
        {
            ResolveMoves(ChosenMovesOfAllPlayers());
        }
        catch
        {
            // ResolveMoves refuses before changing anything
            _chosen[seat] = null;
            _waiting[seat] = true;
            throw;
        }
        Array.Clear(_chosen);
        Turn++;

        if (IsSeventhCardTurn(Turn) && StartPhase(Phase.SeventhCard))
        {
            return;
        }
        AfterSeventhCard();
    }

    /// <summary>Every player's held move, with the hand it is made from.</summary>
    private List<SubmittedMove> ChosenMovesOfAllPlayers()
    {
        List<SubmittedMove> moves = new(Players.Count);
        foreach (PlayerState player in Players)
        {
            Move? chosenMove = _chosen[player.Seat];
            if (chosenMove == null)
            {
                throw new InvalidOperationException($"Seat {player.Seat} has no move in yet.");
            }
            moves.Add(new SubmittedMove(player, chosenMove, player.Hand));
        }
        return moves;
    }

    /// <summary>Babylon B: the last card of the hand, played on its own.</summary>
    private void SubmitSeventhCardMove(PlayerState player, Move move)
    {
        ResolveMoves(new[] { new SubmittedMove(player, move, player.Hand) });
        _waiting[player.Seat] = false;
        if (!AnyoneWaiting())
        {
            AfterSeventhCard();
        }
    }

    /// <summary>Halikarnassos: a free build from the discard pile, or Skip to build nothing.</summary>
    private void SubmitDiscardPileBuild(PlayerState player, Move move)
    {
        Card? card = move.Card;
        if (card != null)
        {
            BuildFromDiscardPile(player, card);
        }
        else
        {
            Trace.Debug("Seat {Seat} builds nothing from the discard pile", player.Seat);
            player.HalikarnassosDiscardBuildPending = false;
        }

        _waiting[player.Seat] = false;
        if (!AnyoneWaiting())
        {
            AfterDiscardPileBuild();
        }
    }

    private void BuildFromDiscardPile(PlayerState player, Card card)
    {
        PlannedMove? planned = MovePlanning.PlanDiscardPileBuild(player, card, DiscardPile);
        if (planned == null)
        {
            throw MoveFailed(player, "build from the discard pile", card);
        }
        MoveResolution.Place(planned);
        MoveResolution.Settle(planned);
        if (_knowledge != null)
        {
            _knowledge.BuiltFromPile(card);
        }

        Trace.Debug("Seat {Seat} builds {Card} from the discard pile", player.Seat, card.Name);
    }

    private void StartIfNeeded()
    {
        if (_hasStarted)
        {
            return;
        }
        _hasStarted = true;
        _waiting = new bool[Players.Count];
        _chosen = new Move?[Players.Count];
        if (AllErasPlayed())
        {
            Phase = Phase.GameOver;
        }
        else
        {
            StartPhase(Phase);
        }
    }

    private bool AllErasPlayed()
    {
        return Era > LastEra;
    }

    /// <summary>Enters a phase and marks who must decide in it. False if nobody has anything to decide.</summary>
    private bool StartPhase(Phase phase)
    {
        Phase = phase;
        foreach (PlayerState player in Players)
        {
            _waiting[player.Seat] = MustDecideIn(phase, player);
            // Halikarnassos looks through the discard pile to choose
            if (phase == Phase.DiscardPileBuild && player.HalikarnassosDiscardBuildPending)
            {
                if (_knowledge != null)
                {
                    _knowledge.LooksAtPile(player.Seat, DiscardPile);
                }
            }
        }
        return AnyoneWaiting();
    }

    /// <summary>Has the player a decision to make in this phase?</summary>
    private static bool MustDecideIn(Phase phase, PlayerState player)
    {
        switch (phase)
        {
            case Phase.ChooseMoves:
                return true;
            case Phase.SeventhCard:
                return PlayerAbilities.Has(player, Ability.PlaySeventhCard) && player.Hand.Count > 0;
            case Phase.DiscardPileBuild:
                return player.HalikarnassosDiscardBuildPending;
            default:
                return false;
        }
    }

    /// <summary>
    /// After the turn's moves (and Babylon's 7th card): the end of an era's last hands, then Halikarnassos.
    /// </summary>
    private void AfterSeventhCard()
    {
        // the 7th card slot (6, 13, 20) is used up whether or not someone played it
        if (IsSeventhCardTurn(Turn))
        {
            Turn++;
        }

        // the last card of each hand is discarded at the end of an era (turns 7, 14 and 21)
        if (IsEraEnd(Turn))
        {
            DiscardLastCardsOfHands();
        }

        if (StartPhase(Phase.DiscardPileBuild))
        {
            return;
        }
        AfterDiscardPileBuild();
    }

    private void DiscardLastCardsOfHands()
    {
        if (_knowledge != null)
        {
            _knowledge.EraEndDiscards(Players);
        }

        if (Tracing)
        {
            IEnumerable<Card> lastCards = Players.SelectMany(player => player.Hand);
            Trace.Debug("End of era {Era}: last cards discarded: {Cards}", Era, Names(lastCards));
        }
        // the cards leave the hands, so no card is in two places during a Halikarnassos build
        foreach (PlayerState player in Players)
        {
            DiscardPile.AddRange(player.Hand);
            player.Hand = new List<Card>();
        }
    }

    /// <summary>
    /// The end of a turn: at the end of an era battles and the next era's hands, otherwise pass the hands.
    /// </summary>
    private void AfterDiscardPileBuild()
    {
        if (IsEraEnd(Turn))
        {
            RearmFreeCardsAndFightBattles();
            Era++;
            if (AllErasPlayed())
            {
                EndGame();
                return;
            }
            StartNextEra();
        }
        else
        {
            PassHands();
        }
        StartPhase(Phase.ChooseMoves);
    }

    private void RearmFreeCardsAndFightBattles()
    {
        foreach (PlayerState player in Players)
        {
            // Olympia A stage 2: one free card per era
            player.OlympiaFreeBuildAvailable = PlayerAbilities.Has(player, Ability.OlympiaFreeBuildEachEra);
            Battles.FightEraBattles(player, Era);
        }
        if (Tracing)
        {
            foreach (PlayerState player in Players)
            {
                TraceBattle(player);
            }
        }
    }

    private void EndGame()
    {
        Trace.Debug("=== Game over ===");
        Phase = Phase.GameOver;
        Array.Clear(_waiting);
    }

    private void StartNextEra()
    {
        if (Tracing)
        {
            TraceEraStart();
        }
        GiveCards();
    }

    private void PassHands()
    {
        RotateHandCards();
        string direction = "east";
        if (HandsPassWestThisEra())
        {
            direction = "west";
        }
        Trace.Debug("Hands passed {Direction}", direction);
    }

    /// <summary>Eras I and III pass hands west, era II east.</summary>
    private bool HandsPassWestThisEra()
    {
        return Era == 1 || Era == 3;
    }

    /// <summary>
    /// Every player gives their hand to the neighbour on this era's side. Goes round the table from the first
    /// player, each receiver keeping its own hand aside to give on.
    /// </summary>
    public void RotateHandCards()
    {
        if (_knowledge != null)
        {
            _knowledge.HandsPassing(Players, west: HandsPassWestThisEra());
        }

        PlayerState firstPlayer = Players[0];
        PlayerState player = firstPlayer;
        PlayerState neighbour;
        List<Card> passedHand = firstPlayer.Hand;
        List<Card> neighbourHand;
        do
        {
            neighbour = ReceiverOfHandFrom(player);
            // keep the receiver's hand to give on next
            neighbourHand = neighbour.Hand;
            neighbour.Hand = passedHand;
            player = neighbour;
            passedHand = neighbourHand;
        } while (firstPlayer != player);
    }

    private PlayerState ReceiverOfHandFrom(PlayerState player)
    {
        if (HandsPassWestThisEra())
        {
            return player.WestNeighbour;
        }
        return player.EastNeighbour;
    }

    /// <summary>
    /// Final results of the last <see cref="Loop(bool)"/> run with record = true; null otherwise.
    /// </summary>
    public List<SeatResult>? Results { get; private set; }

    /// <summary>
    /// <see cref="Results"/> of a game played with record = true. Throws if the game wasn't played that way.
    /// </summary>
    public List<SeatResult> RecordedResults()
    {
        if (Results == null)
        {
            throw new InvalidOperationException("The game has no results: play it with Loop(record: true).");
        }
        return Results;
    }

    /// <summary>
    /// Plays the game to the end and scores it.
    /// </summary>
    /// <param name="record">true for a real game (not a search rollout): fill <see cref="Results"/>.</param>
    /// <returns>Each seat's final VP.</returns>
    public Dictionary<int, int> Loop(bool record = false)
    {
        if (Agents.Count != Players.Count)
        {
            throw new InvalidOperationException($"{Players.Count} players but {Agents.Count} agents.");
        }
        if (Turn == 0 && Tracing)
        {
            TraceSetup();
        }

        IReadOnlyList<int> pending = PendingPlayers();
        while (pending.Count > 0)
        {
            AskAndSubmit(pending);
            pending = PendingPlayers();
        }

        ScoreAllPlayers();
        if (Tracing)
        {
            TraceScores();
        }
        if (record)
        {
            Results = GameResult.ForPlayers(Players, Agents);
        }

        Dictionary<int, int> scores = new();
        foreach (PlayerState player in Players)
        {
            scores.Add(player.Seat, player.VictoryPoints);
        }
        return scores;
    }

    /// <summary>
    /// Asks every pending seat's agent for its decision, then submits them all. Everyone chooses from a view of the
    /// same state before any choice is submitted, so no agent sees another's choice.
    /// </summary>
    private void AskAndSubmit(IReadOnlyList<int> pending)
    {
        List<GameView> views = pending.Select(seat => new GameView(this, seat)).ToList();
        if (Tracing)
        {
            TracePhaseStart(views);
        }

        List<Move> choices = new(views.Count);
        foreach (GameView view in views)
        {
            choices.Add(Agents[view.Seat].Choose(view));
        }
        if (Tracing && Phase != Phase.DiscardPileBuild)
        {
            for (int choiceIndex = 0; choiceIndex < views.Count; choiceIndex++)
            {
                TraceChoice(views[choiceIndex].Seat, choices[choiceIndex]);
            }
        }

        for (int choiceIndex = 0; choiceIndex < views.Count; choiceIndex++)
        {
            Submit(views[choiceIndex].Seat, choices[choiceIndex]);
        }
    }

    /// <summary>The end of the game: Olympia B's guild copy, then every player's final score.</summary>
    public void ScoreAllPlayers()
    {
        foreach (PlayerState player in Players)
        {
            // Copies a neighbor guild before scoring if the player has the ability to do so.
            bool copied = GuildCopying.CopyBestNeighbourGuild(player);
            if (copied && Tracing)
            {
                Trace.Debug("Seat {Seat} copies {Guild} from a neighbour (Olympia B)",
                    player.Seat,
                    LastBuiltCard(player).Name);
            }
            Scoring.CalculateScore(player);
        }
    }

    private static Card LastBuiltCard(PlayerState player)
    {
        int lastIndex = player.BuiltCards.Count - 1;
        return player.BuiltCards[lastIndex];
    }

    /// <summary>
    /// A chosen move that the rules don't allow. The game stops here instead of carrying on in a state
    /// that shouldn't exist; the message has what is needed to replay it.
    /// </summary>
    private MoveNotAllowedException MoveFailed(PlayerState player, string move, Card card)
    {
        return new MoveNotAllowedException(player.Seat,
            $"could not {move} {card.Name} on turn {Turn} (era {Era}, game seed {Seed}).");
    }

    private string AgentName(int seat)
    {
        if (seat < Agents.Count)
        {
            return Agents[seat].Name;
        }
        return "no agent";
    }

    /// <summary>
    /// Carries out moves made at the same time. Every move is checked and priced against the state before any
    /// of them; then every player pays and places their card; then trade income, discard coins and the effects of
    /// the cards and wonder stages arrive. Coins received can't be spent in the same turn, and seat order doesn't
    /// change the result.
    /// </summary>
    public void ResolveMoves(IReadOnlyList<SubmittedMove> moves)
    {
        TraceSnapshot[]? before = null;
        if (Tracing)
        {
            before = SnapshotsBefore(moves);
        }

        PlannedMove?[] planned = PlanAllMoves(moves);
        PlaceAllCards(planned);
        SettleAllMoves(planned);

        if (before != null)
        {
            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                TraceResult(moves[moveIndex].Player, planned[moveIndex], before[moveIndex]);
            }
        }
    }

    /// <summary>
    /// Checks and prices every move against the same state; throws, changing nothing, if one isn't allowed.
    /// </summary>
    private PlannedMove?[] PlanAllMoves(IReadOnlyList<SubmittedMove> moves)
    {
        PlannedMove?[] planned = new PlannedMove?[moves.Count];
        for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
        {
            SubmittedMove submitted = moves[moveIndex];
            PlayerState player = submitted.Player;
            Move move = submitted.Move;
            Card? card = move.Card;
            if (move.Action == MoveAction.Skip || card == null)
            {
                continue;
            }

            PlannedMove? plannedMove = MovePlanning.PlanMove(player, move.Action, card, submitted.Source);
            if (plannedMove == null)
            {
                throw MoveFailed(player, move.Action.ToString(), card);
            }
            planned[moveIndex] = plannedMove;
        }
        return planned;
    }

    /// <summary>Every player pays and places their card; a discarded card goes to the discard pile.</summary>
    private void PlaceAllCards(PlannedMove?[] planned)
    {
        foreach (PlannedMove? move in planned)
        {
            if (move == null)
            {
                continue;
            }
            MoveResolution.Place(move);
            if (move.Action == MoveAction.Discard)
            {
                DiscardPile.Add(move.Card);
            }
            if (_knowledge != null)
            {
                _knowledge.Moved(move);
            }
        }
    }

    /// <summary>Trade income, discard coins and the effects of every move arrive.</summary>
    private static void SettleAllMoves(PlannedMove?[] planned)
    {
        foreach (PlannedMove? move in planned)
        {
            if (move != null)
            {
                MoveResolution.Settle(move);
            }
        }
    }

    // ================= Trace (Debug / Verbose) =================
    // Read-only: these only read state that is already computed.

    private static TraceSnapshot[] SnapshotsBefore(
        IReadOnlyList<SubmittedMove> moves)
    {
        TraceSnapshot[] snapshots = new TraceSnapshot[moves.Count];
        for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
        {
            snapshots[moveIndex] = TraceSnapshot.Of(moves[moveIndex].Player);
        }
        return snapshots;
    }

    private void TraceSetup()
    {
        Trace.Debug("Game seed {Seed} (0x{SeedHex}), {Count} players", Seed, Seed.ToString("x16"), Players.Count);
        foreach (PlayerState player in Players)
        {
            Trace.Debug("Seat {Seat}: {Agent} {Configuration} on {Wonder}",
                player.Seat,
                AgentName(player.Seat),
                AgentConfigurationJson(player.Seat),
                player.Wonder.Id);
        }
        for (int era = 0; era < Decks.Length; era++)
        {
            Trace.Verbose("Era {Era} deck: {Cards}", era + 1, Names(Decks[era]));
        }
        Trace.Debug("=== Era 1 ===");
    }

    private string AgentConfigurationJson(int seat)
    {
        if (seat < Agents.Count)
        {
            return Agents[seat].ConfigurationJson();
        }
        return "{}";
    }

    private void TraceEraStart()
    {
        Trace.Debug("=== Era {Era} ===", Era);
        foreach (PlayerState player in Players)
        {
            if (player.OlympiaFreeBuildAvailable)
            {
                Trace.Debug("Seat {Seat} may build one card for free this era (Olympia A)", player.Seat);
            }
        }
    }

    private void TraceTurnStart(IReadOnlyList<GameView> views)
    {
        Trace.Debug("--- Era {Era}, turn {Turn} ---", Era, Turn);
        foreach (GameView view in views)
        {
            PlayerState player = Players[view.Seat];
            Trace.Debug("Seat {Seat}: {Coins} coins, {Shields} shields, {Wonder} stage {Stage}; hand: {Hand}",
                player.Seat,
                player.Resources[Resource.Coins],
                player.Resources[Resource.Shields],
                player.Wonder.Id,
                player.Wonder.BuiltStageCount,
                Names(player.Hand));
            TracePlayable(view);
        }
    }

    // what the agent was shown (worked out for its view, so tracing it changes nothing)
    private void TracePlayable(GameView view)
    {
        Trace.Verbose("Seat {Seat} can build: {Playable}; can build a wonder stage: {CanBuildWonder}",
            view.Seat,
            Names(view.BuildableCards),
            view.CanBuildWonder);
    }

    private void TracePhaseStart(IReadOnlyList<GameView> views)
    {
        if (Phase == Phase.ChooseMoves)
        {
            TraceTurnStart(views);
            return;
        }
        foreach (GameView view in views)
        {
            PlayerState player = Players[view.Seat];
            if (Phase == Phase.SeventhCard)
            {
                Trace.Debug("Seat {Seat} plays the last card of the hand (Babylon B): {Hand}",
                    player.Seat,
                    Names(player.Hand));
            }
            else
            {
                Trace.Debug("Seat {Seat} may build from the discard pile for free (Halikarnassos)", player.Seat);
                Trace.Verbose("Discard pile: {Cards}", Names(DiscardPile));
            }
            TracePlayable(view);
        }
    }

    private void TraceChoice(int seat, Move move)
    {
        string cardName = "-";
        if (move.Card != null)
        {
            cardName = move.Card.Name;
        }
        Trace.Debug("Seat {Seat} chooses {Action} {Card}", seat, move.Action, cardName);

        // e.g. a search's top moves; the agent made the note when it chose, so reading it changes nothing
        string? note = LastChoiceNoteOf(seat);
        if (note != null)
        {
            Trace.Debug("Seat {Seat} {Agent}: {Note}", seat, AgentName(seat), note);
        }
    }

    private string? LastChoiceNoteOf(int seat)
    {
        if (seat < Agents.Count)
        {
            return Agents[seat].LastChoiceNote;
        }
        return null;
    }

    /// <summary>
    /// What a move changed: coins (own, and paid to each neighbour and the bank), wonder stage and effect flags.
    /// </summary>
    private void TraceResult(PlayerState player, PlannedMove? plannedMove, TraceSnapshot before)
    {
        TraceSnapshot after = TraceSnapshot.Of(player);
        string move = DescribeMove(plannedMove, after.BuiltStageCount);

        List<string> details = new();
        if (plannedMove != null)
        {
            AddPaymentDetails(details, player, plannedMove);
        }
        List<string> gained = after.Flags.Except(before.Flags).ToList();
        List<string> lost = before.Flags.Except(after.Flags).ToList();
        if (gained.Count > 0)
        {
            details.Add("gains " + string.Join(", ", gained));
        }
        if (lost.Count > 0)
        {
            details.Add("uses up " + string.Join(", ", lost));
        }

        string detailsText = "";
        if (details.Count > 0)
        {
            detailsText = "; " + string.Join("; ", details);
        }
        Trace.Debug("Seat {Seat} {Move}: coins {Before} -> {After}{Details}",
            player.Seat,
            move,
            before.Coins,
            after.Coins,
            detailsText);
    }

    /// <summary>The move in words, for the trace: "builds Loom", "discards Loom", "skips the move"...</summary>
    private static string DescribeMove(PlannedMove? move, int stageAfter)
    {
        if (move == null)
        {
            return "skips the move";
        }

        string name = move.Card.Name;
        switch (move.Action)
        {
            case MoveAction.BuildWithOlympiaFreeBuild:
                return $"builds {name} for free (Olympia A)";
            case MoveAction.BuildWonderStage:
                return $"builds wonder stage {stageAfter} using {name}";
            case MoveAction.Discard:
                return $"discards {name}";
            default:
                return $"builds {name}";
        }
    }

    /// <summary>The coins the move paid to each neighbour and to the bank, the ones above zero.</summary>
    private static void AddPaymentDetails(List<string> details, PlayerState player, PlannedMove plannedMove)
    {
        if (plannedMove.Payment.ToWest > 0)
        {
            details.Add($"paid {plannedMove.Payment.ToWest} to seat {player.WestNeighbour.Seat} (west)");
        }
        if (plannedMove.Payment.ToEast > 0)
        {
            details.Add($"paid {plannedMove.Payment.ToEast} to seat {player.EastNeighbour.Seat} (east)");
        }
        if (plannedMove.CoinCost > 0)
        {
            details.Add($"paid {plannedMove.CoinCost} to the bank");
        }
    }

    /// <summary>
    /// One player's two battles at the end of an era (after <see cref="Battles.FightEraBattles"/>; read-only).
    /// </summary>
    private void TraceBattle(PlayerState player)
    {
        int mine = player.Resources[Resource.Shields];
        int win = Battles.VictoryTokensForWinningBattle(Era);
        int west = player.WestNeighbour.Resources[Resource.Shields];
        int east = player.EastNeighbour.Resources[Resource.Shields];
        Trace.Debug("Seat {Seat} battles with {Shields} shields: vs seat {West} (west, {WestShields}) {WestResult}, " +
                    "vs seat {East} (east, {EastShields}) {EastResult}",
            player.Seat,
            mine,
            player.WestNeighbour.Seat,
            west,
            DescribeBattleOutcome(mine, west, win),
            player.EastNeighbour.Seat,
            east,
            DescribeBattleOutcome(mine, east, win));
    }

    /// <summary>The token a battle gave, for the trace: "+3" for a win in era II, "-1" for a loss, or "draw".</summary>
    private static string DescribeBattleOutcome(int shields, int neighbourShields, int tokensForWin)
    {
        if (shields > neighbourShields)
        {
            return $"+{tokensForWin}";
        }
        if (shields < neighbourShields)
        {
            return "-1";
        }
        return "draw";
    }

    private void TraceScores()
    {
        foreach (PlayerState player in Players)
        {
            Trace.Debug("Seat {Seat} {Agent}: {Total} VP = military {Military} + coins {Money} + wonder {Wonder} + " +
                        "civilian {Civilian} + commercial {Commercial} + guilds {Guilds} + science {Science}; " +
                        "{Coins} coins left",
                player.Seat,
                AgentName(player.Seat),
                player.VictoryPoints,
                Scoring.CalculateMilitaryScore(player),
                Scoring.CalculateMoneyScore(player),
                Scoring.CalculateWonderScore(player),
                Scoring.CalculateCivilianScore(player),
                Scoring.CalculateCommercialScore(player),
                Scoring.CalculateGuildScore(player),
                Scoring.CalculateScientificScore(player),
                player.Resources[Resource.Coins]);
        }
    }


    // ================= Legal moves (for search agents) =================
    /// <summary>Every move a seat may make now. A seat with nothing to decide gets a single Skip.</summary>
    public List<Move> LegalMoves(int seat)
    {
        return Moves(seat, pruned: false);
    }

    /// <summary>
    /// Like <see cref="LegalMoves"/>, but with one wonder-stage move and one discard, each with a random hand
    /// card (from the game's own random stream), instead of one per card.
    /// </summary>
    public List<Move> LegalMovesPruned(int seat)
    {
        return Moves(seat, pruned: true);
    }

    private List<Move> Moves(int seat, bool pruned)
    {
        List<Move> moves = new();
        if (IsPending(seat))
        {
            PlayerState player = Players[seat];
            if (Phase == Phase.DiscardPileBuild)
            {
                AddDiscardPileBuilds(moves, player);
            }
            else
            {
                AddBuilds(moves, player);
                AddWonderStages(moves, player, pruned);
                AddOlympiaFreeBuilds(moves, player);
                AddDiscards(moves, player, pruned);
            }
        }

        if (moves.Count == 0)
        {
            moves.Add(new Move(MoveAction.Skip, null));
        }
        return moves;
    }

    /// <summary>Halikarnassos: a free build of any discard-pile card the city doesn't have.</summary>
    private void AddDiscardPileBuilds(List<Move> moves, PlayerState player)
    {
        foreach (Card card in BuildRules.BuildableCards(player, DiscardPile, everyCardFree: true))
        {
            AddIfNew(moves, new Move(MoveAction.Build, card));
        }
    }

    private static void AddBuilds(List<Move> moves, PlayerState player)
    {
        foreach (Card card in BuildRules.BuildableCards(player, player.Hand))
        {
            AddIfNew(moves, new Move(MoveAction.Build, card));
        }
    }

    /// <summary>The next wonder stage with each hand card, or with one random hand card when pruned.</summary>
    private void AddWonderStages(List<Move> moves, PlayerState player, bool pruned)
    {
        List<Card> hand = player.Hand;
        if (!BuildRules.CanBuildWonderStage(player) || hand.Count == 0)
        {
            return;
        }
        if (pruned)
        {
            AddIfNew(moves, new Move(MoveAction.BuildWonderStage, RandomCardOf(hand)));
            return;
        }
        foreach (Card card in hand)
        {
            AddIfNew(moves, new Move(MoveAction.BuildWonderStage, card));
        }
    }

    /// <summary>Olympia A: any hand card the city doesn't have, for free, once per era.</summary>
    private void AddOlympiaFreeBuilds(List<Move> moves, PlayerState player)
    {
        if (Phase != Phase.ChooseMoves || !player.OlympiaFreeBuildAvailable)
        {
            return;
        }
        foreach (Card card in BuildRules.BuildableCards(player, player.Hand, everyCardFree: true))
        {
            AddIfNew(moves, new Move(MoveAction.BuildWithOlympiaFreeBuild, card));
        }
    }

    /// <summary>Discarding each hand card, or one random hand card when pruned.</summary>
    private void AddDiscards(List<Move> moves, PlayerState player, bool pruned)
    {
        List<Card> hand = player.Hand;
        if (hand.Count == 0)
        {
            return;
        }
        if (pruned)
        {
            AddIfNew(moves, new Move(MoveAction.Discard, RandomCardOf(hand)));
            return;
        }
        foreach (Card card in hand)
        {
            AddIfNew(moves, new Move(MoveAction.Discard, card));
        }
    }

    private Card RandomCardOf(List<Card> hand)
    {
        int cardIndex = _random.Next(hand.Count);
        return hand[cardIndex];
    }

    /// <summary>
    /// Adds the move unless it is in the list already (the same card twice in a hand gives the same move).
    /// </summary>
    private static void AddIfNew(List<Move> moves, Move move)
    {
        if (!moves.Contains(move))
        {
            moves.Add(move);
        }
    }
}
