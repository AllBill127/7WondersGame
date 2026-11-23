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
            // Interlocked.Read reads all 64 bits in one step, so it never sees half of a value a worker is adding.
            return Interlocked.Read(ref _otherThreadBytes);
        }
    }

    /// <summary>
    /// Runs workers 0 to <paramref name="workerCount"/> - 1, at most <paramref name="threads"/> at a time.
    /// </summary>
    public void Run(int workerCount, int threads)
    {
        // Parallel.For calls RunWorker once for each number from 0 to workerCount - 1, spreading the calls over
        // thread-pool threads (at most `threads` at once) and returning when every call has finished. The calling
        // thread runs some of the workers itself.
        ParallelOptions options = new() { MaxDegreeOfParallelism = threads };
        System.Threading.Tasks.Parallel.For(0, workerCount, options, RunWorker);
    }

    private void RunWorker(int worker)
    {
        // GetAllocatedBytesForCurrentThread counts the bytes this thread has ever allocated, so the difference is
        // what this worker allocated. Workers on other threads add theirs with Interlocked.Add, which adds as one
        // indivisible step: two workers adding at the same moment both count (a plain `+=` could lose one).
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        _work(worker);
        if (Environment.CurrentManagedThreadId != _callerThread)
        {
            long allocatedByWorker = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Interlocked.Add(ref _otherThreadBytes, allocatedByWorker);
        }
    }
}
