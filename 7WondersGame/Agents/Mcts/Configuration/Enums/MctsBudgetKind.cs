namespace _7Wonders.Agents.Mcts.Configuration.Enums;

/// <summary>When the search stops.</summary>
public enum MctsBudgetKind
{
    /// <summary>
    /// After <see cref="MctsConfiguration.IterationBudget"/> iterations; every iteration counts. Reproducible.
    /// </summary>
    Iterations,
    /// <summary>
    /// After <see cref="MctsConfiguration.TimeBudgetMilliseconds"/> milliseconds. Not reproducible: it depends on the
    /// machine.
    /// </summary>
    Time,
}
