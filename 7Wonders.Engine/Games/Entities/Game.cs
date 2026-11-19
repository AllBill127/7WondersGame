using _7Wonders.Engine.Cards.Data;
using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Games.Dtos;
using _7Wonders.Engine.Games.Enums;
using _7Wonders.Engine.Games.Events;
using _7Wonders.Engine.Games.Interfaces;
using _7Wonders.Engine.Games.Knowledge;
using _7Wonders.Engine.Moves.Dtos;
using _7Wonders.Engine.Moves.Enums;
using _7Wonders.Engine.Moves.Exceptions;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Randomness.Services;
using _7Wonders.Engine.Rules.Services;
using _7Wonders.Engine.Wonders.Entities;
using _7Wonders.Engine.Wonders.Enums;

namespace _7Wonders.Engine.Games.Entities;

/// <summary>
/// One game: the players' state, the decks and the discard pile, and the turn rules as a state machine
/// (<see cref="PendingPlayers"/>, <see cref="Submit"/>). Whoever decides the seats' moves submits them: a match of
/// agents plays the game to the end, a search agent plays its own <see cref="DeepCopy"/>.
/// </summary>
public class Game
{
    /// <summary>The players' cities and hands, by seat (Players[seat].Seat == seat).</summary>
    public List<PlayerState> Players { get; set; }

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
    private readonly Dictionary<int, Random> _seatRandoms = new();
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
    /// Gets what the rules do in this game, event by event (e.g. a match that records the game). Null for none, and
    /// then no event is built. <see cref="DeepCopy"/> never copies it, so search copies and rollouts raise nothing.
    /// Building an event must not change the game: no pay-and-roll-back checks, no random draws.
    /// </summary>
    public IGameEventListener? EventListener { get; set; }

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
    /// The random stream for the player in this seat, derived from the game's seed. Whoever decides the seat's
    /// moves draws all its random choices from it.
    /// </summary>
    public Random SeatRandom(int seat)
    {
        if (!_seatRandoms.TryGetValue(seat, out Random? seatRandom))
        {
            seatRandom = Seeding.CreateRandom(Seeding.AgentSeed(Seed, seat));
            _seatRandoms[seat] = seatRandom;
        }
        return seatRandom;
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
    // The turn rules live here once. Matches and search agents all go through PendingPlayers and Submit.

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
            if (EventListener != null)
            {
                EventListener.EventRaised(new DiscardPileBuildSkipped(player.Seat));
            }
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

        if (EventListener != null)
        {
            EventListener.EventRaised(new DiscardPileBuilt(player.Seat, card));
        }
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

        if (EventListener != null)
        {
            RaiseEraEnded(EventListener);
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
        if (EventListener != null)
        {
            foreach (PlayerState player in Players)
            {
                RaiseBattleFought(EventListener, player);
            }
        }
    }

    private void EndGame()
    {
        if (EventListener != null)
        {
            EventListener.EventRaised(new GameEnded());
        }
        Phase = Phase.GameOver;
        Array.Clear(_waiting);
    }

    private void StartNextEra()
    {
        if (EventListener != null)
        {
            RaiseEraStarted(EventListener);
        }
        GiveCards();
    }

    private void PassHands()
    {
        RotateHandCards();
        if (EventListener != null)
        {
            EventListener.EventRaised(new HandsPassed(HandsPassWestThisEra()));
        }
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

    /// <summary>The end of the game: Olympia B's guild copy, then every player's final score.</summary>
    public void ScoreAllPlayers()
    {
        foreach (PlayerState player in Players)
        {
            // Copies a neighbor guild before scoring if the player has the ability to do so.
            bool copied = GuildCopying.CopyBestNeighbourGuild(player);
            if (copied && EventListener != null)
            {
                EventListener.EventRaised(new GuildCopied(player.Seat, LastBuiltCard(player)));
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

    /// <summary>
    /// Carries out moves made at the same time. Every move is checked and priced against the state before any
    /// of them; then every player pays and places their card; then trade income, discard coins and the effects of
    /// the cards and wonder stages arrive. Coins received can't be spent in the same turn, and seat order doesn't
    /// change the result.
    /// </summary>
    internal void ResolveMoves(IReadOnlyList<SubmittedMove> moves)
    {
        PlayerSnapshot[]? before = null;
        if (EventListener != null)
        {
            before = SnapshotsBefore(moves);
        }

        PlannedMove?[] planned = PlanAllMoves(moves);
        PlaceAllCards(planned);
        SettleAllMoves(planned);

        if (EventListener != null && before != null)
        {
            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                RaiseMoveResolved(EventListener, moves[moveIndex].Player, planned[moveIndex], before[moveIndex]);
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

    // ================= Events (only when a listener is set) =================
    // Read-only: these only read state that is already computed.

    private static PlayerSnapshot[] SnapshotsBefore(IReadOnlyList<SubmittedMove> moves)
    {
        PlayerSnapshot[] snapshots = new PlayerSnapshot[moves.Count];
        for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
        {
            snapshots[moveIndex] = PlayerSnapshot.Of(moves[moveIndex].Player);
        }
        return snapshots;
    }

    private void RaiseEraStarted(IGameEventListener listener)
    {
        List<int> olympiaFreeBuildSeats = new();
        foreach (PlayerState player in Players)
        {
            if (player.OlympiaFreeBuildAvailable)
            {
                olympiaFreeBuildSeats.Add(player.Seat);
            }
        }
        listener.EventRaised(new EraStarted(Era, olympiaFreeBuildSeats));
    }

    private void RaiseEraEnded(IGameEventListener listener)
    {
        List<Card> lastCards = new();
        foreach (PlayerState player in Players)
        {
            lastCards.AddRange(player.Hand);
        }
        listener.EventRaised(new EraEnded(Era, lastCards));
    }

    /// <summary>
    /// What a move changed: coins (own, and paid to each neighbour and the bank), wonder stage and effect flags.
    /// </summary>
    private static void RaiseMoveResolved(IGameEventListener listener, PlayerState player, PlannedMove? plannedMove,
                                          PlayerSnapshot before)
    {
        PlayerSnapshot after = PlayerSnapshot.Of(player);
        List<string> gained = after.Flags.Except(before.Flags).ToList();
        List<string> usedUp = before.Flags.Except(after.Flags).ToList();

        // a skipped move has no card and pays nothing
        MoveAction action = MoveAction.Skip;
        Card? card = null;
        int paidToWest = 0;
        int paidToEast = 0;
        int paidToBank = 0;
        if (plannedMove != null)
        {
            action = plannedMove.Action;
            card = plannedMove.Card;
            paidToWest = plannedMove.Payment.ToWest;
            paidToEast = plannedMove.Payment.ToEast;
            paidToBank = plannedMove.CoinCost;
        }

        listener.EventRaised(new MoveResolved(player.Seat,
            action,
            card,
            after.BuiltStageCount,
            before.Coins,
            after.Coins,
            player.WestNeighbour.Seat,
            paidToWest,
            player.EastNeighbour.Seat,
            paidToEast,
            paidToBank,
            gained,
            usedUp));
    }

    /// <summary>
    /// One player's two battles at the end of an era (after <see cref="Battles.FightEraBattles"/>; read-only).
    /// </summary>
    private void RaiseBattleFought(IGameEventListener listener, PlayerState player)
    {
        listener.EventRaised(new BattleFought(player.Seat,
            player.Resources[Resource.Shields],
            player.WestNeighbour.Seat,
            player.WestNeighbour.Resources[Resource.Shields],
            player.EastNeighbour.Seat,
            player.EastNeighbour.Resources[Resource.Shields],
            Battles.VictoryTokensForWinningBattle(Era)));
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
