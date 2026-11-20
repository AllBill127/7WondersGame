using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Moves.Enums;

namespace _7Wonders.Engine.Moves.Dtos;

/// <summary>
/// A move: an action and the card it uses (null only for <see cref="MoveAction.Skip"/>).
/// Two moves are equal when their action and card (compared by Id) are equal: the search tree finds its moves by
/// this, and the legal moves are de-duplicated with it.
/// </summary>
public sealed class Move : IEquatable<Move>
{
    /// <summary>The move <paramref name="action"/> with <paramref name="card"/>.</summary>
    public Move(MoveAction action, Card? card)
    {
        Action = action;
        Card = card;
    }

    /// <summary>What the player does.</summary>
    public MoveAction Action { get; }

    /// <summary>The card it uses; null only for <see cref="MoveAction.Skip"/>.</summary>
    public Card? Card { get; }

    /// <inheritdoc/>
    public bool Equals(Move? other)
    {
        if (other is null)
        {
            return false;
        }
        // the same comparison the record made: Card's own Equals (by Id), without boxing through object
        return Action == other.Action && EqualityComparer<Card?>.Default.Equals(Card, other.Card);
    }

    /// <inheritdoc/>
    public override bool Equals(object? otherObject)
    {
        return Equals(otherObject as Move);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Action, Card);
    }

    /// <summary>Whether both are the same move.</summary>
    public static bool operator ==(Move? left, Move? right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }

    /// <summary>Whether the moves differ.</summary>
    public static bool operator !=(Move? left, Move? right)
    {
        return !(left == right);
    }

    /// <summary>The action, and the card's name if there is one, e.g. <c>Build Loom</c>.</summary>
    public override string ToString()
    {
        if (Card == null)
        {
            return Action.ToString();
        }
        return $"{Action} {Card.Name}";
    }
}
