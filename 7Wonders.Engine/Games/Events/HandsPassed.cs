namespace _7Wonders.Engine.Games.Events;

/// <summary>The hands are passed to the next seat for the next turn.</summary>
public sealed class HandsPassed : GameEvent
{
    /// <summary>The event with these values.</summary>
    public HandsPassed(bool passedWest)
    {
        PassedWest = passedWest;
    }

    /// <summary>True if the hands went west (eras I and III), false if east (era II).</summary>
    public bool PassedWest { get; }
}
