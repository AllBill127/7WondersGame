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
/// With <see cref="MctsConfiguration.Parallelisation"/> the iterations run on several threads (see <see
/// cref="MctsParallelisation"/>).
/// </summary>
/// <remarks>
/// Selection, expansion, rollout and backup run thousands of times per decision: no LINQ, lambdas or extra
/// allocations there.
/// </remarks>
internal sealed class MctsSearch
{
    private static readonly Move Skip = new(MoveAction.Skip, null);

    private readonly MctsConfiguration _configuration;
    private readonly int _searchingSeat;
    private readonly int _playerCount;
    private readonly Game _rootGame;
    private readonly SeatKnowledge? _searchingSeatKnowledge;
    private readonly Random _random;
    private readonly Stopwatch _clock;
    // only reads the state and keeps nothing between calls, so every thread can use it
    private readonly HeuristicAgent _policy = new();
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

    /// <param name="configuration">The agent's settings.</param>
    /// <param name="searchingSeat">The searching player's seat.</param>
    /// <param name="rootGame">A copy of the game, which the search doesn't change.</param>
    /// <param name="knowledge">
    /// What the player knows about the hidden cards (for <see cref="MctsHiddenInformation.Determinized"/>).
    /// </param>
    /// <param name="random">The searching player's random stream.</param>
    /// <param name="clock">Started when the decision began; a time budget counts from it.</param>
    public MctsSearch(MctsConfiguration configuration, int searchingSeat, Game rootGame, SeatKnowledge? knowledge,
                      Random random, Stopwatch clock)
        : this(configuration, searchingSeat, rootGame, knowledge, random, clock, configuration.IterationBudget)
    {
    }

