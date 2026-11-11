using _7WondersGame.src.models.Wonders;
using _7WondersGame.src.models.Players;

namespace _7WondersGameTests
{
    internal class MockHelper
    {
        /// <summary>
        /// List of Gizah, Babylon, Olympia, Rhodos, Ephesos, Alexandria, Halikarnassos
        /// </summary>
        /// <returns>List of A and B Wonders</returns>
        public static List<Wonder> MockWonders()
        {
            List<Wonder> wonders = new List<Wonder>
            {
                new Wonder(WonderId.GizahA),
                new Wonder(WonderId.BabylonA),
                new Wonder(WonderId.OlympiaA),
                new Wonder(WonderId.RhodosA),
                new Wonder(WonderId.EphesosA),
                new Wonder(WonderId.AlexandriaA),
                new Wonder(WonderId.HalikarnassosA),
                new Wonder(WonderId.GizahB),
                new Wonder(WonderId.BabylonB),
                new Wonder(WonderId.OlympiaB),
                new Wonder(WonderId.RhodosB),
                new Wonder(WonderId.EphesosB),
                new Wonder(WonderId.AlexandriaB),
                new Wonder(WonderId.HalikarnassosB)
            };

            return wonders;
        }

        /// <summary>One player per wonder, seated in that order, each with its west and east neighbour set.</summary>
        public static List<PlayerState> MockPlayers(List<Wonder> wonders)
        {
            List<PlayerState> players = wonders.Select((wonder, seat) => new PlayerState(seat) { Board = wonder }).ToList();
            int n = players.Count;
            foreach (PlayerState p in players)
            {
                p.PlayerWest = players[(p.Id + n - 1) % n];
                p.PlayerEast = players[(p.Id + 1) % n];
            }
            return players;
        }
    }
}
