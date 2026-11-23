using _7Wonders.Engine.Games.Entities;

namespace _7Wonders.Agents.Mcts.Configuration.Enums;

/// <summary>Which moves the tree considers.</summary>
internal enum MctsMovePruning
{
    /// <summary>Every legal move.</summary>
    None,
    /// <summary>
    /// One wonder-stage move and one discard, each with a random hand card (<see cref="Game.LegalMovesPruned"/>).
    /// </summary>
    RandomCard,
}
