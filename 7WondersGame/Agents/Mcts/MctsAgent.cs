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
/// differently is a setting in <see cref="Config"/>, logged with every result row. By default it doesn't see hidden
/// cards: each iteration searches a new guess at them (SO-ISMCTS). A decision with only one legal move is made
/// without a search.
/// </summary>
public sealed class MctsAgent : IAgent
{
    private const double BytesPerMegabyte = 1048576.0;

    // what it measured over its game (an agent is made for one game); in an object, so these aren't settings
    private readonly SearchStatistics _stats = new();

    /// <summary>Its settings.</summary>
    public MctsConfig Config { get; } = new();

    /// <summary>The move a search of the view's game finds, or the only legal move without a search.</summary>
    public Command Choose(GameView view)
    {
        List<Command> legal = view.LegalMoves();
        if (legal.Count == 1)
        {
            _stats.LastNote = null;
            return legal[0];
        }

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        var search = new MctsSearch(Config, view.Seat, view.CopyGame(), view.Known, view.Random, clock);
        Command move = search.Run();
        double ms = clock.Elapsed.TotalMilliseconds;

        long allocatedHere = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        RecordSearch(search, ms, allocatedHere);
        return move;
    }

    /// <summary>Adds one search to the statistics and keeps its note for the trace.</summary>
    private void RecordSearch(MctsSearch search, double ms, long allocatedOnThisThread)
    {
        _stats.Searches++;
        _stats.Iterations += search.Iterations;
        _stats.SearchMs += ms;
        _stats.LongestMs = Math.Max(_stats.LongestMs, ms);
        _stats.AllocatedBytes += allocatedOnThisThread + search.OtherThreadBytes;
        _stats.LockWaitMs += search.LockWaitMs;
        // a few lines of text per decision, next to the search itself: cheap enough to make whether traced or not
        _stats.LastNote = string.Create(CultureInfo.InvariantCulture,
            $"{search.Iterations} iterations in {ms:0} ms; {search.TopMoves(3)}");
    }

    string? IAgent.LastChoiceNote
    {
        get
        {
            return _stats.LastNote;
        }
    }

    bool IAgent.UsesClock
    {
        get
        {
            return Config.Budget == MctsBudget.Time;
        }
    }

    /// <summary>
    /// Searches made, iterations in all, search time in milliseconds (in all and the longest), and the memory the
    /// searches allocated in MB (the garbage collector's work grows with it).
    /// </summary>
    /// <remarks>
    /// A parallel search (<see cref="MctsConfig.Parallel"/>) also gives its workers and the time its threads waited
    /// for the shared tree's lock, summed.
    /// </remarks>
    public string StatsJson()
    {
        var stats = new Dictionary<string, object>
        {
            ["searches"] = _stats.Searches,
            ["iterations"] = _stats.Iterations,
            ["searchMs"] = Math.Round(_stats.SearchMs),
            ["longestSearchMs"] = Math.Round(_stats.LongestMs),
            ["allocatedMb"] = Math.Round(_stats.AllocatedBytes / BytesPerMegabyte),
        };
        if (IsParallel())
        {
            stats["parallel"] = Config.Parallel.ToString();
            stats["workers"] = Config.Workers;
            stats["lockWaitMs"] = Math.Round(_stats.LockWaitMs);
        }
        return JsonSerializer.Serialize(stats);
    }

    private bool IsParallel()
    {
        return Config.Parallel != MctsParallel.None && Config.Workers > 1;
    }
}
