namespace _7Wonders.Agents.Mcts.Configuration.Enums;

/// <summary>The reward of a finished rollout, per player.</summary>
public enum MctsRewardKind
{
    /// <summary>1 for a win, 1/k for a k-way tie for first (VP, then coins), else 0.</summary>
    Win,
    /// <summary>
    /// The player's VP minus the best opponent's VP (not scaled, so the exploration constant must be about 20
    /// times larger).
    /// </summary>
    VictoryPointMargin,
    /// <summary>The player's VP (not scaled, as the old versions).</summary>
    RawVictoryPoints,
}
