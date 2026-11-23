using _7Wonders.Agents.Common.Interfaces;

namespace _7Wonders.Agents.Matches;

/// <summary>
/// What one seat got in a finished game. One CSV row per seat.
/// Wonder is the board name without the side (e.g. "Gizah"); Side is "A" or "B".
/// AgentStatisticsJson is what the agent measured (<see cref="IAgent.StatisticsJson"/>).
/// Two results are equal when every value is equal (a traced game must end the same as a silent one).
/// </summary>
public sealed class SeatResult : IEquatable<SeatResult>
{
    /// <summary>What seat <paramref name="seat"/> got.</summary>
    public SeatResult(int seat, string agent, string configurationJson, string wonder, string side,
                      int victoryPoints, int coins, int rank, double winShare, string agentStatisticsJson = "{}")
    {
        Seat = seat;
        Agent = agent;
        ConfigurationJson = configurationJson;
        Wonder = wonder;
        Side = side;
        VictoryPoints = victoryPoints;
        Coins = coins;
        Rank = rank;
        WinShare = winShare;
        AgentStatisticsJson = agentStatisticsJson;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The agent's name.</summary>
    public string Agent { get; }

    /// <summary>The agent's settings as JSON.</summary>
    public string ConfigurationJson { get; }

    /// <summary>The board name without the side, e.g. "Gizah".</summary>
    public string Wonder { get; }

    /// <summary>"A" or "B".</summary>
    public string Side { get; }

    /// <summary>The final victory points.</summary>
    public int VictoryPoints { get; }

    /// <summary>The coins at the end.</summary>
    public int Coins { get; }

    /// <summary>The place, 1 for the best; tied players share a place.</summary>
    public int Rank { get; }

    /// <summary>1/k for each of the k players tied for first place, otherwise 0.</summary>
    public double WinShare { get; }

    /// <summary>What the agent measured (see <see cref="IAgent.StatisticsJson"/>).</summary>
    public string AgentStatisticsJson { get; }

    /// <inheritdoc/>
    public bool Equals(SeatResult? other)
    {
        if (other is null)
        {
            return false;
        }
        return Seat == other.Seat
            && Agent == other.Agent
            && ConfigurationJson == other.ConfigurationJson
            && Wonder == other.Wonder
            && Side == other.Side
            && VictoryPoints == other.VictoryPoints
            && Coins == other.Coins
            && Rank == other.Rank
            && WinShare.Equals(other.WinShare)
            && AgentStatisticsJson == other.AgentStatisticsJson;
    }

    /// <inheritdoc/>
    public override bool Equals(object? otherObject)
    {
        return Equals(otherObject as SeatResult);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // HashCode.Combine mixes the fields into one int. Equal results must give equal hashes; it leaves out the two
        // JSON texts (Combine takes at most 8 values), which is allowed: unequal results may share a hash.
        return HashCode.Combine(Seat, Agent, Wonder, Side, VictoryPoints, Coins, Rank, WinShare);
    }

    /// <summary>For test failure messages: every value, e.g. <c>SeatResult { Seat = 0, ... }</c>.</summary>
    public override string ToString()
    {
        return $"SeatResult {{ Seat = {Seat}, Agent = {Agent}, ConfigurationJson = {ConfigurationJson}, " +
               $"Wonder = {Wonder}, " +
               $"Side = {Side}, VictoryPoints = {VictoryPoints}, Coins = {Coins}, Rank = {Rank}, " +
               $"WinShare = {WinShare}, AgentStatisticsJson = {AgentStatisticsJson} }}";
    }
}
