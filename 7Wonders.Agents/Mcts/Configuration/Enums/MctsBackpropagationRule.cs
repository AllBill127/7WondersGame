namespace _7Wonders.Agents.Mcts.Configuration.Enums;

/// <summary>Whose reward a player maximises when choosing in the tree.</summary>
public enum MctsBackpropagationRule
{
    /// <summary>max^n: every player maximises its own reward.</summary>
    MaxN,
    /// <summary>The opponents minimise the searching player's reward.</summary>
    Paranoid,
    /// <summary>Every player maximises the searching player's reward (the old v3-v12, "cooperative").</summary>
    OwnScore,
}
