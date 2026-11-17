using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Moves.Enums;

namespace _7WondersGame.Engine.Moves.Dtos;

/// <summary>
/// A move: an action and the card it uses (null only for <see cref="ActionType.Skip"/>).
/// Two moves are equal when their action and card (compared by Id) are equal.
/// </summary>
public sealed record Command(ActionType Action, Card? Card)
{
    public override string ToString() => Card == null ? Action.ToString() : $"{Action} {Card.Name}";
}
