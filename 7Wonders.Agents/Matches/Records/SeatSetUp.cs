using _7Wonders.Engine.Wonders.Enums;

namespace _7Wonders.Agents.Matches.Records;

/// <summary>Who sits at one seat when the match starts, and on which wonder board.</summary>
public sealed class SeatSetUp
{
    /// <summary>Seat <paramref name="seat"/>'s agent and board.</summary>
    public SeatSetUp(int seat, string agentName, string configurationJson, WonderId wonder)
    {
        Seat = seat;
        AgentName = agentName;
        ConfigurationJson = configurationJson;
        Wonder = wonder;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The agent's name.</summary>
    public string AgentName { get; }

    /// <summary>The agent's settings as JSON.</summary>
    public string ConfigurationJson { get; }

    /// <summary>The seat's wonder board and side.</summary>
    public WonderId Wonder { get; }
}
