using _7Wonders.Engine.Cards.Entities;

namespace _7Wonders.Engine.Games.Events;

/// <summary>An era's last turn is over: the last card of every hand is discarded.</summary>
public sealed class EraEnded : GameEvent
{
    /// <summary>The event with these values.</summary>
    public EraEnded(int era, IReadOnlyList<Card> lastCards)
    {
        Era = era;
        LastCards = lastCards;
    }

    /// <summary>The era that ended.</summary>
    public int Era { get; }

    /// <summary>The cards discarded from the hands, seat by seat.</summary>
    public IReadOnlyList<Card> LastCards { get; }
}
