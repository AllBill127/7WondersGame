using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Effects.Interfaces;

namespace _7Wonders.Engine.Games.Events;

/// <summary>A seat copies a neighbour's guild at the end of the game (Olympia B).</summary>
public sealed class GuildCopied : GameEvent
{
    /// <summary>The event with these values.</summary>
    public GuildCopied(int seat, Card guild, IReadOnlyList<IEffectSource> sources)
    {
        Seat = seat;
        Guild = guild;
        Sources = sources;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The guild copied.</summary>
    public Card Guild { get; }

    /// <summary>The cards and wonder stages that let the seat copy a guild.</summary>
    public IReadOnlyList<IEffectSource> Sources { get; }
}
