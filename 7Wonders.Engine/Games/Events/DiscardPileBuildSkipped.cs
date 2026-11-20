namespace _7Wonders.Engine.Games.Events;

/// <summary>A seat that may build from the discard pile for free (Halikarnassos) builds nothing.</summary>
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
