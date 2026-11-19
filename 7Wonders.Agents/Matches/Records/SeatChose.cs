using _7Wonders.Engine.Games.Enums;
using _7Wonders.Engine.Moves.Dtos;

namespace _7Wonders.Agents.Matches.Records;

/// <summary>What a seat's agent chose, and why if the agent said.</summary>
public sealed class SeatChose : MatchEntry
{
    /// <summary>Seat <paramref name="seat"/>'s choice in <paramref name="phase"/>.</summary>
    public SeatChose(Phase phase, int seat, string agentName, Move move, string? note)
    {
        Phase = phase;
        Seat = seat;
        AgentName = agentName;
        Move = move;
        Note = note;
    }

    /// <summary>The decision the move answers (see <see cref="SeatShown.Phase"/>).</summary>
    public Phase Phase { get; }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The agent's name.</summary>
    public string AgentName { get; }

    /// <summary>The move chosen.</summary>
    public Move Move { get; }

    /// <summary>Why the agent chose it, in a line (e.g. a search's top moves); null if the agent said nothing.</summary>
    public string? Note { get; }
}
