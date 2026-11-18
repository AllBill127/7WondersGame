using _7WondersGame.Engine.Moves.Dtos;

namespace _7WondersGame.Agents.Mcts.Tree;

/// <summary>
/// The moves made at a node (one in a sequential tree, everyone pending in DUCT): the key of the child.
/// </summary>
internal readonly struct MoveKey : IEquatable<MoveKey>
{
    private readonly (int Seat, Command Move)[] _moves;

    /// <summary>A key of the moves, in their order (the list is copied).</summary>
    public MoveKey(List<(int Seat, Command Move)> moves)
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
        for (int i = 0; i < _moves.Length; i++)
        {
            if (!_moves[i].Equals(other._moves[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is MoveKey k && Equals(k);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var m in _moves)
        {
            hash.Add(m);
        }
        return hash.ToHashCode();
    }
}
