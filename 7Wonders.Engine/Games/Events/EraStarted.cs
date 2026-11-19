namespace _7Wonders.Engine.Games.Events;

/// <summary>An era starts, before its hands are dealt.</summary>
public sealed class EraStarted : GameEvent
{
    /// <summary>The event with these values.</summary>
    public EraStarted(int era, IReadOnlyList<int> olympiaFreeBuildSeats)
    {
        Era = era;
        OlympiaFreeBuildSeats = olympiaFreeBuildSeats;
    }

    /// <summary>The era, 2 or 3 (the first era starts with the match).</summary>
    public int Era { get; }

    /// <summary>The seats that may build one card for free this era (Olympia A).</summary>
    public IReadOnlyList<int> OlympiaFreeBuildSeats { get; }
}
