using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Games.Dtos;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Games.Services;

/// <summary>
/// The final ranking, used everywhere a game's outcome is judged (the rulebook's tie-break): most VP wins; on a VP
/// tie, the player with more coins wins; players still tied share the place.
/// </summary>
/// <remarks>
/// MCTS ranks the players at the end of every rollout, so the ranking is written without LINQ or lambdas.
/// </remarks>
public static class Standings
{
    /// <summary>
    /// Standings in the same order as the input.
    /// </summary>
    internal static List<PlayerStanding> Rank(IReadOnlyList<FinalScore> players)
    {
        List<PlayerStanding> standings = new(players.Count);
        for (int seat = 0; seat < players.Count; seat++)
        {
            FinalScore player = players[seat];
            int better = CountPlayersBeating(player, players);
            standings.Add(new PlayerStanding(player.Seat, player.VictoryPoints, player.Coins, better + 1, 0));
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
        List<FinalScore> scores = new(players.Count);
        for (int seat = 0; seat < players.Count; seat++)
        {
            PlayerState player = players[seat];
            scores.Add(new FinalScore(player.Seat, player.VictoryPoints, player.Resources[Resource.Coins]));
        }
        return Rank(scores);
    }

    /// <summary>How many of <paramref name="players"/> beat <paramref name="player"/>.</summary>
    private static int CountPlayersBeating(FinalScore player, IReadOnlyList<FinalScore> players)
    {
        int count = 0;
        for (int otherIndex = 0; otherIndex < players.Count; otherIndex++)
        {
            if (players[otherIndex].Beats(player))
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
}
