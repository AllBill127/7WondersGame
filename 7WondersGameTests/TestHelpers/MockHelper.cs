using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGame.Engine.Wonders.Enums;

namespace _7WondersGameTests.TestHelpers;

/// <summary>Wonder boards and seated players for tests that set up a table by hand.</summary>
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
        List<PlayerState> players = new List<PlayerState>();
        for (int seat = 0; seat < wonders.Count; seat++)
        {
            players.Add(new PlayerState(seat) { Board = wonders[seat] });
        }

        foreach (PlayerState p in players)
        {
            p.PlayerWest = players[SeatToTheWest(p.Id, players.Count)];
            p.PlayerEast = players[SeatToTheEast(p.Id, players.Count)];
        }

        return players;
    }

    private static int SeatToTheWest(int seat, int playerCount)
    {
        if (seat == 0)
        {
            return playerCount - 1;
        }

        return seat - 1;
    }

    private static int SeatToTheEast(int seat, int playerCount)
    {
        int lastSeat = playerCount - 1;
        if (seat == lastSeat)
        {
            return 0;
        }

        return seat + 1;
    }
}
