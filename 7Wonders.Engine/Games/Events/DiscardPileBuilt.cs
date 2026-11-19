using _7Wonders.Engine.Cards.Entities;

namespace _7Wonders.Engine.Games.Events;

/// <summary>Halikarnassos builds a card from the discard pile for free.</summary>
public sealed class DiscardPileBuilt : GameEvent
{
    /// <summary>The event with these values.</summary>
    public DiscardPileBuilt(int seat, Card card)
    {
        Seat = seat;
        Card = card;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The card built.</summary>
    public Card Card { get; }
}
