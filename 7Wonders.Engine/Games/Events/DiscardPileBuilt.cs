using _7Wonders.Engine.Cards.Entities;

namespace _7Wonders.Engine.Games.Events;

/// <summary>A seat builds a card from the discard pile for free (Halikarnassos).</summary>
public sealed class DiscardPileBuilt : GameEvent
{
    /// <summary>The event with these values.</summary>
    public DiscardPileBuilt(int seat, Card card, int coinsBefore, int coinsAfter)
    {
        Seat = seat;
        Card = card;
        CoinsBefore = coinsBefore;
        CoinsAfter = coinsAfter;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The card built.</summary>
    public Card Card { get; }

    /// <summary>The seat's coins before the build.</summary>
    public int CoinsBefore { get; }

    /// <summary>The seat's coins after the build: more if the card gives coins.</summary>
    public int CoinsAfter { get; }
}
