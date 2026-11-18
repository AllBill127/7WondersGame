using _7WondersGame.Agents.Common.Interfaces;

namespace _7WondersGame.Cli.Results;

/// <summary>
/// What one seat got in a finished game. One CSV row per seat.
/// Wonder is the board name without the side (e.g. "Gizah"); Side is "A" or "B".
/// StatsJson is what the agent measured (<see cref="IAgent.StatsJson"/>).
/// Two results are equal when every value is equal (a traced game must end the same as a silent one).
/// </summary>
public sealed class SeatResult : IEquatable<SeatResult>
{
    /// <summary>What seat <paramref name="seat"/> got.</summary>
    public SeatResult(int seat, string agent, string configJson, string wonder, string side,
                      int victoryPoints, int coins, int rank, double winShare, string statsJson = "{}")
    {
        Seat = seat;
        Agent = agent;
        ConfigJson = configJson;
        Wonder = wonder;
        Side = side;
        VictoryPoints = victoryPoints;
        Coins = coins;
        Rank = rank;
        WinShare = winShare;
        StatsJson = statsJson;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The agent's name.</summary>
    public string Agent { get; }

    /// <summary>The agent's settings as JSON.</summary>
    public string ConfigJson { get; }

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

    /// <summary>What the agent measured (see <see cref="IAgent.StatsJson"/>).</summary>
    public string StatsJson { get; }

    /// <inheritdoc/>
    public bool Equals(SeatResult? other)
    {
        if (other is null)
        {
            return false;
        }
        return Seat == other.Seat
            && Agent == other.Agent
            && ConfigJson == other.ConfigJson
            && Wonder == other.Wonder
            && Side == other.Side
            && VictoryPoints == other.VictoryPoints
            && Coins == other.Coins
            && Rank == other.Rank
            && WinShare.Equals(other.WinShare)
            && StatsJson == other.StatsJson;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as SeatResult);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Seat, Agent, Wonder, Side, VictoryPoints, Coins, Rank, WinShare);
    }

    /// <summary>For test failure messages: every value, e.g. <c>SeatResult { Seat = 0, ... }</c>.</summary>
    public override string ToString()
    {
        return $"SeatResult {{ Seat = {Seat}, Agent = {Agent}, ConfigJson = {ConfigJson}, Wonder = {Wonder}, " +
               $"Side = {Side}, VictoryPoints = {VictoryPoints}, Coins = {Coins}, Rank = {Rank}, " +
               $"WinShare = {WinShare}, StatsJson = {StatsJson} }}";
    }
}
