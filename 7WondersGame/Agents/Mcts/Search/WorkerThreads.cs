namespace _7Wonders.Agents.Mcts.Search;

/// <summary>
/// Runs a parallel search's workers on several threads and counts the memory allocated on threads other than the
/// caller's (the agent counts the caller's own).
/// </summary>
internal sealed class WorkerThreads
{
    private readonly Action<int> _work;
    private readonly int _callerThread;
    private long _otherThreadBytes;

    /// <param name="work">One worker's work, given the worker's number.</param>
    public WorkerThreads(Action<int> work)
    {
        _work = work;
        _callerThread = Environment.CurrentManagedThreadId;
    }

    /// <summary>Memory allocated by the workers that ran on threads other than the caller's.</summary>
    public long OtherThreadBytes
    {
        get
        {
            return Interlocked.Read(ref _otherThreadBytes);
        }
    }

    /// <summary>
    /// Runs workers 0 to <paramref name="workerCount"/> - 1, at most <paramref name="threads"/> at a time.
    /// </summary>
    public void Run(int workerCount, int threads)
    {
        ParallelOptions options = new() { MaxDegreeOfParallelism = threads };
        System.Threading.Tasks.Parallel.For(0, workerCount, options, RunWorker);
    }

    private void RunWorker(int worker)
    {
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        _work(worker);
        if (Environment.CurrentManagedThreadId != _callerThread)
        {
            long allocatedByWorker = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Interlocked.Add(ref _otherThreadBytes, allocatedByWorker);
        }
    }
}
