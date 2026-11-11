using _7WondersGame.src.models.Agents;
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
    /// What one seat got in a finished game. One CSV row per seat (B2).
    /// Wonder is the board name without the side (e.g. "Gizah"); Side is "A" or "B".
    /// </summary>
    public record SeatResult(int Seat, string Agent, string ConfigJson, string Wonder, string Side,
                             int VictoryPoints, int Coins, int Rank, double WinShare);

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

        public static List<PlayerStanding> Rank(IEnumerable<PlayerState> players) =>
            Rank(players.Select(p => (p.Id, p.VictoryPoints, p.Resources[Resource.Coins])).ToList());

        /// <summary>
        /// Final results of a scored game, one per seat, in seat order, with the agent that played each seat.
        /// </summary>
        public static List<SeatResult> ForPlayers(IReadOnlyList<PlayerState> players, IReadOnlyList<IAgent> agents)
        {
            List<PlayerStanding> standings = Rank(players);
            return players.Select((p, i) =>
            {
                string board = p.Board.Id.ToString();
                return new SeatResult(p.Id, agents[i].Name, agents[i].ConfigJson(),
                    board[..^1], board[^1..],
                    p.VictoryPoints, p.Resources[Resource.Coins], standings[i].Rank, standings[i].WinShare);
            }).ToList();
        }

        private static bool Beats((int Id, int VictoryPoints, int Coins) a, (int Id, int VictoryPoints, int Coins) b) =>
            a.VictoryPoints > b.VictoryPoints ||
            a.VictoryPoints == b.VictoryPoints && a.Coins > b.Coins;
    }
}
