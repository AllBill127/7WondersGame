namespace _7Wonders.Agents.Mcts.Configuration.Enums;

/// <summary>How the tree handles moves made at the same time.</summary>
public enum MctsTreeShape
{
    /// <summary>
    /// One player per node: the searching player first, then the others in seat order (they see its move).
    /// </summary>
    Sequential,
    /// <summary>Decoupled UCT: one node per turn, each player choosing from its own statistics.</summary>
    Duct,
}
