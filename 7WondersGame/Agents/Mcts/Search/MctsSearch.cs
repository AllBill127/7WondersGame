using System.Diagnostics;
using System.Globalization;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.HiddenInformation;
using _7WondersGame.Agents.Mcts.Configuration;
using _7WondersGame.Agents.Mcts.Configuration.Enums;
using _7WondersGame.Agents.Mcts.Tree;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Results;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Games.Enums;
using _7WondersGame.Engine.Games.Knowledge;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Randomness.Services;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Agents.Mcts.Search;

/// <summary>
/// One search, for one decision of the player in seat <c>me</c>. Each iteration takes a new copy of the game (a
/// guess at the hidden cards, unless the search may see them), walks down the tree from the root choosing moves
/// by the tree policy, adds one new node, plays a rollout from there and adds its reward to every move on the way.
/// The tree is keyed by moves only, so the guesses share it (single-observer ISMCTS).
/// With <see cref="MctsConfig.Parallel"/> the iterations run on several threads (see <see cref="MctsParallel"/>).
/// </summary>
/// <remarks>
/// Selection, expansion, rollout and backup run thousands of times per decision: no LINQ, lambdas or extra
/// allocations there.
/// </remarks>
internal sealed class MctsSearch
{
    private static readonly Command Skip = new(ActionType.Skip, null);

    private readonly MctsConfig _c;
    private readonly int _me;
    private readonly int _n;
    private readonly Game _root;
    private readonly SeatKnowledge? _known;
    private readonly Random _rng;
    private readonly Stopwatch _clock;
    // only reads the state and keeps nothing between calls, so every thread can use it
    private readonly HeuristicAI _policy = new();
    private Node _tree = new();
    private readonly int _budget;

    // parallel search: the lock of a shared tree (Tree) and what the threads measured
    private readonly object _treeLock = new();
    private int _iterations;
    private long _lockWaitTicks;
    private long _otherThreadBytes;

    // parallel search: what each worker works on (Root: its search; Leaf: its rollout's game and stream and the
    // rollout's result; Tree: its own copy of the root and stream)
    private MctsSearch[] _rootWorkers = Array.Empty<MctsSearch>();
    private Game[] _workerGames = Array.Empty<Game>();
    private Random[] _workerStreams = Array.Empty<Random>();
    private (double[] Reward, double MyWin)[] _leafResults = Array.Empty<(double[] Reward, double MyWin)>();
    private int _treeIterationsStarted;

    /// <param name="config">The agent's settings.</param>
    /// <param name="me">The searching player's seat.</param>
    /// <param name="root">A copy of the game, which the search doesn't change.</param>
    /// <param name="known">
    /// What the player knows about the hidden cards (for <see cref="MctsInformation.Determinized"/>).
    /// </param>
    /// <param name="rng">The searching player's random stream.</param>
    /// <param name="clock">Started when the decision began; a time budget counts from it.</param>
    public MctsSearch(MctsConfig config, int me, Game root, SeatKnowledge? known, Random rng, Stopwatch clock)
        : this(config, me, root, known, rng, clock, config.Iterations)
    {
    }

    private MctsSearch(MctsConfig config, int me, Game root, SeatKnowledge? known, Random rng, Stopwatch clock,
                       int budget)
    {
        _c = config;
        _me = me;
        _n = root.Players.Count;
        _root = root;
        _known = known;
        _rng = rng;
        _clock = clock;
        _budget = budget;
    }

    /// <summary>Iterations (rollouts) made, by all threads.</summary>
    public int Iterations
    {
        get
        {
            return _iterations;
        }
    }

    /// <summary>Time the threads spent waiting for the shared tree's lock, summed (Tree only).</summary>
    public double LockWaitMs
    {
        get
        {
            return _lockWaitTicks * 1000.0 / Stopwatch.Frequency;
        }
    }

    /// <summary>Memory allocated by threads other than the one that called <see cref="Run"/>.</summary>
    public long OtherThreadBytes
    {
        get
        {
            return _otherThreadBytes;
        }
    }

    private bool IsParallel
    {
        get
        {
            return _c.Parallel != MctsParallel.None && _c.Workers > 1;
        }
    }

    /// <summary>Searches until the budget is used up (at least one iteration) and returns the move to play.</summary>
    public Command Run()
    {
        if (!IsParallel)
        {
            RunSerial();
        }
        else if (_c.Parallel == MctsParallel.Root)
        {
            RunRoot();
        }
        else if (_c.Parallel == MctsParallel.Leaf)
        {
            RunLeaf();
        }
        else
        {
            RunTree();
        }
        return FinalMove();
    }

