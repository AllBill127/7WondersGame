using _7Wonders.Engine.Moves.Dtos;

namespace _7Wonders.Agents.Mcts.Tree;

/// <summary>
/// A move and the seat that made it: the key of a move's statistics at a node. Equal when the seat and the move
/// are equal. A struct (a value copied on assignment, allocating nothing), because the search makes one for every
/// move it looks up.
/// </summary>
internal readonly struct SeatMove : IEquatable<SeatMove>
{
    /// <summary>The seat <paramref name="seat"/> making <paramref name="move"/>.</summary>
    public SeatMove(int seat, Move move)
    {
        Seat = seat;
        Move = move;
    }

    /// <summary>The seat that made the move.</summary>
    public int Seat { get; }

    /// <summary>The move.</summary>
    public Move Move { get; }

    /// <summary>Whether both have the same seat and the same move.</summary>
    public bool Equals(SeatMove other)
    {
        return Seat == other.Seat && Move.Equals(other.Move);
    }

    /// <inheritdoc/>
    public override bool Equals(object? otherObject)
    {
        return otherObject is SeatMove otherSeatMove && Equals(otherSeatMove);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Equal seat moves must give the same hash: HashCode.Combine mixes the two fields Equals compares into one
        // int. A node's statistics are a dictionary keyed by seat move.
        return HashCode.Combine(Seat, Move);
    }
}
