namespace _7WondersGame.Agents.Mcts.Configuration.Enums;

/// <summary>The tree policy (M4). Moves not tried yet are always tried first.</summary>
public enum MctsExploration
{
    /// <summary>UCB1: mean + C √(ln n / nᵢ), n = how often the move was available (ISMCTS).</summary>
    Ucb1,
    /// <summary>The old +1 smoothing: total / (nᵢ + 1) + C √(ln(n + 1) / (nᵢ + 1)).</summary>
    Smoothed,
    /// <summary>The old diminishing exploration: UCB1 with C / (1 + ln(1 + nᵢ)) in place of C.</summary>
    Diminishing,
}
