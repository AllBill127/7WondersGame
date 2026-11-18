using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Common.Settings;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Cli.Results;

/// <summary>
/// Final ranking used everywhere results are judged (the rulebook's tie-break):
/// most VP wins; on a VP tie, the player with more coins wins; players still tied share the place.
/// </summary>
/// <remarks>
/// MCTS ranks the players at the end of every rollout, so the ranking is written without LINQ or lambdas.
/// </remarks>
public static class GameResult
{
    /// <summary>
    /// Standings in the same order as the input.
    /// </summary>
    public static List<PlayerStanding> Rank(IReadOnlyList<(int Id, int VictoryPoints, int Coins)> players)
    {
        var standings = new List<PlayerStanding>(players.Count);
        for (int seat = 0; seat < players.Count; seat++)
        {
            var player = players[seat];
            int better = CountPlayersBeating(player, players);
            standings.Add(new PlayerStanding(player.Id, player.VictoryPoints, player.Coins, better + 1, 0));
        }

        // the players tied for first share the win
        int winners = CountWinners(standings);
        double winShare = 1.0 / winners;
        for (int standingIndex = 0; standingIndex < standings.Count; standingIndex++)
        {
            if (standings[standingIndex].Rank == 1)
            {
                standings[standingIndex] = standings[standingIndex].WithWinShare(winShare);
            }
        }
        return standings;
    }

    /// <summary>Standings of the players by their victory points and coins now, in the same order.</summary>
    public static List<PlayerStanding> Rank(IReadOnlyList<PlayerState> players)
    {
        var scores = new List<(int Id, int VictoryPoints, int Coins)>(players.Count);
        for (int seat = 0; seat < players.Count; seat++)
        {
            PlayerState player = players[seat];
            scores.Add((player.Seat, player.VictoryPoints, player.Resources[Resource.Coins]));
        }
        return Rank(scores);
    }

    /// <summary>
    /// Final results of a scored game, one per seat, in seat order, with the agent that played each seat.
    /// </summary>
    public static List<SeatResult> ForPlayers(IReadOnlyList<PlayerState> players, IReadOnlyList<IAgent> agents)
    {
        List<PlayerStanding> standings = Rank(players);
        var results = new List<SeatResult>(players.Count);
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

    /// <summary>How many of <paramref name="players"/> beat <paramref name="player"/>.</summary>
    private static int CountPlayersBeating((int Id, int VictoryPoints, int Coins) player,
                                           IReadOnlyList<(int Id, int VictoryPoints, int Coins)> players)
    {
        int count = 0;
        for (int otherIndex = 0; otherIndex < players.Count; otherIndex++)
        {
            if (Beats(players[otherIndex], player))
            {
                count++;
            }
        }
        return count;
    }

    private static int CountWinners(List<PlayerStanding> standings)
    {
        int winners = 0;
        foreach (PlayerStanding standing in standings)
        {
            if (standing.Rank == 1)
            {
                winners++;
            }
        }
        return winners;
    }

    private static bool Beats((int Id, int VictoryPoints, int Coins) player,
                              (int Id, int VictoryPoints, int Coins) other)
    {
        return player.VictoryPoints > other.VictoryPoints
            || (player.VictoryPoints == other.VictoryPoints && player.Coins > other.Coins);
    }
}
