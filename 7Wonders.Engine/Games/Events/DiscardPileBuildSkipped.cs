namespace _7Wonders.Engine.Games.Events;

/// <summary>Halikarnassos builds nothing from the discard pile.</summary>
public sealed class DiscardPileBuildSkipped : GameEvent
{
    /// <summary>The event with these values.</summary>
    public DiscardPileBuildSkipped(int seat)
    {
        Seat = seat;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }
}
