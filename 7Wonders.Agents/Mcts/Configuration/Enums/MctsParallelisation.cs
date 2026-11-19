namespace _7Wonders.Agents.Mcts.Configuration.Enums;

/// <summary>
/// How one search uses several threads (<see cref="MctsConfiguration.ParallelWorkerCount"/> of them). The variants are
/// the classic ones (Chaslot, Winands and van den Herik 2008, "Parallel Monte-Carlo Tree Search").
/// </summary>
public enum MctsParallelisation
{
    /// <summary>One tree, one thread: the plain search (ParallelWorkerCount is ignored).</summary>
    None,
    /// <summary>
    /// Root parallelisation: ParallelWorkerCount separate trees, each with its own copy of the game, random stream and
    /// guesses at the hidden cards; at the end their root statistics are added up. Same results whatever the threads
    /// do.
    /// </summary>
    Root,
    /// <summary>
    /// Leaf parallelisation: one tree; from each new node ParallelWorkerCount rollouts at once. Same results whatever
    /// the threads do.
    /// </summary>
    Leaf,
    /// <summary>
    /// Tree parallelisation: one tree shared by Workers threads, with one lock around walking down and updating it
    /// (rollouts run outside the lock). See <see cref="MctsConfiguration.VirtualLossVisits"/>. Results depend on thread
    /// timing.
    /// </summary>
    Tree,
}
