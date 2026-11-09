using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models
{
    /// <summary>
    /// One player's final standing.
    /// Rank 1 is the best; tied players share a rank.
    /// WinShare is 1/k for each of the k players tied for first place, otherwise 0.
    /// </summary>
    public record PlayerStanding(int Id, int VictoryPoints, int Coins, int Rank, double WinShare);

    /// <summary>
    /// Final ranking used everywhere results are judged (R9, rulebook tie-break):
    /// most VP wins; on a VP tie, the player with more coins wins; players still tied share the place.
    /// </summary>
    public static class GameResult
    {
        /// <summary>
        /// Standings in the same order as the input.
        /// </summary>
        public static List<PlayerStanding> Rank(IReadOnlyList<(int Id, int VictoryPoints, int Coins)> players)
        {
            var standings = new List<PlayerStanding>(players.Count);
            foreach (var p in players)
            {
                int better = players.Count(o => Beats(o, p));
                standings.Add(new PlayerStanding(p.Id, p.VictoryPoints, p.Coins, better + 1, 0));
            }

            int winners = standings.Count(s => s.Rank == 1);
            return standings
                .Select(s => s.Rank == 1 ? s with { WinShare = 1.0 / winners } : s)
                .ToList();
        }

        public static List<PlayerStanding> Rank(IEnumerable<Player> players) =>
            Rank(players.Select(p => (p.Id, p.VictoryPoints, p.Resources[Resource.Coins])).ToList());

        internal static List<PlayerStanding> Rank(IEnumerable<PlayerResults> players) =>
            Rank(players.Select(p => (p.Id, p.VictoryPoints, p.Resources[Resource.Coins])).ToList());

        private static bool Beats((int Id, int VictoryPoints, int Coins) a, (int Id, int VictoryPoints, int Coins) b) =>
            a.VictoryPoints > b.VictoryPoints ||
            a.VictoryPoints == b.VictoryPoints && a.Coins > b.Coins;
    }
}
