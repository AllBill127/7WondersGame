using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.RandomChoice;

namespace _7WondersGame.Agents.Mcts.Configuration.Enums;

/// <summary>How the players move in a rollout.</summary>
public enum MctsRollout
{
    /// <summary><see cref="HeuristicAI"/>'s move, or with probability ε a random move.</summary>
    EpsilonHeuristic,
    /// <summary>Always <see cref="HeuristicAI"/>'s move (deterministic, like the old DDK rollouts).</summary>
    Heuristic,
    /// <summary>A random action, then a random card for it (as <see cref="RandomChoiceAI"/>).</summary>
    Random,
}