    /// <summary>Whether the budget allows another iteration.</summary>
    private bool More()
    {
        if (_c.Budget == MctsBudget.Iterations)
        {
            return _iterations < _budget;
        }
        return _clock.ElapsedMilliseconds < _c.TimeMs;
    }

    // =========== the plain search ===========

    private void RunSerial()
    {
        do
        {
            Iterate();
            _iterations++;
        }
        while (More());
    }

    /// <summary>One iteration: guess, walk down and expand, roll out, back up.</summary>
    private void Iterate()
    {
        Game g = Sample(_root, _rng);
        var path = Descend(g, _rng);
        Backup(path, Rollout(g, _rng));
    }

    /// <summary>A copy of the game to search: a guess at the hidden cards, unless the search may see them.</summary>
    private Game Sample(Game root, Random rng)
    {
        if (_c.Information == MctsInformation.PerfectInformation)
        {
            return root.DeepCopy();
        }
        return Determinization.Sample(root, _me, _known, rng);
    }

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
            List<(int Seat, Command Move)> moves = ChooseMoves(node, g, rng);
            path.Add((node, moves));
            foreach (var (seat, move) in moves)
            {
                g.Submit(seat, move);
            }

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
    private void Backup(List<(Node Node, List<(int Seat, Command Move)> Moves)> path,
                        (double[] Reward, double MyWin) result)
    {
        double[] reward = result.Reward;
        double myWin = result.MyWin;
        foreach (var (n, moves) in path)
        {
            foreach (var m in moves)
            {
                MoveStats s = n.Stats[m];
                s.Visits++;
                for (int i = 0; i < _n; i++)
                {
                    s.Total[i] += reward[i];
                }
                s.Wins += myWin;
            }
        }
    }

    // =========== parallel search ===========

    /// <summary>
    /// A new random stream for a worker, drawn from the search's own (so the same seed gives the same streams).
    /// </summary>
    private Random NewStream()
    {
        ulong seed = Seeding.Mix((ulong)_rng.NextInt64());
        return Seeding.CreateRandom(seed);
    }

    /// <summary>
    /// Runs work(0..count-1) on <see cref="MctsConfig.Threads"/> threads (one after another for 1), and counts the
    /// memory the other threads allocate.
    /// </summary>
    private void RunWorkers(int count, Action<int> work)
    {
        int threads = ThreadsFor(count);
        if (threads == 1)
        {
            for (int i = 0; i < count; i++)
            {
                work(i);
            }
            return;
        }
        var workerThreads = new WorkerThreads(work);
        workerThreads.Run(count, threads);
        _otherThreadBytes += workerThreads.OtherThreadBytes;
    }

    /// <summary>The threads to run this many workers on: the configured number, at most one per worker.</summary>
    private int ThreadsFor(int workerCount)
    {
        if (_c.Threads > 0)
        {
            return Math.Min(_c.Threads, workerCount);
        }
        return workerCount;
    }

    /// <summary>
    /// Root parallelisation: Workers separate searches, each with its own copy of the root, random stream, tree and
    /// share of the iterations; then their root statistics are added up, and the move is chosen from the sum.
    /// </summary>
    private void RunRoot()
    {
        int k = _c.Workers;
        // made here, one after another, so each worker gets the same copy and stream whatever the thread timing
        _rootWorkers = new MctsSearch[k];
        for (int i = 0; i < k; i++)
        {
            int share = IterationShare(i, k);
            _rootWorkers[i] = new MctsSearch(_c, _me, _root.DeepCopy(), _known, NewStream(), _clock, share);
        }

        RunWorkers(k, RunRootWorker);

        var merged = new Node();
        foreach (MctsSearch w in _rootWorkers)
        {
            AddRootStatistics(merged, w._tree);
            _iterations += w._iterations;
        }
        _tree = merged;
    }

    /// <summary>
    /// The worker's share of the iterations: an equal share, one more for the first few if it doesn't divide.
    /// </summary>
    private int IterationShare(int worker, int workerCount)
    {
        int share = _budget / workerCount;
        int remainder = _budget % workerCount;
        if (worker < remainder)
        {
            share++;
        }
        return share;
    }

    private void RunRootWorker(int worker)
    {
        _rootWorkers[worker].RunSerial();
    }

