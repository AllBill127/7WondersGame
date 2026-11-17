using _7WondersGame.Agents.Common.Settings;
using _7WondersGame.Agents.Mcts.Configuration.Enums;

namespace _7WondersGame.Agents.Mcts.Configuration;

/// <summary>
/// Every setting of <see cref="MctsAgent"/> (fix plan step 6, E + D1). Each is a setting on the command line,
/// e.g. <c>MctsAgent:C=1.2:Backup=Paranoid</c>, and is logged in the results (configJson). The defaults are the
/// fix plan's; C is still to be tuned (in [0.3, 2]).
/// </summary>
[SettingsGroup]
public sealed record MctsConfig
{
    public MctsInformation Information { get; set; } = MctsInformation.Determinized;
    public MctsTree Tree { get; set; } = MctsTree.Sequential;
    public MctsBackup Backup { get; set; } = MctsBackup.MaxN;
    public MctsReward Reward { get; set; } = MctsReward.Win;
    public MctsExploration Exploration { get; set; } = MctsExploration.Ucb1;

    /// <summary>The exploration constant.</summary>
    public double C { get; set; } = 0.7;

    public MctsFinalMove FinalMove { get; set; } = MctsFinalMove.MostVisits;
    public MctsRollout Rollout { get; set; } = MctsRollout.EpsilonHeuristic;

    /// <summary>For <see cref="MctsRollout.EpsilonHeuristic"/>: the chance of a random move.</summary>
    public double Epsilon { get; set; } = 0.1;

    public MctsRolloutEnd RolloutEnd { get; set; } = MctsRolloutEnd.GameEnd;
    public MctsPruning Pruning { get; set; } = MctsPruning.None;
    public MctsBudget Budget { get; set; } = MctsBudget.Iterations;

    /// <summary>Iterations per decision (selection, expansion, one rollout, backup), for <see cref="MctsBudget.Iterations"/>.</summary>
    public int Iterations { get; set; } = 1000;

    /// <summary>Milliseconds per decision, for <see cref="MctsBudget.Time"/>.</summary>
    public int TimeMs { get; set; } = 1000;

    public MctsParallel Parallel { get; set; } = MctsParallel.None;

    /// <summary>
    /// For <see cref="Parallel"/>: the trees (Root), rollouts per new node (Leaf) or threads sharing the tree (Tree).
    /// <see cref="Iterations"/> is the total over all of them (every rollout counts as an iteration).
    /// </summary>
    public int Workers { get; set; } = 1;

    /// <summary>
    /// Threads used for the workers; 0 = one per worker. 1 runs Root's trees or Leaf's rollouts one after another,
    /// which gives the same moves as with threads (to compare strength without threads).
    /// </summary>
    public int Threads { get; set; } = 0;

    /// <summary>
    /// For <see cref="MctsParallel.Tree"/>: while a thread is in a rollout, every move on its path counts this many
    /// extra visits with reward 0, so the other threads try other paths. 0 = none.
    /// </summary>
    public int VirtualLoss { get; set; } = 0;
}
