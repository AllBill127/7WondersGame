namespace _7WondersGame.Agents.Mcts.Configuration.Enums;

/// <summary>When the search stops.</summary>
public enum MctsBudget
{
    /// <summary>After <see cref="MctsConfig.Iterations"/> iterations; every iteration counts. Reproducible.</summary>
    Iterations,
    /// <summary>
    /// After <see cref="MctsConfig.TimeMs"/> milliseconds. Not reproducible: it depends on the machine.
    /// </summary>
    Time,
}
