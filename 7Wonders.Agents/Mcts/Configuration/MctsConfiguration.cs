using _7Wonders.Agents.Common.Settings;
using _7Wonders.Agents.Mcts.Configuration.Enums;

namespace _7Wonders.Agents.Mcts.Configuration;

/// <summary>
/// Every setting of <see cref="MctsAgent"/> (its <see cref="MctsAgent.Configuration"/> is a setting group). Each
/// is a setting on the command line, e.g.
/// <c>MctsAgent:ExplorationConstant=1.2:BackpropagationRule=Paranoid</c>, and is logged in the results
/// (configurationJson). The exploration constant is still to be tuned (in [0.3, 2]).
/// </summary>
internal sealed class MctsConfiguration
{
    /// <summary>What the search may know about the hidden cards.</summary>
    [Setting]
    public MctsHiddenInformation HiddenInformation { get; set; } = MctsHiddenInformation.Determinized;
    /// <summary>How the tree handles moves made at the same time.</summary>
    [Setting]
    public MctsTreeShape TreeShape { get; set; } = MctsTreeShape.Sequential;
    /// <summary>Whose reward a player maximises when choosing in the tree.</summary>
    [Setting]
    public MctsBackpropagationRule BackpropagationRule { get; set; } = MctsBackpropagationRule.MaxN;
    /// <summary>The reward of a finished rollout.</summary>
    [Setting]
    public MctsRewardKind RewardKind { get; set; } = MctsRewardKind.Win;
    /// <summary>The tree policy's formula.</summary>
    [Setting]
    public MctsExplorationFormula ExplorationFormula { get; set; } = MctsExplorationFormula.Ucb1;

    /// <summary>The exploration constant.</summary>
    [Setting]
    public double ExplorationConstant { get; set; } = 0.7;

    /// <summary>Which of the searching player's moves is played after the search.</summary>
    [Setting]
    public MctsFinalMoveRule FinalMoveRule { get; set; } = MctsFinalMoveRule.MostVisits;
    /// <summary>How the players move in a rollout.</summary>
    [Setting]
    public MctsRolloutPolicy RolloutPolicy { get; set; } = MctsRolloutPolicy.HeuristicWithRandomMoves;

    /// <summary>For <see cref="MctsRolloutPolicy.HeuristicWithRandomMoves"/>: the chance of a random move.</summary>
    [Setting]
    public double RandomRolloutMoveProbability { get; set; } = 0.1;

    /// <summary>Where a rollout stops.</summary>
    [Setting]
    public MctsRolloutEnd RolloutEnd { get; set; } = MctsRolloutEnd.GameEnd;
    /// <summary>Which moves the tree considers.</summary>
    [Setting]
    public MctsMovePruning MovePruning { get; set; } = MctsMovePruning.None;
    /// <summary>When the search stops.</summary>
    [Setting]
    public MctsBudgetKind BudgetKind { get; set; } = MctsBudgetKind.Iterations;

    /// <summary>
    /// Iterations per decision (selection, expansion, one rollout, backup), for <see
    /// cref="MctsBudgetKind.Iterations"/>.
    /// </summary>
    [Setting]
    public int IterationBudget { get; set; } = 1000;

    /// <summary>Milliseconds per decision, for <see cref="MctsBudgetKind.Time"/>.</summary>
    [Setting]
    public int TimeBudgetMilliseconds { get; set; } = 1000;

    /// <summary>How one search uses several threads.</summary>
    [Setting]
    public MctsParallelisation Parallelisation { get; set; } = MctsParallelisation.None;

    /// <summary>
    /// For <see cref="Parallelisation"/>: the trees (Root), rollouts per new node (Leaf) or threads sharing the tree
    /// (Tree). <see cref="IterationBudget"/> is the total over all of them (every rollout counts as an iteration).
    /// </summary>
    [Setting]
    public int ParallelWorkerCount { get; set; } = 1;

    /// <summary>
    /// Threads used for the workers; 0 = one per worker. 1 runs Root's trees or Leaf's rollouts one after another,
    /// which gives the same moves as with threads (to compare strength without threads).
    /// </summary>
    [Setting]
    public int ThreadLimit { get; set; } = 0;

    /// <summary>
    /// For <see cref="MctsParallelisation.Tree"/>: while a thread is in a rollout, every move on its path counts this
    /// many extra visits with reward 0, so the other threads try other paths. 0 = none.
    /// </summary>
    [Setting]
    public int VirtualLossVisits { get; set; } = 0;
}