    private MctsSearch(MctsConfiguration configuration, int searchingSeat, Game rootGame, SeatKnowledge? knowledge,
                       Random random, Stopwatch clock, int budget)
    {
        _configuration = configuration;
        _searchingSeat = searchingSeat;
        _playerCount = rootGame.Players.Count;
        _rootGame = rootGame;
        _searchingSeatKnowledge = knowledge;
        _random = random;
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
    public double LockWaitMilliseconds
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
            return _configuration.Parallelisation != MctsParallelisation.None && _configuration.ParallelWorkerCount > 1;
        }
    }

    /// <summary>Searches until the budget is used up (at least one iteration) and returns the move to play.</summary>
    public Move Run()
    {
        if (!IsParallel)
        {
            RunSerial();
        }
        else if (_configuration.Parallelisation == MctsParallelisation.Root)
        {
            RunRoot();
        }
        else if (_configuration.Parallelisation == MctsParallelisation.Leaf)
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
        if (_configuration.BudgetKind == MctsBudgetKind.Iterations)
        {
            return _iterations < _budget;
        }
        return _clock.ElapsedMilliseconds < _configuration.TimeBudgetMilliseconds;
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
        Game searchGame = Sample(_rootGame, _random);
        List<(Node Node, List<(int Seat, Move Move)> Moves)> path = Descend(searchGame, _random);
        Backup(path, Rollout(searchGame, _random));
    }

    /// <summary>A copy of the game to search: a guess at the hidden cards, unless the search may see them.</summary>
    private Game Sample(Game rootGame, Random random)
    {
        if (_configuration.HiddenInformation == MctsHiddenInformation.PerfectInformation)
        {
            return rootGame.DeepCopy();
        }
        return Determinization.Sample(rootGame, _searchingSeat, _searchingSeatKnowledge, random);
    }

    /// <summary>
    /// Selection and expansion: walks down from the root, making the tree policy's moves in
    /// <paramref name="searchGame"/>, until it adds a new node (or the game ends). Returns the moves made at each node.
    /// </summary>
    private List<(Node Node, List<(int Seat, Move Move)> Moves)> Descend(Game searchGame, Random random)
    {
        List<(Node Node, List<(int Seat, Move Move)> Moves)> path = new();
        Node node = _tree;
        while (searchGame.Phase != Phase.GameOver)
        {
            List<(int Seat, Move Move)> moves = ChooseMoves(node, searchGame, random);
            path.Add((node, moves));
            foreach ((int seat, Move move) in moves)
            {
                searchGame.Submit(seat, move);
            }

            MoveKey key = new(moves);
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
    private void Backup(List<(Node Node, List<(int Seat, Move Move)> Moves)> path,
                        (double[] Reward, double MyWin) result)
    {
        double[] reward = result.Reward;
        double myWin = result.MyWin;
        foreach ((Node node, List<(int Seat, Move Move)> moves) in path)
        {
            foreach ((int Seat, Move Move) seatAndMove in moves)
            {
                MoveStatistics moveStatistics = node.Statistics[seatAndMove];
                moveStatistics.Visits++;
                for (int seat = 0; seat < _playerCount; seat++)
                {
                    moveStatistics.Total[seat] += reward[seat];
                }
                moveStatistics.Wins += myWin;
            }
        }
    }

    // =========== parallel search ===========

    /// <summary>
    /// A new random stream for a worker, drawn from the search's own (so the same seed gives the same streams).
    /// </summary>
    private Random NewStream()
    {
        ulong seed = Seeding.Mix((ulong)_random.NextInt64());
        return Seeding.CreateRandom(seed);
    }

    /// <summary>
    /// Runs work(0..count-1) on <see cref="MctsConfiguration.ThreadLimit"/> threads (one after another for 1), and
    /// counts the memory the other threads allocate.
    /// </summary>
    private void RunWorkers(int count, Action<int> work)
    {
        int threads = ThreadsFor(count);
        if (threads == 1)
        {
            for (int worker = 0; worker < count; worker++)
            {
                work(worker);
            }
            return;
        }
        WorkerThreads workerThreads = new(work);
        workerThreads.Run(count, threads);
        _otherThreadBytes += workerThreads.OtherThreadBytes;
    }

    /// <summary>The threads to run this many workers on: the configured number, at most one per worker.</summary>
    private int ThreadsFor(int workerCount)
    {
        if (_configuration.ThreadLimit > 0)
        {
            return Math.Min(_configuration.ThreadLimit, workerCount);
        }
        return workerCount;
    }

    /// <summary>
    /// Root parallelisation: ParallelWorkerCount separate searches, each with its own copy of the root, random stream,
    /// tree and share of the iterations; then their root statistics are added up, and the move is chosen from the sum.
    /// </summary>
    private void RunRoot()
    {
        int workerCount = _configuration.ParallelWorkerCount;
        // made here, one after another, so each worker gets the same copy and stream whatever the thread timing
        _rootWorkers = new MctsSearch[workerCount];
        for (int worker = 0; worker < workerCount; worker++)
        {
            int share = IterationShare(worker, workerCount);
            Game workerRootGame = _rootGame.DeepCopy();
            _rootWorkers[worker] = new MctsSearch(_configuration, _searchingSeat, workerRootGame,
                                                  _searchingSeatKnowledge, NewStream(), _clock, share);
        }

        RunWorkers(workerCount, RunRootWorker);

        Node merged = new();
        foreach (MctsSearch rootWorker in _rootWorkers)
        {
            AddRootStatistics(merged, rootWorker._tree);
            _iterations += rootWorker._iterations;
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
        foreach ((int Seat, Move Move) key in workerRoot.Order)
        {
            MoveStatistics workerMoveStatistics = workerRoot.Statistics[key];
            if (!merged.Statistics.TryGetValue(key, out MoveStatistics? sum))
            {
                sum = new MoveStatistics(_playerCount);
                merged.Statistics[key] = sum;
                merged.Order.Add(key);
            }
            sum.Visits += workerMoveStatistics.Visits;
            sum.Available += workerMoveStatistics.Available;
            sum.Wins += workerMoveStatistics.Wins;
            for (int seat = 0; seat < _playerCount; seat++)
            {
                sum.Total[seat] += workerMoveStatistics.Total[seat];
            }
        }
    }

    /// <summary>
    /// Leaf parallelisation: one tree; each iteration walks down and expands as usual, then plays ParallelWorkerCount
    /// rollouts from the new node at once (each on its own copy and stream) and backs up every one of them.
    /// </summary>
    private void RunLeaf()
    {
        int workerCount = _configuration.ParallelWorkerCount;
        _workerGames = new Game[workerCount];
        _workerStreams = new Random[workerCount];
        _leafResults = new (double[] Reward, double MyWin)[workerCount];
        do
        {
            Game searchGame = Sample(_rootGame, _random);
            List<(Node Node, List<(int Seat, Move Move)> Moves)> path = Descend(searchGame, _random);
            for (int worker = 0; worker < workerCount; worker++)
            {
                if (worker == 0)
                {
                    _workerGames[worker] = searchGame;
                }
                else
                {
                    _workerGames[worker] = searchGame.DeepCopy();
                }
                _workerStreams[worker] = NewStream();
            }

            RunWorkers(workerCount, RunLeafRollout);

            foreach ((double[] Reward, double MyWin) result in _leafResults)
            {
                Backup(path, result);
            }
            _iterations += workerCount;
        }
        while (More());
    }

    private void RunLeafRollout(int worker)
    {
        _leafResults[worker] = Rollout(_workerGames[worker], _workerStreams[worker]);
    }

    /// <summary>
    /// Tree parallelisation: ParallelWorkerCount threads share one tree. Walking down (and expanding) and backing up
    /// are done holding one lock; the rollout, which takes most of the time, outside it. With
    /// <see cref="MctsConfiguration.VirtualLossVisits"/>, a thread's path counts as lost until its rollout is backed
    /// up.
    /// </summary>
    private void RunTree()
    {
        int workerCount = _configuration.ParallelWorkerCount;
        _workerGames = new Game[workerCount];
        _workerStreams = new Random[workerCount];
        for (int worker = 0; worker < workerCount; worker++)
        {
            _workerGames[worker] = _rootGame.DeepCopy();
            _workerStreams[worker] = NewStream();
        }
        _treeIterationsStarted = 0;

        RunWorkers(workerCount, RunTreeWorker);
    }

    /// <summary>
    /// One Tree worker: iterations on its own copy of the root and stream, until the budget is used up.
    /// </summary>
    private void RunTreeWorker(int worker)
    {
        Game rootGame = _workerGames[worker];
        Random random = _workerStreams[worker];
        bool first = true;
        while (MayStartTreeIteration(worker, first))
        {
            first = false;
            RunTreeIteration(rootGame, random);
        }
    }

    /// <summary>
    /// Whether a Tree worker may start another iteration. The first worker always makes one, so even a tiny time
    /// budget gives a move.
    /// </summary>
    private bool MayStartTreeIteration(int worker, bool first)
    {
        if (_configuration.BudgetKind == MctsBudgetKind.Iterations)
        {
            int started = Interlocked.Increment(ref _treeIterationsStarted);
            return started <= Math.Max(1, _budget);
        }
        if (_clock.ElapsedMilliseconds < _configuration.TimeBudgetMilliseconds)
        {
            return true;
        }
        return worker == 0 && first;
    }

    /// <summary>One Tree iteration: walks down holding the lock, rolls out without it, backs up holding it.</summary>
    private void RunTreeIteration(Game rootGame, Random random)
    {
        Game searchGame = Sample(rootGame, random);
        List<(Node Node, List<(int Seat, Move Move)> Moves)> path = DescendHoldingLock(searchGame, random);
        (double[] Reward, double MyWin) result = Rollout(searchGame, random);
        BackupHoldingLock(path, result);
    }

    private List<(Node Node, List<(int Seat, Move Move)> Moves)> DescendHoldingLock(Game searchGame, Random random)
    {
        EnterTree();
        try
        {
            List<(Node Node, List<(int Seat, Move Move)> Moves)> path = Descend(searchGame, random);
            AddVirtualLoss(path, _configuration.VirtualLossVisits);
            return path;
        }
        finally
        {
            Monitor.Exit(_treeLock);
        }
    }

    private void BackupHoldingLock(List<(Node Node, List<(int Seat, Move Move)> Moves)> path,
                                   (double[] Reward, double MyWin) result)
    {
        EnterTree();
        try
        {
            AddVirtualLoss(path, -_configuration.VirtualLossVisits);
            Backup(path, result);
            _iterations++;
        }
        finally
        {
            Monitor.Exit(_treeLock);
        }
    }

    /// <summary>
    /// Takes the tree's lock, adding the time spent waiting for it to <see cref="LockWaitMilliseconds"/>.
    /// </summary>
    private void EnterTree()
    {
        long start = Stopwatch.GetTimestamp();
        Monitor.Enter(_treeLock);
        long waited = Stopwatch.GetTimestamp() - start;
        Interlocked.Add(ref _lockWaitTicks, waited);
    }

    /// <summary>Adds (or with a negative count takes back) visits with reward 0 on every move of the path.</summary>
    private static void AddVirtualLoss(List<(Node Node, List<(int Seat, Move Move)> Moves)> path, int count)
    {
        if (count == 0)
        {
            return;
        }
        foreach ((Node node, List<(int Seat, Move Move)> moves) in path)
        {
            foreach ((int Seat, Move Move) seatAndMove in moves)
            {
                node.Statistics[seatAndMove].Visits += count;
            }
        }
    }

    // =========== tree policy ===========

    /// <summary>The moves made at a node: one player's (sequential tree) or every pending player's (DUCT).</summary>
    private List<(int Seat, Move Move)> ChooseMoves(Node node, Game searchGame, Random random)
    {
        if (_configuration.TreeShape == MctsTreeShape.Sequential)
        {
            return ChooseSequential(node, searchGame, random);
        }
        return ChooseDuct(node, searchGame, random);
    }

    /// <summary>
    /// One player decides: the searching player whenever it is pending, otherwise the lowest pending seat.
    /// </summary>
    private List<(int Seat, Move Move)> ChooseSequential(Node node, Game searchGame, Random random)
    {
        int seat = DecidingSeat(searchGame.PendingPlayers());
        return new List<(int Seat, Move Move)> { (seat, Choose(node, searchGame, seat, random)) };
    }

    private int DecidingSeat(IReadOnlyList<int> pending)
    {
        for (int pendingIndex = 0; pendingIndex < pending.Count; pendingIndex++)
        {
            if (pending[pendingIndex] == _searchingSeat)
            {
                return _searchingSeat;
            }
        }
        return pending[0];
    }

    /// <summary>DUCT: every pending player decides, each from its own statistics.</summary>
    private List<(int Seat, Move Move)> ChooseDuct(Node node, Game searchGame, Random random)
    {
        IReadOnlyList<int> pending = searchGame.PendingPlayers();
        List<(int Seat, Move Move)> moves = new();
        // an index loop: a foreach over the interface would allocate an enumerator
        for (int pendingIndex = 0; pendingIndex < pending.Count; pendingIndex++)
        {
            int seat = pending[pendingIndex];
            moves.Add((seat, Choose(node, searchGame, seat, random)));
        }
        return moves;
    }

    /// <summary>A move not tried yet at this node (a random one), else the best by the tree policy.</summary>
    private Move Choose(Node node, Game searchGame, int seat, Random random)
    {
        List<Move> legal = LegalMovesFor(searchGame, seat);
        List<Move> untried = CountAvailableAndListUntried(node, seat, legal);
        if (untried.Count > 0)
        {
            return TryNewMove(node, seat, untried, random);
        }
        return BestByTreeValue(node, seat, legal);
    }

    /// <summary>The moves the tree considers for the seat (all legal moves, or the pruned ones).</summary>
    private List<Move> LegalMovesFor(Game searchGame, int seat)
    {
        if (_configuration.MovePruning == MctsMovePruning.RandomCard)
        {
            return searchGame.LegalMovesPruned(seat);
        }
        return searchGame.LegalMoves(seat);
    }

    /// <summary>
    /// Counts the node's tried moves as available once more, and returns the moves not tried yet, in legal order.
    /// </summary>
    private static List<Move> CountAvailableAndListUntried(Node node, int seat, List<Move> legal)
    {
        List<Move> untried = new();
        foreach (Move move in legal)
        {
            if (node.Statistics.TryGetValue((seat, move), out MoveStatistics? moveStatistics))
            {
                moveStatistics.Available++;
            }
            else
            {
                untried.Add(move);
            }
        }
        return untried;
    }

    /// <summary>Expansion: a random untried move, added to the node's statistics.</summary>
    private Move TryNewMove(Node node, int seat, List<Move> untried, Random random)
    {
        int untriedIndex = random.Next(untried.Count);
        Move move = untried[untriedIndex];
        node.Statistics[(seat, move)] = new MoveStatistics(_playerCount) { Available = 1 };
        node.Order.Add((seat, move));
        return move;
    }

    /// <summary>Selection: the legal move with the highest tree value (the first of equal ones).</summary>
    private Move BestByTreeValue(Node node, int seat, List<Move> legal)
    {
        Move best = legal[0];
        double bestValue = double.NegativeInfinity;
        foreach (Move move in legal)
        {
            double treeValue = TreeValue(node.Statistics[(seat, move)], seat);
            if (treeValue > bestValue)
            {
                best = move;
                bestValue = treeValue;
            }
        }
        return best;
    }

    /// <summary>The move's value to the deciding seat: its reward so far plus the exploration bonus.</summary>
    private double TreeValue(MoveStatistics moveStatistics, int seat)
    {
        if (moveStatistics.Visits == 0)
        {
            return double.PositiveInfinity;
        }
        double total = TotalRewardFor(moveStatistics, seat);
        double visits = moveStatistics.Visits;
        int timesAvailable = moveStatistics.Available;
        switch (_configuration.ExplorationFormula)
        {
            case MctsExplorationFormula.Smoothed:
                return total / (visits + 1)
                    + _configuration.ExplorationConstant * Math.Sqrt(Math.Log(timesAvailable + 1) / (visits + 1));
            case MctsExplorationFormula.Diminishing:
                return total / visits
                    + _configuration.ExplorationConstant / (1 + Math.Log(1 + visits))
                    * Math.Sqrt(Math.Log(timesAvailable) / visits);
            default:
                return total / visits
                    + _configuration.ExplorationConstant * Math.Sqrt(Math.Log(timesAvailable) / visits);
        }
    }

    /// <summary>The summed reward the deciding seat maximises, by the backup rule.</summary>
    private double TotalRewardFor(MoveStatistics moveStatistics, int seat)
    {
        switch (_configuration.BackpropagationRule)
        {
            case MctsBackpropagationRule.MaxN:
                return moveStatistics.Total[seat];
            case MctsBackpropagationRule.Paranoid:
                if (seat == _searchingSeat)
                {
                    return moveStatistics.Total[_searchingSeat];
                }
                return -moveStatistics.Total[_searchingSeat];
            default:
                return moveStatistics.Total[_searchingSeat];
        }
    }

    /// <summary>Of the searching player's moves at the root, the one the config says.</summary>
    private Move FinalMove()
    {
        Move? best = null;
        double bestValue = double.NegativeInfinity;
        foreach ((int Seat, Move Move) key in _tree.Order)
        {
            if (key.Seat != _searchingSeat)
            {
                continue;
            }
            double finalValue = FinalMoveValue(_tree.Statistics[key]);
            if (best == null || finalValue > bestValue)
            {
                best = key.Move;
                bestValue = finalValue;
            }
        }
        if (best == null)
        {
            throw new InvalidOperationException("The search tried no move of its own player.");
        }
        return best;
    }

    /// <summary>What the final move maximises: visits, total reward or wins.</summary>
    private double FinalMoveValue(MoveStatistics moveStatistics)
    {
        switch (_configuration.FinalMoveRule)
        {
            case MctsFinalMoveRule.HighestReward:
                return moveStatistics.Total[_searchingSeat];
            case MctsFinalMoveRule.MostWins:
                return moveStatistics.Wins;
            default:
                return moveStatistics.Visits;
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

    private bool IsSearchingPlayersMove((int Seat, Move Move) key)
    {
        return key.Seat == _searchingSeat;
    }

    private int VisitsOf((int Seat, Move Move) key)
    {
        return _tree.Statistics[key].Visits;
    }

    /// <summary>
    /// One root move for <see cref="TopMoves"/>, e.g. "Build Loom 412 visits, reward 0.61, wins 55%".
    /// </summary>
    private string DescribeRootMove((int Seat, Move Move) key)
    {
        MoveStatistics moveStatistics = _tree.Statistics[key];
        double averageReward = Average(moveStatistics.Total[_searchingSeat], moveStatistics.Visits);
        double averageWins = Average(moveStatistics.Wins, moveStatistics.Visits);
        return string.Create(CultureInfo.InvariantCulture,
            $"{key.Move.Action} {CardNameOrDash(key.Move)} {moveStatistics.Visits} visits, " +
            $"reward {averageReward:0.##}, wins {averageWins:0%}");
    }

    private static string CardNameOrDash(Move move)
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
    private (double[] Reward, double MyWin) Rollout(Game searchGame, Random random)
    {
        int era = searchGame.Era;
        bool eraEnd = _configuration.RolloutEnd == MctsRolloutEnd.EraEnd;
        List<(int Seat, Move Move)> moves = new(_playerCount);
        while (searchGame.Phase != Phase.GameOver && !(eraEnd && searchGame.Era != era))
        {
            // everyone chooses from the same state, as in a real game
            moves.Clear();
            IReadOnlyList<int> pending = searchGame.PendingPlayers();
            for (int pendingIndex = 0; pendingIndex < pending.Count; pendingIndex++)
            {
                int seat = pending[pendingIndex];
                moves.Add((seat, RolloutMove(searchGame, seat, random)));
            }
            foreach ((int seat, Move move) in moves)
            {
                searchGame.Submit(seat, move);
            }
        }
        return Score(searchGame);
    }

    private Move RolloutMove(Game searchGame, int seat, Random random)
    {
        PlayerState player = searchGame.Players[seat];
        if (IsRandomRolloutMove(random))
        {
            return RandomMove(searchGame, player, random);
        }
        return _policy.Choose(player, searchGame.Phase, searchGame.Era, searchGame.DiscardPile);
    }

    /// <summary>Whether this rollout move is a random one (always, never, or with probability ε).</summary>
    private bool IsRandomRolloutMove(Random random)
    {
        if (_configuration.RolloutPolicy == MctsRolloutPolicy.Random)
        {
            return true;
        }
        if (_configuration.RolloutPolicy == MctsRolloutPolicy.HeuristicWithRandomMoves)
        {
            return random.NextDouble() < _configuration.RandomRolloutMoveProbability;
        }
        return false;
    }

    /// <summary>
    /// A random action the player can take, then a random card for it (as <see cref="RandomAgent"/>).
    /// </summary>
    private static Move RandomMove(Game searchGame, PlayerState player, Random random)
    {
        if (searchGame.Phase == Phase.DiscardPileBuild)
        {
            return RandomDiscardPileBuild(searchGame, player, random);
        }
        List<Card> hand = player.Hand;
        if (hand.Count == 0)
        {
            return Skip;
        }

        List<Card> buildable = player.BuildableCards(hand);
        List<Card>? free = null;
        if (searchGame.Phase == Phase.ChooseMoves && player.OlympiaFreeBuildAvailable)
        {
            free = player.BuildableCards(hand, everyCardFree: true);
        }
        List<MoveAction> options = new(4);
        if (buildable.Count > 0)
        {
            options.Add(MoveAction.Build);
        }
        if (free != null && free.Count > 0)
        {
            options.Add(MoveAction.BuildWithOlympiaFreeBuild);
        }
        if (player.CanBuildWonder())
        {
            options.Add(MoveAction.BuildWonderStage);
        }
        options.Add(MoveAction.Discard);

        int optionIndex = random.Next(options.Count);
        MoveAction action = options[optionIndex];
        List<Card> cards = CardsFor(action, hand, buildable, free);
        int cardIndex = random.Next(cards.Count);
        return new Move(action, cards[cardIndex]);
    }

    /// <summary>Halikarnassos: a random playable discard-pile card, or Skip when there is none.</summary>
    private static Move RandomDiscardPileBuild(Game searchGame, PlayerState player, Random random)
    {
        List<Card> fromPile = player.BuildableCards(searchGame.DiscardPile, everyCardFree: true);
        if (fromPile.Count == 0)
        {
            return Skip;
        }
        int cardIndex = random.Next(fromPile.Count);
        return new Move(MoveAction.Build, fromPile[cardIndex]);
    }

    /// <summary>
    /// The cards the action can take: playable ones to build, the free ones for Olympia, else the hand.
    /// </summary>
    private static List<Card> CardsFor(MoveAction action, List<Card> hand, List<Card> buildable, List<Card>? free)
    {
        switch (action)
        {
            case MoveAction.Build:
                return buildable;
            case MoveAction.BuildWithOlympiaFreeBuild:
                if (free == null)
                {
                    throw new InvalidOperationException("Olympia's free build was chosen without its free cards.");
                }
                return free;
            default:
                return hand;
        }
    }

    /// <summary>Each seat's reward and the searching player's win share, from the scores now.</summary>
    private (double[] Reward, double MyWin) Score(Game searchGame)
    {
        List<PlayerState> players = PlayersToScore(searchGame);
        foreach (PlayerState player in players)
        {
            player.CopyGuild();
            player.CalculateScore();
        }
        List<PlayerStanding> standings = GameResult.Rank(players);

        double[] reward = new double[_playerCount];
        for (int seat = 0; seat < _playerCount; seat++)
        {
            reward[seat] = RewardFor(seat, players, standings);
        }
        return (reward, standings[_searchingSeat].WinShare);
    }

    /// <summary>
    /// The players to score: the game is a throwaway copy at the end; mid-game (era-end cutoff) a copy of the table,
    /// so the game can't change.
    /// </summary>
    private static List<PlayerState> PlayersToScore(Game searchGame)
    {
        if (searchGame.Phase == Phase.GameOver)
        {
            return searchGame.Players;
        }
        return PlayerState.CloneTable(searchGame.Players);
    }

    /// <summary>The seat's reward of the kind the config says.</summary>
    private double RewardFor(int seat, List<PlayerState> players, List<PlayerStanding> standings)
    {
        int victoryPoints = players[seat].VictoryPoints;
        switch (_configuration.RewardKind)
        {
            case MctsRewardKind.VictoryPointMargin:
                return victoryPoints - BestOpponentVictoryPoints(players, seat);
            case MctsRewardKind.RawVictoryPoints:
                return victoryPoints;
            default:
                return standings[seat].WinShare;
        }
    }

    private static int BestOpponentVictoryPoints(List<PlayerState> players, int seat)
    {
        int best = int.MinValue;
        foreach (PlayerState opponent in players)
        {
            if (opponent.Seat != seat && opponent.VictoryPoints > best)
            {
                best = opponent.VictoryPoints;
            }
        }
        return best;
    }
}
