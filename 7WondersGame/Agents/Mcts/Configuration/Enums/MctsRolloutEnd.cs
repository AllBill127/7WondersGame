namespace _7Wonders.Agents.Mcts.Configuration.Enums;

/// <summary>Where a rollout stops.</summary>
public enum MctsRolloutEnd
{
    /// <summary>At the end of the game.</summary>
    GameEnd,
    /// <summary>At the end of the era it started in; the reward uses the scores at that point.</summary>
    EraEnd,
}
