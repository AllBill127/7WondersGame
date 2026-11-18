using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Mcts.Configuration;
using _7WondersGame.Agents.Mcts.Configuration.Enums;
using _7WondersGame.Agents.Mcts.Search;
using _7WondersGame.Engine.Moves.Dtos;

namespace _7WondersGame.Agents.Mcts;

/// <summary>
/// The one configurable MCTS agent, which replaces the old v1-v12 and JM agents. Every choice the old versions made
/// differently is a setting in <see cref="Configuration"/>, logged with every result row. By default it doesn't see
/// hidden cards: each iteration searches a new guess at them (SO-ISMCTS). A decision with only one legal move is made
/// without a search.
/// </summary>
public sealed class MctsAgent : IAgent
{
    private const double BytesPerMegabyte = 1048576.0;

    // what it measured over its game (an agent is made for one game); in an object, so these aren't settings
    private readonly SearchStatistics _statistics = new();

    /// <summary>Its settings.</summary>
    public MctsConfiguration Configuration { get; } = new();

    /// <summary>The move a search of the view's game finds, or the only legal move without a search.</summary>
    public Move Choose(GameView view)
    {
        List<Move> legal = view.LegalMoves();
        if (legal.Count == 1)
        {
            _statistics.LastNote = null;
            return legal[0];
        }

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        var search = new MctsSearch(Configuration, view.Seat, view.CopyGame(), view.Knowledge, view.Random, clock);
        Move move = search.Run();
        double milliseconds = clock.Elapsed.TotalMilliseconds;

        long allocatedHere = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        RecordSearch(search, milliseconds, allocatedHere);
        return move;
    }

    /// <summary>Adds one search to the statistics and keeps its note for the trace.</summary>
    private void RecordSearch(MctsSearch search, double milliseconds, long allocatedOnThisThread)
    {
        _statistics.Searches++;
        _statistics.Iterations += search.Iterations;
        _statistics.SearchMilliseconds += milliseconds;
        _statistics.LongestSearchMilliseconds = Math.Max(_statistics.LongestSearchMilliseconds, milliseconds);
        _statistics.AllocatedBytes += allocatedOnThisThread + search.OtherThreadBytes;
        _statistics.LockWaitMilliseconds += search.LockWaitMs;
        // a few lines of text per decision, next to the search itself: cheap enough to make whether traced or not
        _statistics.LastNote = string.Create(CultureInfo.InvariantCulture,
            $"{search.Iterations} iterations in {milliseconds:0} ms; {search.TopMoves(3)}");
    }

    string? IAgent.LastChoiceNote
    {
        get
        {
            return _statistics.LastNote;
        }
    }

    bool IAgent.UsesClock
    {
        get
        {
            return Configuration.BudgetKind == MctsBudgetKind.Time;
        }
    }

    /// <summary>
    /// Searches made, iterations in all, search time in milliseconds (in all and the longest), and the memory the
    /// searches allocated in MB (the garbage collector's work grows with it).
    /// </summary>
    /// <remarks>
    /// A parallel search (<see cref="MctsConfiguration.Parallelisation"/>) also gives its workers and the time its
    /// threads waited for the shared tree's lock, summed.
    /// </remarks>
    public string StatisticsJson()
    {
        var statistics = new Dictionary<string, object>
        {
            ["searches"] = _statistics.Searches,
            ["iterations"] = _statistics.Iterations,
            ["searchMilliseconds"] = Math.Round(_statistics.SearchMilliseconds),
            ["longestSearchMilliseconds"] = Math.Round(_statistics.LongestSearchMilliseconds),
            ["allocatedMegabytes"] = Math.Round(_statistics.AllocatedBytes / BytesPerMegabyte),
        };
        if (IsParallel())
        {
            statistics["parallelisation"] = Configuration.Parallelisation.ToString();
            statistics["parallelWorkerCount"] = Configuration.ParallelWorkerCount;
            statistics["lockWaitMilliseconds"] = Math.Round(_statistics.LockWaitMilliseconds);
        }
        return JsonSerializer.Serialize(statistics);
    }

    private bool IsParallel()
    {
        return Configuration.Parallelisation != MctsParallelisation.None && Configuration.ParallelWorkerCount > 1;
    }
}
