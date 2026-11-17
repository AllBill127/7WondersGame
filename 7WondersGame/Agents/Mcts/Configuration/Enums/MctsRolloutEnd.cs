namespace _7WondersGame.Agents.Mcts.Configuration.Enums;

/// <summary>Where a rollout stops (M8).</summary>
public enum MctsRolloutEnd
{
    /// <summary>At the end of the game.</summary>
    GameEnd,
    /// <summary>At the end of the era it started in; the reward uses the scores at that point.</summary>
    EraEnd,
}
