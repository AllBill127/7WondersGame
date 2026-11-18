using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.RandomChoice;

namespace _7WondersGame.Agents.Mcts.Configuration.Enums;

/// <summary>How the players move in a rollout.</summary>
public enum MctsRolloutPolicy
{
    /// <summary><see cref="HeuristicAgent"/>'s move, or with probability ε a random move.</summary>
    HeuristicWithRandomMoves,
    /// <summary>Always <see cref="HeuristicAgent"/>'s move (deterministic, like the old DDK rollouts).</summary>
    Heuristic,
    /// <summary>A random action, then a random card for it (as <see cref="RandomAgent"/>).</summary>
    Random,
}
