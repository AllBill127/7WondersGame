using _7WondersGame.Engine.Games.Entities;

namespace _7WondersGame.Agents.Mcts.Configuration.Enums;

/// <summary>Which moves the tree considers.</summary>
public enum MctsPruning
{
    /// <summary>Every legal move.</summary>
    None,
    /// <summary>
    /// One wonder-stage move and one discard, each with a random hand card (<see cref="Game.LegalMovesPruned"/>).
    /// </summary>
    RandomCard,
}
