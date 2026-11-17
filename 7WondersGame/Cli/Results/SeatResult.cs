using _7WondersGame.Agents.Common.Interfaces;

namespace _7WondersGame.Cli.Results;

/// <summary>
/// What one seat got in a finished game. One CSV row per seat (B2).
/// Wonder is the board name without the side (e.g. "Gizah"); Side is "A" or "B".
/// StatsJson is what the agent measured (<see cref="IAgent.StatsJson"/>).
/// </summary>
public record SeatResult(int Seat, string Agent, string ConfigJson, string Wonder, string Side,
                         int VictoryPoints, int Coins, int Rank, double WinShare, string StatsJson = "{}");
