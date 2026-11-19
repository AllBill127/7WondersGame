using _7WondersGame.Agents.Mcts.Tree;

namespace _7WondersGame.Agents.Mcts.Search;

/// <summary>
/// One step of the walk down the tree: a node and the moves made there (one in a sequential tree, every pending
/// player's in DUCT). A struct, because the search makes one at every node of every iteration.
/// </summary>
internal readonly struct SelectionStep
{
    /// <summary>The moves <paramref name="moves"/> made at <paramref name="node"/>.</summary>
    public SelectionStep(Node node, List<SeatMove> moves)
    {
        Node = node;
        Moves = moves;
    }

    /// <summary>The node the moves were made at.</summary>
    public Node Node { get; }

    /// <summary>The moves made at the node.</summary>
    public List<SeatMove> Moves { get; }
}
