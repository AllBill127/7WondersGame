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
    public static List<WonderBoardProgress> MockWonders()
    {
        List<WonderBoardProgress> wonders = new()
        {
            new WonderBoardProgress(WonderId.GizahA),
            new WonderBoardProgress(WonderId.BabylonA),
            new WonderBoardProgress(WonderId.OlympiaA),
            new WonderBoardProgress(WonderId.RhodosA),
            new WonderBoardProgress(WonderId.EphesosA),
            new WonderBoardProgress(WonderId.AlexandriaA),
            new WonderBoardProgress(WonderId.HalikarnassosA),
            new WonderBoardProgress(WonderId.GizahB),
            new WonderBoardProgress(WonderId.BabylonB),
            new WonderBoardProgress(WonderId.OlympiaB),
            new WonderBoardProgress(WonderId.RhodosB),
            new WonderBoardProgress(WonderId.EphesosB),
            new WonderBoardProgress(WonderId.AlexandriaB),
            new WonderBoardProgress(WonderId.HalikarnassosB)
        };

        return wonders;
    }

    /// <summary>One player per wonder, seated in that order, each with its west and east neighbour set.</summary>
    public static List<PlayerState> MockPlayers(List<WonderBoardProgress> wonders)
    {
        List<PlayerState> players = new();
        for (int seat = 0; seat < wonders.Count; seat++)
        {
            players.Add(new PlayerState(seat) { Wonder = wonders[seat] });
        }

        foreach (PlayerState player in players)
        {
            player.WestNeighbour = players[SeatToTheWest(player.Seat, players.Count)];
            player.EastNeighbour = players[SeatToTheEast(player.Seat, players.Count)];
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
