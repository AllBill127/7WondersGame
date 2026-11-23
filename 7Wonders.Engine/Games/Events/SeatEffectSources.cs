using _7Wonders.Engine.Effects.Interfaces;

namespace _7Wonders.Engine.Games.Events;

/// <summary>A seat that has an effect, and the built cards and wonder stages it comes from.</summary>
public sealed class SeatEffectSources
{
    /// <summary>Seat <paramref name="seat"/> has the effect from <paramref name="sources"/>.</summary>
    public SeatEffectSources(int seat, IReadOnlyList<IEffectSource> sources)
    {
        Seat = seat;
        Sources = sources;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The cards and wonder stages that give the effect.</summary>
    public IReadOnlyList<IEffectSource> Sources { get; }
}
