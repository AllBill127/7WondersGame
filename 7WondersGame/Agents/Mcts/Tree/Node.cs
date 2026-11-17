using _7WondersGame.Engine.Moves.Dtos;

namespace _7WondersGame.Agents.Mcts.Tree;

internal sealed class Node
{
    public readonly Dictionary<(int Seat, Command Move), MoveStats> Stats = new();
    /// <summary>The keys of <see cref="Stats"/> in the order they were added (for ties).</summary>
    public readonly List<(int Seat, Command Move)> Order = new();
    public readonly Dictionary<MoveKey, Node> Children = new();
}