    /// <summary>
    /// Adds the statistics of a worker's root moves to <paramref name="merged"/>'s, keeping first-seen order.
    /// </summary>
    private void AddRootStatistics(Node merged, Node workerRoot)
    {
        foreach (var key in workerRoot.Order)
        {
            MoveStats s = workerRoot.Stats[key];
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
            {
                sum.Total[i] += s.Total[i];
            }
        }
    }

    /// <summary>
    /// Leaf parallelisation: one tree; each iteration walks down and expands as usual, then plays Workers rollouts
    /// from the new node at once (each on its own copy and stream) and backs up every one of them.
    /// </summary>
    private void RunLeaf()
    {
        int k = _c.Workers;
        _workerGames = new Game[k];
        _workerStreams = new Random[k];
        _leafResults = new (double[] Reward, double MyWin)[k];
        do
        {
            Game g = Sample(_root, _rng);
            var path = Descend(g, _rng);
            for (int j = 0; j < k; j++)
            {
                if (j == 0)
                {
                    _workerGames[j] = g;
                }
                else
                {
                    _workerGames[j] = g.DeepCopy();
                }
                _workerStreams[j] = NewStream();
            }

            RunWorkers(k, RunLeafRollout);

            foreach (var result in _leafResults)
            {
                Backup(path, result);
            }
            _iterations += k;
        }
        while (More());
    }

    private void RunLeafRollout(int worker)
    {
        _leafResults[worker] = Rollout(_workerGames[worker], _workerStreams[worker]);
    }

    /// <summary>
    /// Tree parallelisation: Workers threads share one tree. Walking down (and expanding) and backing up are done
    /// holding one lock; the rollout, which takes most of the time, outside it. With
    /// <see cref="MctsConfig.VirtualLoss"/>, a thread's path counts as lost until its rollout is backed up.
    /// </summary>
    private void RunTree()
    {
        int k = _c.Workers;
        _workerGames = new Game[k];
        _workerStreams = new Random[k];
        for (int i = 0; i < k; i++)
        {
            _workerGames[i] = _root.DeepCopy();
            _workerStreams[i] = NewStream();
        }
        _treeIterationsStarted = 0;

        RunWorkers(k, RunTreeWorker);
    }

    /// <summary>
    /// One Tree worker: iterations on its own copy of the root and stream, until the budget is used up.
    /// </summary>
    private void RunTreeWorker(int worker)
    {
        Game root = _workerGames[worker];
        Random rng = _workerStreams[worker];
        bool first = true;
        while (MayStartTreeIteration(worker, first))
        {
            first = false;
            RunTreeIteration(root, rng);
        }
    }

    /// <summary>
    /// Whether a Tree worker may start another iteration. The first worker always makes one, so even a tiny time
    /// budget gives a move.
    /// </summary>
    private bool MayStartTreeIteration(int worker, bool first)
    {
        if (_c.Budget == MctsBudget.Iterations)
        {
            int started = Interlocked.Increment(ref _treeIterationsStarted);
            return started <= Math.Max(1, _budget);
        }
        if (_clock.ElapsedMilliseconds < _c.TimeMs)
        {
            return true;
        }
        return worker == 0 && first;
    }

    /// <summary>One Tree iteration: walks down holding the lock, rolls out without it, backs up holding it.</summary>
    private void RunTreeIteration(Game root, Random rng)
    {
        Game g = Sample(root, rng);
        var path = DescendHoldingLock(g, rng);
        var result = Rollout(g, rng);
        BackupHoldingLock(path, result);
    }

    private List<(Node Node, List<(int Seat, Command Move)> Moves)> DescendHoldingLock(Game g, Random rng)
    {
        EnterTree();
        try
        {
            var path = Descend(g, rng);
            AddVirtualLoss(path, _c.VirtualLoss);
            return path;
        }
        finally
        {
            Monitor.Exit(_treeLock);
        }
    }

