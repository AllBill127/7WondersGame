namespace _7Wonders.Agents.Mcts.Tree;

/// <summary>
/// The moves made at a node (one in a sequential tree, everyone pending in DUCT): the key of the child.
/// A <c>readonly struct</c>: a value that allocates nothing itself and never changes after it is made.
/// </summary>
internal readonly struct MoveKey : IEquatable<MoveKey>
{
    private readonly SeatMove[] _moves;

    /// <summary>A key of the moves, in their order (the list is copied).</summary>
    public MoveKey(List<SeatMove> moves)
    {
        _moves = moves.ToArray();
    }

    /// <summary>Whether both keys have the same moves in the same order.</summary>
    public bool Equals(MoveKey other)
    {
        if (_moves.Length != other._moves.Length)
        {
            return false;
        }
        for (int moveIndex = 0; moveIndex < _moves.Length; moveIndex++)
        {
            if (!_moves[moveIndex].Equals(other._moves[moveIndex]))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? otherObject)
    {
        // `is MoveKey otherKey` tests the type and, if it matches, names the object as a MoveKey.
        return otherObject is MoveKey otherKey && Equals(otherKey);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Keys with the same moves in the same order must give the same hash: HashCode mixes each move's hash into
        // the total in turn, and ToHashCode gives the result. A node finds its child in a dictionary by this.
        HashCode hash = new();
        foreach (SeatMove move in _moves)
        {
            hash.Add(move);
        }
        return hash.ToHashCode();
    }
}
