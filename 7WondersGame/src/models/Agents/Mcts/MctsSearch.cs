using System.Diagnostics;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Agents.Mcts
{
    /// <summary>One decision of one player at a node: its statistics.</summary>
    internal sealed class MoveStats
    {
        public MoveStats(int players) => Total = new double[players];

        public int Visits;
        /// <summary>How many times the move was legal when its node was visited (ISMCTS: it may not be in every guess).</summary>
        public int Available;
        /// <summary>The rewards summed, per seat.</summary>
        public readonly double[] Total;
        /// <summary>The searching player's win shares summed.</summary>
        public double Wins;
    }

    internal sealed class Node
    {
        public readonly Dictionary<(int Seat, Command Move), MoveStats> Stats = new();
        /// <summary>The keys of <see cref="Stats"/> in the order they were added (for ties).</summary>
        public readonly List<(int Seat, Command Move)> Order = new();
        public readonly Dictionary<MoveKey, Node> Children = new();
    }

    /// <summary>The moves made at a node (one in a sequential tree, everyone pending in DUCT): the key of the child.</summary>
    internal readonly struct MoveKey : IEquatable<MoveKey>
    {
        private readonly (int Seat, Command Move)[] _moves;

        public MoveKey(IEnumerable<(int Seat, Command Move)> moves) => _moves = moves.ToArray();

        public bool Equals(MoveKey other) => _moves.SequenceEqual(other._moves);
        public override bool Equals(object? obj) => obj is MoveKey k && Equals(k);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var m in _moves)
                hash.Add(m);
            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// One search, for one decision of the player in seat <c>me</c>. Each iteration takes a new copy of the game (a
    /// guess at the hidden cards, unless the search may see them), walks down the tree from the root choosing moves
    /// by the tree policy, adds one new node, plays a rollout from there and adds its reward to every move on the way.
    /// The tree is keyed by moves only, so the guesses share it (single-observer ISMCTS).
    /// With <see cref="MctsConfig.Parallel"/> the iterations run on several threads (see <see cref="MctsParallel"/>).
    /// </summary>
    internal sealed class MctsSearch
    {
        private static readonly Command Skip = new(ActionType.Skip, null);

        private readonly MctsConfig _c;
        private readonly int _me;
        private readonly int _n;
        private readonly Game _root;
        private readonly SeatKnowledge? _known;
        private readonly Random _rng;
        // only reads the state and keeps nothing between calls, so every thread can use it
        private readonly HeuristicAI _policy = new();
        private Node _tree = new();
        private readonly int _budget;

        // parallel search: the lock of a shared tree (Tree) and what the threads measured
        private readonly object _treeLock = new();
        private int _iterations;
        private long _lockWaitTicks;
        private long _otherThreadBytes;

        /// <param name="root">A copy of the game, which the search doesn't change.</param>
        /// <param name="known">What the player knows about the hidden cards (for <see cref="MctsInformation.Determinized"/>).</param>
        public MctsSearch(MctsConfig config, int me, Game root, SeatKnowledge? known, Random rng)
            : this(config, me, root, known, rng, config.Iterations) { }

        private MctsSearch(MctsConfig config, int me, Game root, SeatKnowledge? known, Random rng, int budget)
        {
            _c = config;
            _me = me;
            _n = root.Players.Count;
            _root = root;
            _known = known;
            _rng = rng;
            _budget = budget;
        }

        /// <summary>Iterations (rollouts) made, by all threads.</summary>
        public int Iterations => _iterations;

        /// <summary>Time the threads spent waiting for the shared tree's lock, summed (Tree only).</summary>
        public double LockWaitMs => _lockWaitTicks * 1000.0 / Stopwatch.Frequency;

        /// <summary>Memory allocated by threads other than the one that called <see cref="Run"/>.</summary>
        public long OtherThreadBytes => _otherThreadBytes;

        private bool IsParallel => _c.Parallel != MctsParallel.None && _c.Workers > 1;

        /// <summary>Searches until the budget is used up (at least one iteration) and returns the move to play.</summary>
        public Command Run(Stopwatch clock)
        {
            if (!IsParallel)
                RunSerial(clock);
            else if (_c.Parallel == MctsParallel.Root)
                RunRoot(clock);
            else if (_c.Parallel == MctsParallel.Leaf)
                RunLeaf(clock);
            else
                RunTree(clock);
            return FinalMove();
        }

        private bool More(Stopwatch clock) =>
            _c.Budget == MctsBudget.Iterations ? _iterations < _budget : clock.ElapsedMilliseconds < _c.TimeMs;

        // =========== the plain search ===========

        private void RunSerial(Stopwatch clock)
        {
            do
            {
                Iterate();
                _iterations++;
            }
            while (More(clock));
        }

        /// <summary>One iteration: guess, walk down and expand, roll out, back up.</summary>
        private void Iterate()
        {
            Game g = Sample(_root, _rng);
            var path = Descend(g, _rng);
            Backup(path, Rollout(g, _rng));
        }

        /// <summary>A copy of the game to search: a guess at the hidden cards, unless the search may see them.</summary>
        private Game Sample(Game root, Random rng) =>
            _c.Information == MctsInformation.PerfectInformation
                ? root.DeepCopy()
                : Determinization.Sample(root, _me, _known, rng);

        /// <summary>
        /// Selection and expansion: walks down from the root, making the tree policy's moves in <paramref name="g"/>,
        /// until it adds a new node (or the game ends). Returns the moves made at each node.
        /// </summary>
        private List<(Node Node, List<(int Seat, Command Move)> Moves)> Descend(Game g, Random rng)
        {
            var path = new List<(Node Node, List<(int Seat, Command Move)> Moves)>();
            Node node = _tree;
            while (g.Phase != Phase.GameOver)
            {
                List<(int Seat, Command Move)> moves = _c.Tree == MctsTree.Sequential ? ChooseSequential(node, g, rng) : ChooseDuct(node, g, rng);
                path.Add((node, moves));
                foreach (var (seat, move) in moves)
                    g.Submit(seat, move);

                var key = new MoveKey(moves);
                if (!node.Children.TryGetValue(key, out Node? child))
                {
                    // a new node: roll out from it
                    node.Children[key] = new Node();
                    break;
                }
                node = child;
            }
            return path;
        }

        /// <summary>Backpropagation: adds a rollout's rewards to every move on the path.</summary>
        private void Backup(List<(Node Node, List<(int Seat, Command Move)> Moves)> path, (double[] Reward, double MyWin) result)
        {
            (double[] reward, double myWin) = result;
            foreach (var (n, moves) in path)
            {
                foreach (var m in moves)
                {
                    MoveStats s = n.Stats[m];
                    s.Visits++;
                    for (int i = 0; i < _n; i++)
                        s.Total[i] += reward[i];
                    s.Wins += myWin;
                }
            }
        }

        // =========== parallel search ===========

        /// <summary>A new random stream for a worker, drawn from the search's own (so the same seed gives the same streams).</summary>
        private Random NewStream() => Seeding.CreateRandom(Seeding.Mix((ulong)_rng.NextInt64()));

        /// <summary>
        /// Runs work(0..count-1) on <see cref="MctsConfig.Threads"/> threads (one after another for 1), and counts the
        /// memory the other threads allocate.
        /// </summary>
        private void RunWorkers(int count, Action<int> work)
        {
            int threads = _c.Threads > 0 ? Math.Min(_c.Threads, count) : count;
            if (threads == 1)
            {
                for (int i = 0; i < count; i++)
                    work(i);
                return;
            }
            int caller = Environment.CurrentManagedThreadId;
            System.Threading.Tasks.Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                work(i);
                if (Environment.CurrentManagedThreadId != caller)
                    Interlocked.Add(ref _otherThreadBytes, GC.GetAllocatedBytesForCurrentThread() - before);
            });
        }

        /// <summary>
        /// Root parallelisation: Workers separate searches, each with its own copy of the root, random stream, tree and
        /// share of the iterations; then their root statistics are added up, and the move is chosen from the sum.
        /// </summary>
        private void RunRoot(Stopwatch clock)
        {
            int k = _c.Workers;
            // made here, one after another, so each worker gets the same copy and stream whatever the thread timing
            var workers = new MctsSearch[k];
            for (int i = 0; i < k; i++)
            {
                int share = _budget / k + (i < _budget % k ? 1 : 0);
                workers[i] = new MctsSearch(_c, _me, _root.DeepCopy(), _known, NewStream(), share);
            }

            RunWorkers(k, i => workers[i].RunSerial(clock));

            var merged = new Node();
            foreach (MctsSearch w in workers)
            {
                foreach (var key in w._tree.Order)
                {
                    MoveStats s = w._tree.Stats[key];
                    if (!merged.Stats.TryGetValue(key, out MoveStats? sum))
                    {
                        sum = new MoveStats(_n);
                        merged.Stats[key] = sum;
                        merged.Order.Add(key);
                    }
                    sum.Visits += s.Visits;
                    sum.Available += s.Available;
                    sum.Wins += s.Wins;
                    for (int i = 0; i < _n; i++)
                        sum.Total[i] += s.Total[i];
                }
                _iterations += w._iterations;
            }
            _tree = merged;
        }

        /// <summary>
        /// Leaf parallelisation: one tree; each iteration walks down and expands as usual, then plays Workers rollouts
        /// from the new node at once (each on its own copy and stream) and backs up every one of them.
        /// </summary>
        private void RunLeaf(Stopwatch clock)
        {
            int k = _c.Workers;
            var games = new Game[k];
            var streams = new Random[k];
            var results = new (double[] Reward, double MyWin)[k];
            do
            {
                Game g = Sample(_root, _rng);
                var path = Descend(g, _rng);
                for (int j = 0; j < k; j++)
                {
                    games[j] = j == 0 ? g : g.DeepCopy();
                    streams[j] = NewStream();
                }

                RunWorkers(k, j => results[j] = Rollout(games[j], streams[j]));

                foreach (var result in results)
                    Backup(path, result);
                _iterations += k;
            }
            while (More(clock));
        }

        /// <summary>
        /// Tree parallelisation: Workers threads share one tree. Walking down (and expanding) and backing up are done
        /// holding one lock; the rollout, which takes most of the time, outside it. With
        /// <see cref="MctsConfig.VirtualLoss"/>, a thread's path counts as lost until its rollout is backed up.
        /// </summary>
        private void RunTree(Stopwatch clock)
        {
            int k = _c.Workers;
            var roots = new Game[k];
            var streams = new Random[k];
            for (int i = 0; i < k; i++)
            {
                roots[i] = _root.DeepCopy();
                streams[i] = NewStream();
            }
            int started = 0;

            RunWorkers(k, i =>
            {
                Game root = roots[i];
                Random rng = streams[i];
                bool first = true;
                while (true)
                {
                    // the first worker always makes one iteration, so even a tiny time budget gives a move
                    bool go = _c.Budget == MctsBudget.Iterations
                        ? Interlocked.Increment(ref started) <= Math.Max(1, _budget)
                        : clock.ElapsedMilliseconds < _c.TimeMs || (i == 0 && first);
                    if (!go)
                        break;
                    first = false;

                    Game g = Sample(root, rng);
                    List<(Node Node, List<(int Seat, Command Move)> Moves)> path;
                    EnterTree();
                    try
                    {
                        path = Descend(g, rng);
                        AddVirtualLoss(path, _c.VirtualLoss);
                    }
                    finally
                    {
                        Monitor.Exit(_treeLock);
                    }

                    var result = Rollout(g, rng);

                    EnterTree();
                    try
                    {
                        AddVirtualLoss(path, -_c.VirtualLoss);
                        Backup(path, result);
                        _iterations++;
                    }
                    finally
                    {
                        Monitor.Exit(_treeLock);
                    }
                }
            });
        }

        /// <summary>Takes the tree's lock, adding the time spent waiting for it to <see cref="LockWaitMs"/>.</summary>
        private void EnterTree()
        {
            long start = Stopwatch.GetTimestamp();
            Monitor.Enter(_treeLock);
            Interlocked.Add(ref _lockWaitTicks, Stopwatch.GetTimestamp() - start);
        }

        /// <summary>Adds (or with a negative count takes back) visits with reward 0 on every move of the path.</summary>
        private static void AddVirtualLoss(List<(Node Node, List<(int Seat, Command Move)> Moves)> path, int count)
        {
            if (count == 0)
                return;
            foreach (var (n, moves) in path)
                foreach (var m in moves)
                    n.Stats[m].Visits += count;
        }

        // =========== tree policy ===========

        /// <summary>One player decides: the searching player whenever it is pending, otherwise the lowest pending seat.</summary>
        private List<(int Seat, Command Move)> ChooseSequential(Node node, Game g, Random rng)
        {
            IReadOnlyList<int> pending = g.PendingPlayers();
            int seat = pending.Contains(_me) ? _me : pending[0];
            return new() { (seat, Choose(node, g, seat, rng)) };
        }

        /// <summary>DUCT: every pending player decides, each from its own statistics.</summary>
        private List<(int Seat, Command Move)> ChooseDuct(Node node, Game g, Random rng) =>
            g.PendingPlayers().Select(seat => (seat, Choose(node, g, seat, rng))).ToList();

        /// <summary>A move not tried yet at this node (a random one), else the best by the tree policy.</summary>
        private Command Choose(Node node, Game g, int seat, Random rng)
        {
            List<Command> legal = _c.Pruning == MctsPruning.RandomCard ? g.LegalMovesPruned(seat) : g.LegalMoves(seat);
            var untried = new List<Command>();
            foreach (Command m in legal)
            {
                if (node.Stats.TryGetValue((seat, m), out MoveStats? s))
                    s.Available++;
                else
                    untried.Add(m);
            }
            if (untried.Count > 0)
            {
                Command m = untried[rng.Next(untried.Count)];
                node.Stats[(seat, m)] = new MoveStats(_n) { Available = 1 };
                node.Order.Add((seat, m));
                return m;
            }

            Command best = legal[0];
            double bestValue = double.NegativeInfinity;
            foreach (Command m in legal)
            {
                double v = TreeValue(node.Stats[(seat, m)], seat);
                if (v > bestValue)
                {
                    best = m;
                    bestValue = v;
                }
            }
            return best;
        }

        private double TreeValue(MoveStats s, int seat)
        {
            if (s.Visits == 0)
                return double.PositiveInfinity;
            double total = _c.Backup switch
            {
                MctsBackup.MaxN => s.Total[seat],
                MctsBackup.Paranoid => seat == _me ? s.Total[_me] : -s.Total[_me],
                _ => s.Total[_me],
            };
            double n = s.Visits;
            return _c.Exploration switch
            {
                MctsExploration.Smoothed => total / (n + 1) + _c.C * Math.Sqrt(Math.Log(s.Available + 1) / (n + 1)),
                MctsExploration.Diminishing => total / n + _c.C / (1 + Math.Log(1 + n)) * Math.Sqrt(Math.Log(s.Available) / n),
                _ => total / n + _c.C * Math.Sqrt(Math.Log(s.Available) / n),
            };
        }

        /// <summary>Of the searching player's moves at the root, the one the config says.</summary>
        private Command FinalMove()
        {
            Command? best = null;
            double bestValue = double.NegativeInfinity;
            foreach (var key in _tree.Order.Where(k => k.Seat == _me))
            {
                MoveStats s = _tree.Stats[key];
                double v = _c.FinalMove switch
                {
                    MctsFinalMove.HighestReward => s.Total[_me],
                    MctsFinalMove.MostWins => s.Wins,
                    _ => s.Visits,
                };
                if (best == null || v > bestValue)
                {
                    best = key.Move;
                    bestValue = v;
                }
            }
            return best ?? throw new InvalidOperationException("The search tried no move of its own player.");
        }

        /// <summary>
        /// The searching player's most visited root moves, for the trace: visits, average reward and win share,
        /// e.g. "Build Loom 412 visits, reward 0.61, wins 55% | ...".
        /// </summary>
        public string TopMoves(int count) => string.Join(" | ", _tree.Order
            .Where(k => k.Seat == _me)
            .Select(k => (k.Move, Stats: _tree.Stats[k]))
            .OrderByDescending(m => m.Stats.Visits)
            .Take(count)
            .Select(m => string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"{m.Move.Action} {m.Move.Card?.Name ?? "-"} {m.Stats.Visits} visits, " +
                $"reward {Average(m.Stats.Total[_me], m.Stats.Visits):0.##}, wins {Average(m.Stats.Wins, m.Stats.Visits):0%}")));

        private static double Average(double total, int visits) => visits == 0 ? 0 : total / visits;

        // =========== rollout ===========

        /// <summary>Plays on to the end (of the game, or of the era if the config says so) and gives the rewards.</summary>
        private (double[] Reward, double MyWin) Rollout(Game g, Random rng)
        {
            int era = g.Era;
            bool eraEnd = _c.RolloutEnd == MctsRolloutEnd.EraEnd;
            while (g.Phase != Phase.GameOver && !(eraEnd && g.Era != era))
            {
                // everyone chooses from the same state, as in a real game
                var moves = g.PendingPlayers().Select(seat => (seat, RolloutMove(g, seat, rng))).ToList();
                foreach (var (seat, move) in moves)
                    g.Submit(seat, move);
            }
            return Score(g);
        }

        private Command RolloutMove(Game g, int seat, Random rng)
        {
            PlayerState p = g.Players[seat];
            bool random = _c.Rollout == MctsRollout.Random ||
                          _c.Rollout == MctsRollout.EpsilonHeuristic && rng.NextDouble() < _c.Epsilon;
            return random ? RandomMove(g, p, rng) : _policy.Choose(p, g.Phase, g.Era, g.DiscardedCards);
        }

        /// <summary>A random action the player can take, then a random card for it (as <see cref="RandomChoiceAI"/>).</summary>
        private Command RandomMove(Game g, PlayerState p, Random rng)
        {
            if (g.Phase == Phase.DiscardBuild)
            {
                List<Card> fromPile = p.GetPlayableCards(g.DiscardedCards, playAllFree: true);
                return fromPile.Count == 0 ? Skip : new Command(ActionType.Build, fromPile[rng.Next(fromPile.Count)]);
            }
            List<Card> hand = p.HandCards;
            if (hand.Count == 0)
                return Skip;

            List<Card> playable = p.GetPlayableCards(hand);
            List<Card>? free = g.Phase == Phase.ChooseMoves && p.FreeCardOnce ? p.GetPlayableCards(hand, playAllFree: true) : null;
            var options = new List<ActionType>(4);
            if (playable.Count > 0)
                options.Add(ActionType.Build);
            if (free is { Count: > 0 })
                options.Add(ActionType.BuildFreeOnce);
            if (p.CanBuildWonder())
                options.Add(ActionType.BuildWonder);
            options.Add(ActionType.Discard);

            ActionType action = options[rng.Next(options.Count)];
            List<Card> cards = action switch
            {
                ActionType.Build => playable,
                ActionType.BuildFreeOnce => free!,
                _ => hand,
            };
            return new Command(action, cards[rng.Next(cards.Count)]);
        }

        /// <summary>Each seat's reward and the searching player's win share, from the scores now.</summary>
        private (double[] Reward, double MyWin) Score(Game g)
        {
            // the game is a throwaway copy at the end; mid-game (era-end cutoff) score copies so it can't change
            List<PlayerState> players = g.Phase == Phase.GameOver ? g.Players : PlayerState.CloneTable(g.Players);
            foreach (PlayerState p in players)
            {
                p.CopyGuild();
                p.CalculateScore();
            }
            List<PlayerStanding> standings = GameResult.Rank(players);

            var reward = new double[_n];
            for (int s = 0; s < _n; s++)
            {
                int vp = players[s].VictoryPoints;
                reward[s] = _c.Reward switch
                {
                    MctsReward.VpMargin => vp - players.Where(o => o.Id != s).Max(o => o.VictoryPoints),
                    MctsReward.RawVp => vp,
                    _ => standings[s].WinShare,
                };
            }
            return (reward, standings[_me].WinShare);
        }
    }
}
