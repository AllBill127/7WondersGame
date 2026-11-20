namespace _7Wonders.Engine.Games.Events;

/// <summary>
/// An era starts: the first when the game is first played (its hands are dealt with the setup), the others before
/// their hands are dealt.
/// </summary>
public sealed class EraStarted : GameEvent
{
    /// <summary>The event with these values.</summary>
    public EraStarted(int era, IReadOnlyList<SeatEffectSources> freeBuilds)
    {
        Era = era;
        FreeBuilds = freeBuilds;
    }

    /// <summary>The era, 1 to 3.</summary>
    public int Era { get; }

    /// <summary>
    /// The seats that may build one card for free this era (Olympia A), with the cards and stages that let them.
    /// </summary>
    public IReadOnlyList<SeatEffectSources> FreeBuilds { get; }
}
