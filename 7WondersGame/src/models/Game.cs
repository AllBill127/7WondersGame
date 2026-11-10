using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace _7WondersGame.src.models
{
    public class Game
    {
        public List<Player> Players { get; set; }
        int NumPlayers { get; set; } = 3;
        public int Era { get; set; } = 1;
        // 0, 7 , 14, 21 are new era turns (0 being the start of the game)
        public int Turn { get; set; } = 0;
        public List<Card>[] Decks { get; set; } 
        public List<Card> DiscardedCards { get; set; }

        // game loop data
        /// <summary>Where an agent's <see cref="Player.ChooseMoveCommand"/> puts its answer. Not game state: never copied.</summary>
        public Command[] PlayerCommands { get; set; }

        // randomness (B1): everything random in a game comes from its seed
        public ulong Seed { get; private set; }
        private readonly Random _rng;
        private readonly Dictionary<int, Random> _agentRngs = new();
        private ulong _copyCount = 0;

        /// <summary>
        /// Step-by-step trace of this game: Debug for the playout, Verbose for detail.
        /// Silent unless the runner sets it. <see cref="DeepCopy"/> never copies it, so MCTS copies and rollouts stay silent.
        /// Players log through it too (<see cref="Loop"/> hands it to them).
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

            // init with default size; changes in InitGame()
            PlayerCommands = new Command[NumPlayers];
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

            // fully copy players
            // copy player data
            foreach (Player p in Players)
                copy.Players.Add((Player)p.DeepCopy());
            // set player neighbors
            copy.NumPlayers = this.NumPlayers;
            copy.SetPlayerNeighbors();

            copy.Era = this.Era;
            copy.Turn = this.Turn;

            // copy decks
            copy.Decks[0] = this.Decks[0].Select(e => e).ToList();
            copy.Decks[1] = this.Decks[1].Select(e => e).ToList();
            copy.Decks[2] = this.Decks[2].Select(e => e).ToList();

            copy.DiscardedCards = this.DiscardedCards.Select(e => e).ToList();

            // the agents' answer slots stay empty: a copy never shows a choice made in the original (M2)
            copy.PlayerCommands = new Command[this.PlayerCommands.Length];

            copy.Phase = this.Phase;
            copy._waiting = this._waiting?.ToArray();
            copy._chosen = this._chosen.ToArray();

            return copy;
        }


        // ============ Game creation related ============
        /// <summary>
        /// Setup initial game data <br/>
        /// - Game Players must be initiated and set before calling this function
        /// </summary>
        public void InitGame()
        {
            NumPlayers = Players.Count;
            PlayerCommands = new Command[NumPlayers];

            SetPlayerNeighbors();

            CreateDecks();
            GiveCards();
            GiveWonders();
        }


        /// <summary>
        /// Setup initial game data with game dataset containing decks card ids and wonder ids in specific shuffled order<br/>
        /// - Game Players must be initiated and set before calling this function
        /// </summary>
        public void InitGameNotRandom(List<int>[] gameDataset)
        {
            NumPlayers = Players.Count;
            PlayerCommands = new Command[NumPlayers];

            SetPlayerNeighbors();


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

            foreach (Player player in Players)
            {
                cards.Clear();
                for (int i = 0; i < 7; i++)
                {
                    cards.Add(Decks[Era - 1][cardIdx++]);
                }

                player.HandCards = cards.Select(e => e).ToList();
            }
        }

        private Wonder CreateWonders(int wonderId)
        {
            Wonder wonder = null;
            switch (wonderId)
            {
                case 0:
                    wonder = new GizahA();
                    break;
                case 1:
                    wonder = new BabylonA();
                    break;
                case 2:
                    wonder = new OlympiaA();
                    break;
                case 3:
                    wonder = new RhodosA();
                    break;
                case 4:
                    wonder = new EphesosA();
                    break;
                case 5:
                    wonder = new AlexandriaA();
                    break;
                case 6:
                    wonder = new HalikarnassosA();
                    break;
                case 7:
                    wonder = new GizahB();
                    break;
                case 8:
                    wonder = new BabylonB();
                    break;
                case 9:
                    wonder = new OlympiaB();
                    break;
                case 10:
                    wonder = new RhodosB();
                    break;
                case 11:
                    wonder = new EphesosB();
                    break;
                case 12:
                    wonder = new AlexandriaB();
                    break;
                case 13:
                    wonder = new HalikarnassosB();
                    break;
                default:
                    break;
            }

            return wonder;
        }

        /// <summary>
        /// Give a player a wonder board and its starting resource. Used by both setup paths.
        /// </summary>
        private void AssignWonder(Player player, int wonderId)
        {
            player.Board = CreateWonders(wonderId);
            player.Resources[player.Board.Production] += 1;
        }

        private void GiveWonders()
        {
            Random rng = _rng;
            bool[] wonder_availability = new bool[7];
            for (int i = 0; i < 7; i++)
                wonder_availability[i] = true;

            foreach (Player player in Players)
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
        // The turn rules live here once. Loop and the MCTS trees all go through PendingPlayers and Submit.

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
            Player player = Players[seat];

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

        /// <summary>
        /// Enters a phase: marks who must decide and whether each of them can pay for a wonder stage (the rules read
        /// <see cref="Player.CanBuildWonder"/>). False if nobody has anything to decide in it.
        /// </summary>
        private bool StartPhase(Phase phase)
        {
            Phase = phase;
            bool any = false;
            foreach (Player p in Players)
            {
                bool waits = phase switch
                {
                    Phase.ChooseMoves => true,
                    Phase.SeventhCard => p.PlaySeventh && p.HandCards.Count > 0,
                    Phase.DiscardBuild => CanBuildFromDiscardPile(p),
                    _ => false,
                };
                _waiting![p.Id] = waits;
                if (!waits)
                    continue;
                any = true;
                p.CheckCanBuildWonder();
            }
            return any;
        }

        /// <summary>
        /// What an agent looks at when choosing: its playable cards and whether it can build a wonder stage.
        /// Worked out just before asking, because agents overwrite <see cref="Player.PlayableCards"/> while choosing
        /// and MCTS rollouts swap in other agents.
        /// </summary>
        private void PrepareToAsk(Player p)
        {
            // the discard-pile build is free, so only the no-duplicates rule limits it
            p.PlayableCards = Phase == Phase.DiscardBuild
                ? p.GetPlayableCards(DiscardedCards, playAllFree: true)
                : p.GetPlayableCards(p.HandCards);
            p.CheckCanBuildWonder();
        }

        private static bool CanBuildFromDiscardPile(Player p) =>
            p.DiscardFree && (p.Board.Id == WonderId.HalikarnassosA && p.Board.Stage == 2 ||
                              p.Board.Id == WonderId.HalikarnassosB && p.Board.Stage >= 1);

        /// <summary>After the turn's moves (and Babylon's 7th card): the end of an era's last hands, then Halikarnassos.</summary>
        private void AfterSeventhCard()
        {
            // the 7th card slot (6, 13, 20) is used up whether or not someone played it
            if (Turn % 7 == 6)
                Turn++;

            // the last card of each hand is discarded at the end of an era (turns 7, 14 and 21)
            if (Turn % 7 == 0)
            {
                foreach (Player player in Players)
                    DiscardedCards.AddRange(player.HandCards);
                if (Tracing)
                    Trace.Debug("End of era {Era}: last cards discarded: {Cards}", Era, Names(Players.SelectMany(p => p.HandCards)));
            }

            // Halikarnassos A gets its discard-pile build from stage 2 only
            foreach (Player player in Players)
                if (player.DiscardFree && player.Board.Id == WonderId.HalikarnassosA && player.Board.Stage != 2)
                    player.DiscardFree = false;

            if (StartPhase(Phase.DiscardBuild))
                return;
            AfterDiscardBuild();
        }

        /// <summary>The end of a turn: at the end of an era battles and the next era's hands, otherwise pass the hands.</summary>
        private void AfterDiscardBuild()
        {
            if (Turn % 7 == 0)
            {
                foreach (Player player in Players)
                {
                    // Olympia A stage 2: one free card per era
                    player.FreeCardOnce = player.Board.Id == WonderId.OlympiaA && player.Board.Stage >= 2;
                    player.Battle(Era);
                }
                if (Tracing)
                    foreach (Player player in Players)
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
                    foreach (Player player in Players.Where(p => p.FreeCardOnce))
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
            Player p,
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
        /// <param name="record">true for a real game (not an MCTS rollout): keep <see cref="Results"/> and the final
        /// state (players, cards, discard pile) instead of releasing it.</param>
        /// <returns>Each seat's final VP.</returns>
        public Dictionary<int, int> Loop(bool record = false)
        {
            foreach (Player player in Players)
                player.Trace = Trace;
            if (Turn == 0 && Tracing)
                TraceSetup();

            while (PendingPlayers() is { Count: > 0 } pending)
            {
                foreach (int seat in pending)
                    PrepareToAsk(Players[seat]);
                if (Tracing)
                    TracePhaseStart(pending);

                // Everyone chooses from the same state before any choice is submitted, so no agent sees another's (M2).
                var choices = pending.Select(seat => (Seat: seat, Move: Ask(Players[seat]))).ToList();
                if (Tracing && Phase != Phase.DiscardBuild)
                    foreach (var (seat, move) in choices)
                        TraceChoice(Players[seat], move);

                foreach (var (seat, move) in choices)
                    Submit(seat, move);
            }

            //calculate scores
            foreach (Player player in Players)
            {
                // Copies a neighbor guild before scoring if the player has the ability to do so.
                bool copied = player.CopyGuild();
                if (copied && Tracing)
                    Trace.Debug("Seat {Seat} copies {Guild} from a neighbour (Olympia B)", player.Id, player.PlayedCards[^1].Name);
                player.CalculateScore();
            }
            if (Tracing)
                TraceScores();

            // keep the results before the game memory is released
            if (record)
                Results = GameResult.ForPlayers(Players);

            var scores = new Dictionary<int, int>();
            Players.ForEach(p => scores.Add(p.Id, p.VictoryPoints));
            if (!record)
                ReleaseGameMemory();
            return scores;
        }

        /// <summary>A pending player's decision for the current phase, from the agent.</summary>
        private Command Ask(Player player)
        {
            if (Phase == Phase.DiscardBuild)
            {
                Card? card = player.ChooseMoveCard(this, ActionType.Build, player.PlayableCards);
                return card == null ? new Command(ActionType.Skip, null) : new Command(ActionType.Build, card);
            }

            player.ChooseMoveCommand(this);
            Command move = PlayerCommands[player.Id];
            // the slot only carries the agent's answer; clear it so the next agent can't read it (M2)
            PlayerCommands[player.Id] = null!;
            return move;
        }

        /// <summary>
        /// A chosen move that the rules don't allow (E8). The game stops here instead of carrying on in a state
        /// that shouldn't exist; the message has what is needed to replay it.
        /// </summary>
        private InvalidOperationException MoveFailed(Player player, string move, Card card) =>
            new($"Seat {player.Id} ({player.GetType().Name}) could not {move} {card.Name} on turn {Turn} " +
                $"(era {Era}, game seed {Seed}).");

        /// <summary>One player's move on its own (Babylon's 7th card).</summary>
        public void HandlePlayerCommand(Player player, Command playerCommand, List<Card> cardsList) =>
            ResolveMoves(new[] { (player, playerCommand, cardsList) });

        /// <summary>
        /// Carries out moves made at the same time (R5). Every move is checked and priced against the state before any
        /// of them; then every player pays and places their card; then trade income, discard coins and the effects of
        /// the cards and wonder stages arrive. Coins received can't be spent in the same turn, and seat order doesn't
        /// change the result.
        /// </summary>
        public void ResolveMoves(IReadOnlyList<(Player Player, Command Command, List<Card> Source)> moves)
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
            foreach (Player p in Players)
                Trace.Debug("Seat {Seat}: {Agent} {Config} on {Wonder}", p.Id, p.GetType().Name, p.ConfigJson(), p.Board.Id);
            for (int era = 0; era < Decks.Length; era++)
                Trace.Verbose("Era {Era} deck: {Cards}", era + 1, Names(Decks[era]));
            Trace.Debug("=== Era 1 ===");
        }

        private void TraceTurnStart()
        {
            Trace.Debug("--- Era {Era}, turn {Turn} ---", Era, Turn);
            foreach (Player p in Players)
            {
                Trace.Debug("Seat {Seat}: {Coins} coins, {Shields} shields, {Wonder} stage {Stage}; hand: {Hand}",
                    p.Id, p.Resources[Resource.Coins], p.Resources[Resource.Shields], p.Board.Id, p.Board.Stage, Names(p.HandCards));
                TracePlayable(p);
            }
        }

        private void TracePlayable(Player p) =>
            Trace.Verbose("Seat {Seat} can build: {Playable}; can build a wonder stage: {CanBuildWonder}",
                p.Id, Names(p.PlayableCards), p.CanBuildWonder);

        private void TracePhaseStart(IReadOnlyList<int> pending)
        {
            if (Phase == Phase.ChooseMoves)
            {
                TraceTurnStart();
                return;
            }
            foreach (int seat in pending)
            {
                Player p = Players[seat];
                if (Phase == Phase.SeventhCard)
                    Trace.Debug("Seat {Seat} plays the last card of the hand (Babylon B): {Hand}", p.Id, Names(p.HandCards));
                else
                {
                    Trace.Debug("Seat {Seat} may build from the discard pile for free (Halikarnassos)", p.Id);
                    Trace.Verbose("Discard pile: {Cards}", Names(DiscardedCards));
                }
                TracePlayable(p);
            }
        }

        private void TraceChoice(Player p, Command c)
        {
            Trace.Debug("Seat {Seat} chooses {Action} {Card}", p.Id, c.Action, c.Card?.Name ?? "-");
        }

        /// <summary>What a move changed: coins (own, and paid to each neighbour and the bank), wonder stage and effect flags.</summary>
        private void TraceResult(Player p, PlannedMove? m, TraceSnapshot before)
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

        /// <summary>One player's two battles at the end of an era (after <see cref="Player.Battle"/>; read-only).</summary>
        private void TraceBattle(Player p)
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
            foreach (Player p in Players)
                Trace.Debug("Seat {Seat} {Agent}: {Total} VP = military {Military} + coins {Money} + wonder {Wonder} + " +
                            "civilian {Civilian} + commercial {Commercial} + guilds {Guilds} + science {Science}; {Coins} coins left",
                    p.Id, p.GetType().Name, p.VictoryPoints, p.CalculateMilitaryScore(), p.CalculateMoneyScore(),
                    p.CalculateWonderScore(), p.CalculateCivilianScore(), p.CalculateCommercialScore(),
                    p.CalculateGuildScore(), p.CalculateScientificScore(), p.Resources[Resource.Coins]);
        }

        private sealed record TraceSnapshot(int Coins, int Stage, List<string> Flags)
        {
            public static TraceSnapshot Of(Player p)
            {
                var flags = new List<string>();
                if (p.PlaySeventh) flags.Add(nameof(p.PlaySeventh));
                if (p.DiscardFree) flags.Add(nameof(p.DiscardFree));
                if (p.FreeCardOnce) flags.Add(nameof(p.FreeCardOnce));
                if (p.WonderRawCheap) flags.Add(nameof(p.WonderRawCheap));
                if (p.RawCheapWest) flags.Add(nameof(p.RawCheapWest));
                if (p.RawCheapEast) flags.Add(nameof(p.RawCheapEast));
                if (p.ManufCheap) flags.Add(nameof(p.ManufCheap));
                if (p.ExtraScience) flags.Add(nameof(p.ExtraScience));
                return new TraceSnapshot(p.Resources[Resource.Coins], p.Board.Stage, flags);
            }
        }


        // ================= MCTS related =================
        /// <summary>
        /// Every move a seat may make now. A seat with nothing to decide gets a single Skip, which is what a joint
        /// move holds for it.
        /// </summary>
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
                // Worked out from the state, not from Player.PlayableCards: agents overwrite that while choosing
                // (with Olympia A's free card, for example), and a copy may be taken after they have.
                Player player = Players[seat];
                List<Card> hand = player.HandCards;

                if (Phase == Phase.DiscardBuild)
                    moves.AddRange(player.GetPlayableCards(DiscardedCards, playAllFree: true).Select(c => new Command(ActionType.Build, c)));
                else
                {
                    moves.AddRange(player.GetPlayableCards(hand).Select(c => new Command(ActionType.Build, c)));
                    if (player.CheckCanBuildWonder() && hand.Count > 0)
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

        /// <summary>Every combination of the seats' <see cref="LegalMoves"/>, indexed by seat.</summary>
        public List<List<Command>> JointMoves() => Combinations(LegalMoves);

        /// <summary>Every combination of the seats' <see cref="LegalMovesPruned"/>, indexed by seat.</summary>
        public List<List<Command>> JointMovesPruned() => Combinations(LegalMovesPruned);

        private List<List<Command>> Combinations(Func<int, List<Command>> movesOf)
        {
            List<Command>[] perSeat = Players.Select(p => movesOf(p.Id)).ToArray();
            var all = new List<List<Command>>();
            GetAllCombinationsRecursive(perSeat, 0, new List<Command>(), all);
            return all;
        }

        /// <summary>A joint move (one per seat): every pending seat's move is submitted; the other seats' are ignored.</summary>
        public void SubmitJointMove(IReadOnlyList<Command> jointMove)
        {
            foreach (int seat in PendingPlayers())
                Submit(seat, jointMove[seat]);
        }


        private static void GetAllCombinationsRecursive(List<Command>[] arrayOfLists, int currentIndex, List<Command> currentCombination, List<List<Command>> allCombinations)
        {
            if (currentIndex == arrayOfLists.Length)
            {
                allCombinations.Add(new List<Command>(currentCombination));
                return;
            }

            foreach (var command in arrayOfLists[currentIndex])
            {
                currentCombination.Add(command);
                GetAllCombinationsRecursive(arrayOfLists, currentIndex + 1, currentCombination, allCombinations);
                currentCombination.RemoveAt(currentCombination.Count - 1);
            }
        }

        public void ReleaseGameMemory()
        {
            foreach (var player in Players)
            {
                player.HandCards.Clear();
                player.PlayableCards.Clear();
                player.PlayedCards.Clear();
                player.Resources.Clear();
                player.PlayerEast = null;
                player.PlayerWest = null;
                player.Board = null;
            }
            this.Players.Clear();
            Decks[0].Clear();
            Decks[0].Clear();
            Decks[0].Clear();
            DiscardedCards.Clear();
        }
    }
}
