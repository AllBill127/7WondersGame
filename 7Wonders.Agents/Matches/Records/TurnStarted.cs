namespace _7Wonders.Agents.Matches.Records;

/// <summary>A turn in which every seat chooses a move starts.</summary>
public sealed class TurnStarted : MatchEntry
{
    /// <summary>Turn <paramref name="turn"/> of era <paramref name="era"/> starts.</summary>
    public TurnStarted(int era, int turn)
    {
        Era = era;
        Turn = turn;
    }

    /// <summary>The era, 1 to 3.</summary>
    public int Era { get; }

    /// <summary>The turn, 0 to 20 over the whole game.</summary>
    public int Turn { get; }
}
