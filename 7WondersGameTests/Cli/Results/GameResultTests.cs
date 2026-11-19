using _7WondersGame.Cli.Results;

namespace _7WondersGameTests.Cli.Results;

/// <summary>Ranking the players at the end of a game: victory points, then coins, shared wins.</summary>
[TestClass]
public class GameResultTests
{
    private static List<PlayerStanding> Rank(params (int Id, int VictoryPoints, int Coins)[] players)
    {
        return GameResult.Rank(players.ToList());
    }

    [TestMethod]
    public void Rank_HighestVPWins()
    {
        List<PlayerStanding> standings = Rank((0, 40, 0), (1, 50, 0), (2, 45, 0));

        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, standings.Select(standing => standing.Rank).ToArray());
        CollectionAssert.AreEqual(new[] { 0.0, 1.0, 0.0 }, standings.Select(standing => standing.WinShare).ToArray());
    }

    [TestMethod]
    public void Rank_VPTie_MoreCoinsWins()
    {
        List<PlayerStanding> standings = Rank((0, 50, 3), (1, 50, 7), (2, 45, 20));

        CollectionAssert.AreEqual(new[] { 2, 1, 3 }, standings.Select(standing => standing.Rank).ToArray());
        CollectionAssert.AreEqual(new[] { 0.0, 1.0, 0.0 }, standings.Select(standing => standing.WinShare).ToArray());
    }

    [TestMethod]
    public void Rank_FullTieForFirst_SharesWin()
    {
        List<PlayerStanding> standings = Rank((0, 50, 4), (1, 30, 0), (2, 50, 4));

        CollectionAssert.AreEqual(new[] { 1, 3, 1 }, standings.Select(standing => standing.Rank).ToArray());
        CollectionAssert.AreEqual(new[] { 0.5, 0.0, 0.5 }, standings.Select(standing => standing.WinShare).ToArray());
    }

    [TestMethod]
    public void Rank_ThreeWayTie_EachGetsAThird()
    {
        List<PlayerStanding> standings = Rank((0, 40, 2), (1, 40, 2), (2, 40, 2));

        Assert.IsTrue(standings.All(standing => standing.Rank == 1));
        foreach (PlayerStanding standing in standings)
        {
            Assert.AreEqual(1.0 / 3, standing.WinShare, 1e-12);
        }
    }

    [TestMethod]
    public void Rank_TieBelowFirst_SharesRank_NoWinShare()
    {
        List<PlayerStanding> standings = Rank((0, 60, 0), (1, 40, 1), (2, 40, 1), (3, 10, 0));

        CollectionAssert.AreEqual(new[] { 1, 2, 2, 4 }, standings.Select(standing => standing.Rank).ToArray());
        Assert.AreEqual(1.0, standings.Sum(standing => standing.WinShare), 1e-12);
    }
}
