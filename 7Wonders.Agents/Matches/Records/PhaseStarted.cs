using _7Wonders.Engine.Games.Enums;

namespace _7Wonders.Agents.Matches.Records;

/// <summary>
/// The game asks for decisions: a turn in which every seat chooses a move, or an extra move that an effect gives
/// (playing the last card of the hand, a free build from the discard pile).
/// </summary>
public sealed class PhaseStarted : MatchEntry
{
    /// <summary>
    /// <paramref name="phase"/> starts in turn <paramref name="turn"/> of era <paramref name="era"/>.
    /// </summary>
    public PhaseStarted(Phase phase, int era, int turn)
    {
        Phase = phase;
        Era = era;
        Turn = turn;
    }

    /// <summary>The decision asked for.</summary>
    public Phase Phase { get; }

    /// <summary>The era, 1 to 3.</summary>
    public int Era { get; }

    /// <summary>The turn, 0 to 20 over the whole game.</summary>
    public int Turn { get; }
}
