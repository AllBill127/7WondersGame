namespace _7Wonders.Agents.Mcts.Configuration.Enums;

/// <summary>Which of the searching player's moves is played after the search.</summary>
internal enum MctsFinalMoveRule
{
    /// <summary>The most visited (robust child).</summary>
    MostVisits,
    /// <summary>The highest total reward (a sum, as the old versions).</summary>
    HighestReward,
    /// <summary>The most wins (win shares summed), whatever the reward.</summary>
    MostWins,
}