    private void BackupHoldingLock(List<(Node Node, List<(int Seat, Command Move)> Moves)> path,
                                   (double[] Reward, double MyWin) result)
    {
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

    /// <summary>Takes the tree's lock, adding the time spent waiting for it to <see cref="LockWaitMs"/>.</summary>
    private void EnterTree()
    {
        long start = Stopwatch.GetTimestamp();
        Monitor.Enter(_treeLock);
        long waited = Stopwatch.GetTimestamp() - start;
        Interlocked.Add(ref _lockWaitTicks, waited);
    }

    /// <summary>Adds (or with a negative count takes back) visits with reward 0 on every move of the path.</summary>
    private static void AddVirtualLoss(List<(Node Node, List<(int Seat, Command Move)> Moves)> path, int count)
    {
        if (count == 0)
        {
            return;
        }
        foreach (var (n, moves) in path)
        {
            foreach (var m in moves)
            {
                n.Stats[m].Visits += count;
            }
        }
    }

    // =========== tree policy ===========

    /// <summary>The moves made at a node: one player's (sequential tree) or every pending player's (DUCT).</summary>
    private List<(int Seat, Command Move)> ChooseMoves(Node node, Game g, Random rng)
    {
        if (_c.Tree == MctsTree.Sequential)
        {
            return ChooseSequential(node, g, rng);
        }
        return ChooseDuct(node, g, rng);
    }

    /// <summary>
    /// One player decides: the searching player whenever it is pending, otherwise the lowest pending seat.
    /// </summary>
    private List<(int Seat, Command Move)> ChooseSequential(Node node, Game g, Random rng)
    {
        int seat = DecidingSeat(g.PendingPlayers());
        return new List<(int Seat, Command Move)> { (seat, Choose(node, g, seat, rng)) };
    }

    private int DecidingSeat(IReadOnlyList<int> pending)
    {
        for (int pendingIndex = 0; pendingIndex < pending.Count; pendingIndex++)
        {
            if (pending[pendingIndex] == _me)
            {
                return _me;
            }
        }
        return pending[0];
    }

    /// <summary>DUCT: every pending player decides, each from its own statistics.</summary>
    private List<(int Seat, Command Move)> ChooseDuct(Node node, Game g, Random rng)
    {
        IReadOnlyList<int> pending = g.PendingPlayers();
        var moves = new List<(int Seat, Command Move)>();
        // an index loop: a foreach over the interface would allocate an enumerator
        for (int pendingIndex = 0; pendingIndex < pending.Count; pendingIndex++)
        {
            int seat = pending[pendingIndex];
            moves.Add((seat, Choose(node, g, seat, rng)));
        }
        return moves;
    }

    /// <summary>A move not tried yet at this node (a random one), else the best by the tree policy.</summary>
    private Command Choose(Node node, Game g, int seat, Random rng)
    {
        List<Command> legal = LegalMovesFor(g, seat);
        List<Command> untried = CountAvailableAndListUntried(node, seat, legal);
        if (untried.Count > 0)
        {
            return TryNewMove(node, seat, untried, rng);
        }
        return BestByTreeValue(node, seat, legal);
    }

    /// <summary>The moves the tree considers for the seat (all legal moves, or the pruned ones).</summary>
    private List<Command> LegalMovesFor(Game g, int seat)
    {
        if (_c.Pruning == MctsPruning.RandomCard)
        {
            return g.LegalMovesPruned(seat);
        }
        return g.LegalMoves(seat);
    }

    /// <summary>
    /// Counts the node's tried moves as available once more, and returns the moves not tried yet, in legal order.
    /// </summary>
    private static List<Command> CountAvailableAndListUntried(Node node, int seat, List<Command> legal)
    {
        var untried = new List<Command>();
        foreach (Command m in legal)
        {
            if (node.Stats.TryGetValue((seat, m), out MoveStats? s))
            {
                s.Available++;
            }
            else
            {
                untried.Add(m);
            }
        }
        return untried;
    }

    /// <summary>Expansion: a random untried move, added to the node's statistics.</summary>
    private Command TryNewMove(Node node, int seat, List<Command> untried, Random rng)
    {
        int untriedIndex = rng.Next(untried.Count);
        Command m = untried[untriedIndex];
        node.Stats[(seat, m)] = new MoveStats(_n) { Available = 1 };
        node.Order.Add((seat, m));
        return m;
    }

    /// <summary>Selection: the legal move with the highest tree value (the first of equal ones).</summary>
    private Command BestByTreeValue(Node node, int seat, List<Command> legal)
    {
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

    /// <summary>The move's value to the deciding seat: its reward so far plus the exploration bonus.</summary>
    private double TreeValue(MoveStats s, int seat)
    {
        if (s.Visits == 0)
        {
            return double.PositiveInfinity;
        }
        double total = TotalRewardFor(s, seat);
        double n = s.Visits;
        switch (_c.Exploration)
        {
            case MctsExploration.Smoothed:
                return total / (n + 1) + _c.C * Math.Sqrt(Math.Log(s.Available + 1) / (n + 1));
            case MctsExploration.Diminishing:
                return total / n + _c.C / (1 + Math.Log(1 + n)) * Math.Sqrt(Math.Log(s.Available) / n);
            default:
                return total / n + _c.C * Math.Sqrt(Math.Log(s.Available) / n);
        }
    }

    /// <summary>The summed reward the deciding seat maximises, by the backup rule.</summary>
    private double TotalRewardFor(MoveStats s, int seat)
    {
        switch (_c.Backup)
        {
            case MctsBackup.MaxN:
                return s.Total[seat];
            case MctsBackup.Paranoid:
                if (seat == _me)
                {
                    return s.Total[_me];
                }
                return -s.Total[_me];
            default:
                return s.Total[_me];
        }
    }

    /// <summary>Of the searching player's moves at the root, the one the config says.</summary>
    private Command FinalMove()
    {
        Command? best = null;
        double bestValue = double.NegativeInfinity;
        foreach (var key in _tree.Order)
        {
            if (key.Seat != _me)
            {
                continue;
            }
            double v = FinalMoveValue(_tree.Stats[key]);
            if (best == null || v > bestValue)
            {
                best = key.Move;
                bestValue = v;
            }
        }
        if (best == null)
        {
            throw new InvalidOperationException("The search tried no move of its own player.");
        }
        return best;
    }

    /// <summary>What the final move maximises: visits, total reward or wins.</summary>
    private double FinalMoveValue(MoveStats s)
    {
        switch (_c.FinalMove)
        {
            case MctsFinalMove.HighestReward:
                return s.Total[_me];
            case MctsFinalMove.MostWins:
                return s.Wins;
            default:
                return s.Visits;
        }
    }

    /// <summary>
    /// The searching player's most visited root moves, for the trace: visits, average reward and win share,
    /// e.g. "Build Loom 412 visits, reward 0.61, wins 55% | ...".
    /// </summary>
    public string TopMoves(int count)
    {
        IEnumerable<string> descriptions = _tree.Order
            .Where(IsSearchingPlayersMove)
            .OrderByDescending(VisitsOf)
            .Take(count)
            .Select(DescribeRootMove);
        return string.Join(" | ", descriptions);
    }

    private bool IsSearchingPlayersMove((int Seat, Command Move) key)
    {
        return key.Seat == _me;
    }

    private int VisitsOf((int Seat, Command Move) key)
    {
        return _tree.Stats[key].Visits;
    }

    /// <summary>
    /// One root move for <see cref="TopMoves"/>, e.g. "Build Loom 412 visits, reward 0.61, wins 55%".
    /// </summary>
    private string DescribeRootMove((int Seat, Command Move) key)
    {
        MoveStats s = _tree.Stats[key];
        double averageReward = Average(s.Total[_me], s.Visits);
        double averageWins = Average(s.Wins, s.Visits);
        return string.Create(CultureInfo.InvariantCulture,
            $"{key.Move.Action} {CardNameOrDash(key.Move)} {s.Visits} visits, " +
            $"reward {averageReward:0.##}, wins {averageWins:0%}");
    }

    private static string CardNameOrDash(Command move)
    {
        if (move.Card == null)
        {
            return "-";
        }
        return move.Card.Name;
    }

    private static double Average(double total, int visits)
    {
        if (visits == 0)
        {
            return 0;
        }
        return total / visits;
    }

    // =========== rollout ===========

    /// <summary>Plays on to the end (of the game, or of the era if the config says so) and gives the rewards.</summary>
    private (double[] Reward, double MyWin) Rollout(Game g, Random rng)
    {
        int era = g.Era;
        bool eraEnd = _c.RolloutEnd == MctsRolloutEnd.EraEnd;
        var moves = new List<(int Seat, Command Move)>(_n);
        while (g.Phase != Phase.GameOver && !(eraEnd && g.Era != era))
        {
            // everyone chooses from the same state, as in a real game
            moves.Clear();
            IReadOnlyList<int> pending = g.PendingPlayers();
            for (int pendingIndex = 0; pendingIndex < pending.Count; pendingIndex++)
            {
                int seat = pending[pendingIndex];
                moves.Add((seat, RolloutMove(g, seat, rng)));
            }
            foreach (var (seat, move) in moves)
            {
                g.Submit(seat, move);
            }
        }
        return Score(g);
    }

    private Command RolloutMove(Game g, int seat, Random rng)
    {
        PlayerState p = g.Players[seat];
        if (IsRandomRolloutMove(rng))
        {
            return RandomMove(g, p, rng);
        }
        return _policy.Choose(p, g.Phase, g.Era, g.DiscardedCards);
    }

    /// <summary>Whether this rollout move is a random one (always, never, or with probability ε).</summary>
    private bool IsRandomRolloutMove(Random rng)
    {
        if (_c.Rollout == MctsRollout.Random)
        {
            return true;
        }
        if (_c.Rollout == MctsRollout.EpsilonHeuristic)
        {
            return rng.NextDouble() < _c.Epsilon;
        }
        return false;
    }

    /// <summary>
    /// A random action the player can take, then a random card for it (as <see cref="RandomChoiceAI"/>).
    /// </summary>
    private static Command RandomMove(Game g, PlayerState p, Random rng)
    {
        if (g.Phase == Phase.DiscardBuild)
        {
            return RandomDiscardPileBuild(g, p, rng);
        }
        List<Card> hand = p.HandCards;
        if (hand.Count == 0)
        {
            return Skip;
        }

        List<Card> playable = p.GetPlayableCards(hand);
        List<Card>? free = null;
        if (g.Phase == Phase.ChooseMoves && p.FreeCardOnce)
        {
            free = p.GetPlayableCards(hand, playAllFree: true);
        }
        var options = new List<ActionType>(4);
        if (playable.Count > 0)
        {
            options.Add(ActionType.Build);
        }
        if (free != null && free.Count > 0)
        {
            options.Add(ActionType.BuildFreeOnce);
        }
        if (p.CanBuildWonder())
        {
            options.Add(ActionType.BuildWonder);
        }
        options.Add(ActionType.Discard);

        int optionIndex = rng.Next(options.Count);
        ActionType action = options[optionIndex];
        List<Card> cards = CardsFor(action, hand, playable, free);
        int cardIndex = rng.Next(cards.Count);
        return new Command(action, cards[cardIndex]);
    }

    /// <summary>Halikarnassos: a random playable discard-pile card, or Skip when there is none.</summary>
    private static Command RandomDiscardPileBuild(Game g, PlayerState p, Random rng)
    {
        List<Card> fromPile = p.GetPlayableCards(g.DiscardedCards, playAllFree: true);
        if (fromPile.Count == 0)
        {
            return Skip;
        }
        int cardIndex = rng.Next(fromPile.Count);
        return new Command(ActionType.Build, fromPile[cardIndex]);
    }

    /// <summary>
    /// The cards the action can take: playable ones to build, the free ones for Olympia, else the hand.
    /// </summary>
    private static List<Card> CardsFor(ActionType action, List<Card> hand, List<Card> playable, List<Card>? free)
    {
        switch (action)
        {
            case ActionType.Build:
                return playable;
            case ActionType.BuildFreeOnce:
                return free!;
            default:
                return hand;
        }
    }

    /// <summary>Each seat's reward and the searching player's win share, from the scores now.</summary>
    private (double[] Reward, double MyWin) Score(Game g)
    {
        List<PlayerState> players = PlayersToScore(g);
        foreach (PlayerState p in players)
        {
            p.CopyGuild();
            p.CalculateScore();
        }
        List<PlayerStanding> standings = GameResult.Rank(players);

        var reward = new double[_n];
        for (int s = 0; s < _n; s++)
        {
            reward[s] = RewardFor(s, players, standings);
        }
        return (reward, standings[_me].WinShare);
    }

    /// <summary>
    /// The players to score: the game is a throwaway copy at the end; mid-game (era-end cutoff) a copy of the table,
    /// so the game can't change.
    /// </summary>
    private static List<PlayerState> PlayersToScore(Game g)
    {
        if (g.Phase == Phase.GameOver)
        {
            return g.Players;
        }
        return PlayerState.CloneTable(g.Players);
    }

    /// <summary>The seat's reward of the kind the config says.</summary>
    private double RewardFor(int seat, List<PlayerState> players, List<PlayerStanding> standings)
    {
        int vp = players[seat].VictoryPoints;
        switch (_c.Reward)
        {
            case MctsReward.VpMargin:
                return vp - BestOpponentVictoryPoints(players, seat);
            case MctsReward.RawVp:
                return vp;
            default:
                return standings[seat].WinShare;
        }
    }

    private static int BestOpponentVictoryPoints(List<PlayerState> players, int seat)
    {
        int best = int.MinValue;
        foreach (PlayerState opponent in players)
        {
            if (opponent.Id != seat && opponent.VictoryPoints > best)
            {
                best = opponent.VictoryPoints;
            }
        }
        return best;
    }
}
