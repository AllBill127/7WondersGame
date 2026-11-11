using _7WondersGame.src.models.Agents;
using _7WondersGame.src.models.Effects;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace _7WondersGame.src.models
{
    public class Game
    {
        /// <summary>The players' cities and hands, by seat (Players[i].Id == i).</summary>
        public List<PlayerState> Players { get; set; }

        /// <summary>Who decides each seat's moves, by seat. <see cref="InitGame"/> makes one player per agent.</summary>
        public List<IAgent> Agents { get; set; } = new();
        int NumPlayers { get; set; } = 3;
        public int Era { get; set; } = 1;
        // 0, 7 , 14, 21 are new era turns (0 being the start of the game)
        public int Turn { get; set; } = 0;
        public List<Card>[] Decks { get; set; } 
        public List<Card> DiscardedCards { get; set; }

        // randomness (B1): everything random in a game comes from its seed
        public ulong Seed { get; private set; }
        private readonly Random _rng;
        private readonly Dictionary<int, Random> _agentRngs = new();
        private ulong _copyCount = 0;

        /// <summary>
        /// Step-by-step trace of this game: Debug for the playout, Verbose for detail.
        /// Silent unless the runner sets it. <see cref="DeepCopy"/> never copies it, so search copies and rollouts stay silent.
        /// A trace line must not change the game: no pay-and-roll-back checks, no random draws.
        /// </summary>
        public ILogger Trace { get; set; } = Logger.None;

        private bool Tracing => Trace.IsEnabled(LogEventLevel.Debug);

        private static string Names(IEnumerable<Card> cards) => string.Join(", ", cards.Select(c => c.Name));

        /// <summary>
        /// A game with seed 0. Use <see cref="Game(ulong)"/> when the game should differ from others.
        /// </summary>
        public Game() : this(0) { }

        public Game(ulong seed)
        {
            Seed = seed;
            _rng = Seeding.CreateRandom(Seeding.Nth(seed, Seeding.GameStream));

            Players = new();
            Decks = new List<Card>[3];
            for (int i = 0; i < Decks.Length; ++i)
                Decks[i] = new List<Card>();
            DiscardedCards = new();
        }

        /// <summary>
        /// The random stream for the player in this seat. Agents draw all their random choices from it.
        /// </summary>
        public Random AgentRandom(int seat)
        {
            if (!_agentRngs.TryGetValue(seat, out Random? rng))
            {
                rng = Seeding.CreateRandom(Seeding.AgentSeed(Seed, seat));
                _agentRngs[seat] = rng;
            }
            return rng;
        }

        /// <summary>
        /// Copies the game state. The copy gets its own seed (derived from this game's seed and a copy counter),
        /// so searching in copies never uses up this game's random streams.
        /// </summary>
        public Game DeepCopy()
        {
            Game copy = new Game(Seeding.Nth(Seed, Seeding.CopyStreamBase + ++_copyCount));

            copy.Players = PlayerState.CloneTable(Players);
            // the same agents; a search may swap in others (for its rollouts, say) without touching this game
            copy.Agents = Agents.ToList();
            copy.NumPlayers = this.NumPlayers;

            copy.Era = this.Era;
            copy.Turn = this.Turn;

            // copy decks
            copy.Decks[0] = this.Decks[0].Select(e => e).ToList();
            copy.Decks[1] = this.Decks[1].Select(e => e).ToList();
            copy.Decks[2] = this.Decks[2].Select(e => e).ToList();

            copy.DiscardedCards = this.DiscardedCards.Select(e => e).ToList();

            copy.Phase = this.Phase;
            copy._waiting = this._waiting?.ToArray();
            copy._chosen = this._chosen.ToArray();

            return copy;
        }


        // ============ Game creation related ============
        /// <summary>
        /// Sets up a random game: one player per agent in <see cref="Agents"/>, the decks and the wonders.
        /// </summary>
        public void InitGame()
        {
            CreatePlayers();

            CreateDecks();
            GiveCards();
            GiveWonders();
        }


        /// <summary>
        /// Sets up a game from a dataset deal (the decks' card ids and the wonder ids, in order): one player per agent
        /// in <see cref="Agents"/>.
        /// </summary>
        public void InitGameNotRandom(List<int>[] gameDataset)
        {
            CreatePlayers();

            for (int i = 0; i < 3; i++)
            {
                foreach (var cardId in gameDataset[i])
                {
                    Decks[i].Add(CardsDataset.GameCards[(CardId)cardId]);
                }
            }
            GiveCards();

            for (int i = 0; i < Players.Count; i++)
            {
                AssignWonder(Players[i], gameDataset[3][i]);
            }
        }

        private void CreatePlayers()
        {
            if (Agents.Count == 0)
                throw new InvalidOperationException("Add the agents (Game.Agents) before setting up the game.");
            NumPlayers = Agents.Count;
            Players = Enumerable.Range(0, NumPlayers).Select(seat => new PlayerState(seat)).ToList();
            SetPlayerNeighbors();
        }

        public void SetPlayerNeighbors()
        {
            // set player neighbors
            for (int i = 0; i < NumPlayers; i++)
            {
                if (i == 0)
                {
                    Players[i].PlayerWest = Players[NumPlayers - 1];
                    Players[i].PlayerEast = Players[i + 1];
                }
                else if (i == NumPlayers - 1)
                {
                    Players[i].PlayerWest = Players[i - 1];
                    Players[i].PlayerEast = Players[0];
                }
                else
                {
                    Players[i].PlayerWest = Players[i - 1];
                    Players[i].PlayerEast = Players[i + 1];
                }
            }
        }

        public void CreateDecks()
        {
            Random rng = _rng;

            // Add cards to decks
            foreach (var card in CardsDataset.Cards)
            {
                for (int i = 0; i < card.Value.CopiesFor(NumPlayers); i++)
                {
                    Decks[card.Value.Era - 1].Add(card.Value);
                }
            }

            // Add guild cards to Era 3 deck
            var guildCardsDict = CardsDataset.GuildCards;
            var guildCards = guildCardsDict.Values.OrderBy(a => rng.Next()).ToList();

            for (int i = 0; i < NumPlayers + 2; i++)
            {
                Decks[2].Add(guildCards[i]);
            }

            // Shuffle decks
            Decks[0] = Decks[0].OrderBy(a => rng.Next()).ToList();
            Decks[1] = Decks[1].OrderBy(a => rng.Next()).ToList();
            Decks[2] = Decks[2].OrderBy(a => rng.Next()).ToList();
        }

        private void GiveCards()
        {
            List<Card> cards = new List<Card>();
            int cardIdx = 0;

            foreach (PlayerState player in Players)
            {
                cards.Clear();
                for (int i = 0; i < 7; i++)
                {
                    cards.Add(Decks[Era - 1][cardIdx++]);
                }

                player.HandCards = cards.Select(e => e).ToList();
            }
        }

        /// <summary>
        /// Give a player a wonder board and its starting resource. Used by both setup paths.
        /// </summary>
        private void AssignWonder(PlayerState player, int wonderId)
        {
            player.Board = new Wonder((WonderId)wonderId);
            player.Resources[player.Board.Production] += 1;
        }

        private void GiveWonders()
        {
            Random rng = _rng;
            bool[] wonder_availability = new bool[7];
            for (int i = 0; i < 7; i++)
                wonder_availability[i] = true;

            foreach (PlayerState player in Players)
            {
                while (true)
                {
                    int n = rng.Next(14);
                    if (wonder_availability[n % 7])
                    {
                        AssignWonder(player, n);
                        wonder_availability[n % 7] = false;
                        break;
                    }
                }
            }
        }



        // ============ Turn state machine (E1) ============
        // The turn rules live here once. Loop and search agents all go through PendingPlayers and Submit.

        /// <summary>What the game is waiting for.</summary>
        public Phase Phase { get; private set; } = Phase.ChooseMoves;

        // who still has to decide in this phase; null until the first phase starts
        private bool[]? _waiting;
        // ChooseMoves: the moves already in, held until everyone has chosen
        private Command?[] _chosen = Array.Empty<Command?>();

        /// <summary>The seats that must decide now, in seat order. Empty once the game is over.</summary>
        public IReadOnlyList<int> PendingPlayers()
        {
            StartIfNeeded();
            var pending = new List<int>();
            for (int seat = 0; seat < _waiting!.Length; seat++)
                if (_waiting[seat])
                    pending.Add(seat);
            return pending;
        }

        /// <summary>True when some players' moves for this turn are in and the others' aren't yet.</summary>
        public bool MovesWaiting => _chosen.Any(c => c != null);

        /// <summary>
        /// A pending player's decision. The game moves on when everyone pending has decided:
        /// <list type="bullet">
        /// <item><see cref="Phase.ChooseMoves"/>: the move is held until all moves are in, then they happen together.</item>
        /// <item><see cref="Phase.SeventhCard"/>: a normal move with the last card of the hand.</item>
        /// <item><see cref="Phase.DiscardBuild"/>: a Build of a discard-pile card, or Skip to build nothing.</item>
        /// </list>
        /// Throws if the seat has nothing to decide or the rules don't allow the move; the state is then unchanged.
        /// </summary>
        public void Submit(int seat, Command move)
        {
            StartIfNeeded();
            if (seat < 0 || seat >= _waiting!.Length || !_waiting[seat])
                throw new InvalidOperationException(
                    $"Seat {seat} has nothing to decide now ({Phase}, turn {Turn}, era {Era}, game seed {Seed}).");
            PlayerState player = Players[seat];

            switch (Phase)
            {
                case Phase.ChooseMoves:
                    _chosen[seat] = move;
                    _waiting[seat] = false;
                    if (_waiting.Contains(true))
                        return;
                    try
                    {
                        // every player's move happens at the same time (R5)
                        ResolveMoves(Players.Select(p => (p, _chosen[p.Id]!, p.HandCards)).ToList());
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
                    if (Turn % 7 == 6 && StartPhase(Phase.SeventhCard))
                        return;
                    AfterSeventhCard();
                    break;

                case Phase.SeventhCard:
                    HandlePlayerCommand(player, move, player.HandCards);
                    _waiting[seat] = false;
                    if (!_waiting.Contains(true))
                        AfterSeventhCard();
                    break;

                case Phase.DiscardBuild:
                    if (move.Card is { } card)
                    {
                        if (!player.BuildDiscardFree(card, DiscardedCards))
                            throw MoveFailed(player, "build from the discard pile", card);
                        Trace.Debug("Seat {Seat} builds {Card} from the discard pile", seat, card.Name);
                    }
                    else
                    {
                        Trace.Debug("Seat {Seat} builds nothing from the discard pile", seat);
                        player.DiscardFree = false;
                    }
                    _waiting[seat] = false;
                    if (!_waiting.Contains(true))
                        AfterDiscardBuild();
                    break;
            }
        }

        private void StartIfNeeded()
        {
            if (_waiting != null)
                return;
            _waiting = new bool[Players.Count];
            _chosen = new Command?[Players.Count];
            if (Era >= 4)
                Phase = Phase.GameOver;
            else
                StartPhase(Phase);
        }

        /// <summary>Enters a phase and marks who must decide in it. False if nobody has anything to decide.</summary>
        private bool StartPhase(Phase phase)
        {
            Phase = phase;
            foreach (PlayerState p in Players)
            {
                _waiting![p.Id] = phase switch
                {
                    Phase.ChooseMoves => true,
                    Phase.SeventhCard => p.Has(Ability.PlaySeventh) && p.HandCards.Count > 0,
                    Phase.DiscardBuild => p.DiscardFree,
                    _ => false,
                };
            }
            return _waiting!.Contains(true);
        }

        /// <summary>After the turn's moves (and Babylon's 7th card): the end of an era's last hands, then Halikarnassos.</summary>
        private void AfterSeventhCard()
        {
            // the 7th card slot (6, 13, 20) is used up whether or not someone played it
            if (Turn % 7 == 6)
                Turn++;

            // the last card of each hand is discarded at the end of an era (turns 7, 14 and 21)
            if (Turn % 7 == 0)
            {
                foreach (PlayerState player in Players)
                    DiscardedCards.AddRange(player.HandCards);
                if (Tracing)
                    Trace.Debug("End of era {Era}: last cards discarded: {Cards}", Era, Names(Players.SelectMany(p => p.HandCards)));
            }

            if (StartPhase(Phase.DiscardBuild))
                return;
            AfterDiscardBuild();
        }

        /// <summary>The end of a turn: at the end of an era battles and the next era's hands, otherwise pass the hands.</summary>
        private void AfterDiscardBuild()
        {
            if (Turn % 7 == 0)
            {
                foreach (PlayerState player in Players)
                {
                    // Olympia A stage 2: one free card per era
                    player.FreeCardOnce = player.Has(Ability.FreeCardEachEra);
                    player.Battle(Era);
                }
                if (Tracing)
                    foreach (PlayerState player in Players)
                        TraceBattle(player);

                Era++;
                if (Era >= 4)
                {
                    Trace.Debug("=== Game over ===");
                    Phase = Phase.GameOver;
                    Array.Clear(_waiting!);
                    return;
                }
                if (Tracing)
                {
                    Trace.Debug("=== Era {Era} ===", Era);
                    foreach (PlayerState player in Players.Where(p => p.FreeCardOnce))
                        Trace.Debug("Seat {Seat} may build one card for free this era (Olympia A)", player.Id);
                }
                GiveCards();
            }
            else
            {
                RotateHandCards();
                Trace.Debug("Hands passed {Direction}", Era == 1 || Era == 3 ? "west" : "east");
            }
            StartPhase(Phase.ChooseMoves);
        }

        public void RotateHandCards()
        {
            PlayerState p,
                p1,
                neighbor;
            p1 = p = Players.First();
            List<Card> neighbor_deck,
                player_deck = p1.HandCards;

            bool clockwise = (Era == 1 || Era == 3);
            do
            {
                // Get the neighbor of player p who will receive the cards
                // Keep his deck from being overwritten and lost
                // The neighbor receives the cards from player p
                // The neighbor becomes the player p
                // Continue until you reach the first player again
                neighbor = clockwise ? p.PlayerWest : p.PlayerEast;
                // backup neighbor card list
                neighbor_deck = neighbor.HandCards;
                // change card list references
                neighbor.HandCards = player_deck;
                // go to next player
                p = neighbor;
                // set new list reference that is the neighbors hand card list copy
                player_deck = neighbor_deck;
            } while (p1 != p);
        }

        /// <summary>
        /// Final results of the last <see cref="Loop(bool)"/> run with record = true; null otherwise.
        /// </summary>
        public List<SeatResult>? Results { get; private set; }

        /// <summary>
        /// Plays the game to the end and scores it.
        /// </summary>
        /// <param name="record">true for a real game (not a search rollout): fill <see cref="Results"/>.</param>
        /// <returns>Each seat's final VP.</returns>
        public Dictionary<int, int> Loop(bool record = false)
        {
            if (Agents.Count != Players.Count)
                throw new InvalidOperationException($"{Players.Count} players but {Agents.Count} agents.");
            if (Turn == 0 && Tracing)
                TraceSetup();

            while (PendingPlayers() is { Count: > 0 } pending)
            {
                // Everyone chooses from a view of the same state before any choice is submitted, so no agent sees
                // another's choice (M2).
                List<GameView> views = pending.Select(seat => new GameView(this, seat)).ToList();
                if (Tracing)
                    TracePhaseStart(views);

                var choices = views.Select(v => (v.Seat, Move: Agents[v.Seat].Choose(v))).ToList();
                if (Tracing && Phase != Phase.DiscardBuild)
                    foreach (var (seat, move) in choices)
                        TraceChoice(seat, move);

                foreach (var (seat, move) in choices)
                    Submit(seat, move);
            }

            //calculate scores
            foreach (PlayerState player in Players)
            {
                // Copies a neighbor guild before scoring if the player has the ability to do so.
                bool copied = player.CopyGuild();
                if (copied && Tracing)
                    Trace.Debug("Seat {Seat} copies {Guild} from a neighbour (Olympia B)", player.Id, player.PlayedCards[^1].Name);
                player.CalculateScore();
            }
            if (Tracing)
                TraceScores();

            if (record)
                Results = GameResult.ForPlayers(Players, Agents);

            var scores = new Dictionary<int, int>();
            Players.ForEach(p => scores.Add(p.Id, p.VictoryPoints));
            return scores;
        }

        /// <summary>
        /// A chosen move that the rules don't allow (E8). The game stops here instead of carrying on in a state
        /// that shouldn't exist; the message has what is needed to replay it.
        /// </summary>
        private InvalidOperationException MoveFailed(PlayerState player, string move, Card card) =>
            new($"Seat {player.Id} ({AgentName(player.Id)}) could not {move} {card.Name} on turn {Turn} " +
                $"(era {Era}, game seed {Seed}).");

        private string AgentName(int seat) => seat < Agents.Count ? Agents[seat].Name : "no agent";

        /// <summary>One player's move on its own (Babylon's 7th card).</summary>
        public void HandlePlayerCommand(PlayerState player, Command playerCommand, List<Card> cardsList) =>
            ResolveMoves(new[] { (player, playerCommand, cardsList) });

        /// <summary>
        /// Carries out moves made at the same time (R5). Every move is checked and priced against the state before any
        /// of them; then every player pays and places their card; then trade income, discard coins and the effects of
        /// the cards and wonder stages arrive. Coins received can't be spent in the same turn, and seat order doesn't
        /// change the result.
        /// </summary>
        public void ResolveMoves(IReadOnlyList<(PlayerState Player, Command Command, List<Card> Source)> moves)
        {
            TraceSnapshot[]? before = Tracing ? moves.Select(m => TraceSnapshot.Of(m.Player)).ToArray() : null;

            var planned = new PlannedMove?[moves.Count];
            for (int i = 0; i < moves.Count; i++)
            {
                var (player, command, source) = moves[i];
                if (command.Action != ActionType.Skip && command.Card is { } card)
                    planned[i] = player.PlanMove(command.Action, card, source) ?? throw MoveFailed(player, command.Action.ToString(), card);
            }

            foreach (PlannedMove? move in planned)
            {
                if (move == null)
                    continue;
                move.Player.Place(move);
                if (move.Action == ActionType.Discard)
                    DiscardedCards.Add(move.Card);
            }

            foreach (PlannedMove? move in planned)
                if (move != null)
                    move.Player.Settle(move);

            if (before != null)
                for (int i = 0; i < moves.Count; i++)
                    TraceResult(moves[i].Player, planned[i], before[i]);
        }

        // ================= Trace (Debug / Verbose) =================
        // Read-only: these only read state that is already computed.

        private void TraceSetup()
        {
            Trace.Debug("Game seed {Seed} (0x{SeedHex}), {Count} players", Seed, Seed.ToString("x16"), Players.Count);
            foreach (PlayerState p in Players)
                Trace.Debug("Seat {Seat}: {Agent} {Config} on {Wonder}", p.Id, AgentName(p.Id),
                    p.Id < Agents.Count ? Agents[p.Id].ConfigJson() : "{}", p.Board.Id);
            for (int era = 0; era < Decks.Length; era++)
                Trace.Verbose("Era {Era} deck: {Cards}", era + 1, Names(Decks[era]));
            Trace.Debug("=== Era 1 ===");
        }

        private void TraceTurnStart(IReadOnlyList<GameView> views)
        {
            Trace.Debug("--- Era {Era}, turn {Turn} ---", Era, Turn);
            foreach (GameView v in views)
            {
                PlayerState p = Players[v.Seat];
                Trace.Debug("Seat {Seat}: {Coins} coins, {Shields} shields, {Wonder} stage {Stage}; hand: {Hand}",
                    p.Id, p.Resources[Resource.Coins], p.Resources[Resource.Shields], p.Board.Id, p.Board.Stage, Names(p.HandCards));
                TracePlayable(v);
            }
        }

        // what the agent was shown (worked out for its view, so tracing it changes nothing)
        private void TracePlayable(GameView v) =>
            Trace.Verbose("Seat {Seat} can build: {Playable}; can build a wonder stage: {CanBuildWonder}",
                v.Seat, Names(v.PlayableCards), v.CanBuildWonder);

        private void TracePhaseStart(IReadOnlyList<GameView> views)
        {
            if (Phase == Phase.ChooseMoves)
            {
                TraceTurnStart(views);
                return;
            }
            foreach (GameView v in views)
            {
                PlayerState p = Players[v.Seat];
                if (Phase == Phase.SeventhCard)
                    Trace.Debug("Seat {Seat} plays the last card of the hand (Babylon B): {Hand}", p.Id, Names(p.HandCards));
                else
                {
                    Trace.Debug("Seat {Seat} may build from the discard pile for free (Halikarnassos)", p.Id);
                    Trace.Verbose("Discard pile: {Cards}", Names(DiscardedCards));
                }
                TracePlayable(v);
            }
        }

        private void TraceChoice(int seat, Command c)
        {
            Trace.Debug("Seat {Seat} chooses {Action} {Card}", seat, c.Action, c.Card?.Name ?? "-");
        }

        /// <summary>What a move changed: coins (own, and paid to each neighbour and the bank), wonder stage and effect flags.</summary>
        private void TraceResult(PlayerState p, PlannedMove? m, TraceSnapshot before)
        {
            TraceSnapshot after = TraceSnapshot.Of(p);
            string name = m?.Card.Name ?? "";
            string move = m == null ? "skips the move" : m.Action switch
            {
                ActionType.BuildFreeOnce => $"builds {name} for free (Olympia A)",
                ActionType.BuildWonder => $"builds wonder stage {after.Stage} using {name}",
                ActionType.Discard => $"discards {name}",
                _ => $"builds {name}",
            };

            var details = new List<string>();
            if (m != null)
            {
                if (m.Payment.ToWest > 0)
                    details.Add($"paid {m.Payment.ToWest} to seat {p.PlayerWest.Id} (west)");
                if (m.Payment.ToEast > 0)
                    details.Add($"paid {m.Payment.ToEast} to seat {p.PlayerEast.Id} (east)");
                if (m.CoinCost > 0)
                    details.Add($"paid {m.CoinCost} to the bank");
            }
            var gained = after.Flags.Except(before.Flags).ToList();
            var lost = before.Flags.Except(after.Flags).ToList();
            if (gained.Count > 0)
                details.Add("gains " + string.Join(", ", gained));
            if (lost.Count > 0)
                details.Add("uses up " + string.Join(", ", lost));

            Trace.Debug("Seat {Seat} {Move}: coins {Before} -> {After}{Details}", p.Id, move, before.Coins, after.Coins,
                details.Count > 0 ? "; " + string.Join("; ", details) : "");
        }

        /// <summary>One player's two battles at the end of an era (after <see cref="Rules.Battle"/>; read-only).</summary>
        private void TraceBattle(PlayerState p)
        {
            int mine = p.Resources[Resource.Shields];
            int win = Era == 1 ? 1 : Era == 2 ? 3 : 5;
            string Outcome(int theirs) => mine > theirs ? $"+{win}" : mine < theirs ? "-1" : "draw";
            int west = p.PlayerWest.Resources[Resource.Shields], east = p.PlayerEast.Resources[Resource.Shields];
            Trace.Debug("Seat {Seat} battles with {Shields} shields: vs seat {West} (west, {WestShields}) {WestResult}, " +
                        "vs seat {East} (east, {EastShields}) {EastResult}",
                p.Id, mine, p.PlayerWest.Id, west, Outcome(west), p.PlayerEast.Id, east, Outcome(east));
        }

        private void TraceScores()
        {
            foreach (PlayerState p in Players)
                Trace.Debug("Seat {Seat} {Agent}: {Total} VP = military {Military} + coins {Money} + wonder {Wonder} + " +
                            "civilian {Civilian} + commercial {Commercial} + guilds {Guilds} + science {Science}; {Coins} coins left",
                    p.Id, AgentName(p.Id), p.VictoryPoints, p.CalculateMilitaryScore(), p.CalculateMoneyScore(),
                    p.CalculateWonderScore(), p.CalculateCivilianScore(), p.CalculateCommercialScore(),
                    p.CalculateGuildScore(), p.CalculateScientificScore(), p.Resources[Resource.Coins]);
        }

        private sealed record TraceSnapshot(int Coins, int Stage, List<string> Flags)
        {
            public static TraceSnapshot Of(PlayerState p)
            {
                var flags = Enum.GetValues<Ability>().Where(a => a != Ability.None && p.Has(a)).Select(a => a.ToString()).ToList();
                if (p.DiscardFree) flags.Add(nameof(p.DiscardFree));
                if (p.FreeCardOnce) flags.Add(nameof(p.FreeCardOnce));
                return new TraceSnapshot(p.Resources[Resource.Coins], p.Board.Stage, flags);
            }
        }


        // ================= Legal moves (for search agents) =================
        /// <summary>Every move a seat may make now. A seat with nothing to decide gets a single Skip.</summary>
        public List<Command> LegalMoves(int seat) => Moves(seat, pruned: false);

        /// <summary>
        /// Like <see cref="LegalMoves"/>, but with one wonder-stage move and one discard, each with a random hand
        /// card (from the game's own random stream), instead of one per card.
        /// </summary>
        public List<Command> LegalMovesPruned(int seat) => Moves(seat, pruned: true);

        private List<Command> Moves(int seat, bool pruned)
        {
            var moves = new List<Command>();
            if (PendingPlayers().Contains(seat))
            {
                PlayerState player = Players[seat];
                List<Card> hand = player.HandCards;

                if (Phase == Phase.DiscardBuild)
                    moves.AddRange(player.GetPlayableCards(DiscardedCards, playAllFree: true).Select(c => new Command(ActionType.Build, c)));
                else
                {
                    moves.AddRange(player.GetPlayableCards(hand).Select(c => new Command(ActionType.Build, c)));
                    if (player.CanBuildWonder() && hand.Count > 0)
                    {
                        if (pruned)
                            moves.Add(new Command(ActionType.BuildWonder, hand[_rng.Next(hand.Count)]));
                        else
                            moves.AddRange(hand.Select(c => new Command(ActionType.BuildWonder, c)));
                    }
                    if (Phase == Phase.ChooseMoves && player.FreeCardOnce)
                        moves.AddRange(player.GetPlayableCards(hand, playAllFree: true).Select(c => new Command(ActionType.BuildFreeOnce, c)));
                    if (hand.Count > 0)
                    {
                        if (pruned)
                            moves.Add(new Command(ActionType.Discard, hand[_rng.Next(hand.Count)]));
                        else
                            moves.AddRange(hand.Select(c => new Command(ActionType.Discard, c)));
                    }
                }
            }

            if (moves.Count == 0)
                moves.Add(new Command(ActionType.Skip, null));
            // the same card twice in a hand gives the same move twice
            return moves.Distinct().ToList();
        }
    }
}
