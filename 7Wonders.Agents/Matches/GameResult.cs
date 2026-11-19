using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Agents.Common.Settings;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Games.Dtos;
using _7Wonders.Engine.Games.Services;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Agents.Matches;

/// <summary>
/// The results of a finished game as the results file records them: each seat's <see cref="Standings"/> with the
/// agent that played it.
/// </summary>
public static class GameResult
{
    /// <summary>
    /// Final results of a scored game, one per seat, in seat order, with the agent that played each seat.
    /// </summary>
    public static List<SeatResult> ForPlayers(IReadOnlyList<PlayerState> players, IReadOnlyList<IAgent> agents)
    {
        List<PlayerStanding> standings = Standings.Rank(players);
        List<SeatResult> results = new(players.Count);
        for (int seat = 0; seat < players.Count; seat++)
        {
            results.Add(ResultOf(players[seat], agents[seat], standings[seat]));
        }
        return results;
    }

    /// <summary>What one seat got: the board name and side apart (e.g. "Gizah" and "A"), and the agent.</summary>
    private static SeatResult ResultOf(PlayerState player, IAgent agent, PlayerStanding standing)
    {
        string board = player.Wonder.Id.ToString();
        int sideStart = board.Length - 1;
        string wonder = board.Substring(0, sideStart);
        string side = board.Substring(sideStart);
        return new SeatResult(
            player.Seat,
            agent.Name,
            agent.ConfigurationJson(),
            wonder,
            side,
            player.VictoryPoints,
            player.Resources[Resource.Coins],
            standing.Rank,
            standing.WinShare,
            agent.StatisticsJson());
    }
}
