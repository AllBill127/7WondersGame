using _7Wonders.Engine.Cards.Entities;

namespace _7Wonders.Engine.Games.Events;

/// <summary>Olympia B copies a neighbour's guild at the end of the game.</summary>
public sealed class GuildCopied : GameEvent
{
    /// <summary>The event with these values.</summary>
    public GuildCopied(int seat, Card guild)
    {
        Seat = seat;
        Guild = guild;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The guild copied.</summary>
    public Card Guild { get; }
}
