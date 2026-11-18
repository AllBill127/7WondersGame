namespace _7WondersGame.Agents.Mcts;

/// <summary>What an <see cref="MctsAgent"/> measured over its game's searches.</summary>
internal sealed class SearchStatistics
{
    /// <summary>Searches made (decisions with more than one legal move).</summary>
    public int Searches { get; set; }

    /// <summary>Iterations made, over all searches.</summary>
    public long Iterations { get; set; }

    /// <summary>Search time in milliseconds, over all searches.</summary>
    public double SearchMs { get; set; }

    /// <summary>The longest search's time in milliseconds.</summary>
    public double LongestMs { get; set; }

    /// <summary>Memory the searches allocated, on every thread.</summary>
    public long AllocatedBytes { get; set; }

    /// <summary>Time the threads of parallel searches waited for the shared tree's lock, summed.</summary>
    public double LockWaitMs { get; set; }

    /// <summary>The last search's note for the trace; null when the last decision needed no search.</summary>
    public string? LastNote { get; set; }
}
