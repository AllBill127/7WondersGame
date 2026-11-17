namespace _7WondersGame.Agents.Mcts;

internal sealed class SearchStatistics
{
    public int Searches;
    public long Iterations;
    public double SearchMs;
    public double LongestMs;
    public long AllocatedBytes;
    public double LockWaitMs;
    public string? LastNote;
}
