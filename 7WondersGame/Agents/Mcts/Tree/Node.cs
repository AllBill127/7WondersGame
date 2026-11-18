using _7WondersGame.Engine.Moves.Dtos;

namespace _7WondersGame.Agents.Mcts.Tree;

/// <summary>A node of the search tree: the statistics of the moves made there and the nodes they lead to.</summary>
internal sealed class Node
{
    /// <summary>Each move tried here, by the seat that made it: its statistics.</summary>
    public Dictionary<(int Seat, Move Move), MoveStatistics> Statistics { get; } = new();

    /// <summary>The keys of <see cref="Statistics"/> in the order they were added (for ties).</summary>
    public List<(int Seat, Move Move)> Order { get; } = new();

    /// <summary>The child nodes, by the moves that lead to them.</summary>
    public Dictionary<MoveKey, Node> Children { get; } = new();
}
