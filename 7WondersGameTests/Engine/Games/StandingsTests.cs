using _7WondersGame.Engine.Games.Dtos;
using _7WondersGame.Engine.Games.Services;

namespace _7WondersGameTests.Engine.Games;

/// <summary>Ranking the players at the end of a game: victory points, then coins, shared wins.</summary>
[TestClass]
public class StandingsTests
{
    private static List<PlayerStanding> Rank(params FinalScore[] players)
    {
        return Standings.Rank(players.ToList());
    }

    [TestMethod]
    public void Rank_HighestVPWins()
    {
        List<PlayerStanding> standings = Rank(
            new FinalScore(0, 40, 0),
            new FinalScore(1, 50, 0),
            new FinalScore(2, 45, 0));

        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, standings.Select(standing => standing.Rank).ToArray());
        CollectionAssert.AreEqual(new[] { 0.0, 1.0, 0.0 }, standings.Select(standing => standing.WinShare).ToArray());
    }

    [TestMethod]
    public void Rank_VPTie_MoreCoinsWins()
    {
        List<PlayerStanding> standings = Rank(
            new FinalScore(0, 50, 3),
            new FinalScore(1, 50, 7),
            new FinalScore(2, 45, 20));

        CollectionAssert.AreEqual(new[] { 2, 1, 3 }, standings.Select(standing => standing.Rank).ToArray());
        CollectionAssert.AreEqual(new[] { 0.0, 1.0, 0.0 }, standings.Select(standing => standing.WinShare).ToArray());
    }

    [TestMethod]
    public void Rank_FullTieForFirst_SharesWin()
    {
        List<PlayerStanding> standings = Rank(
            new FinalScore(0, 50, 4),
            new FinalScore(1, 30, 0),
            new FinalScore(2, 50, 4));

        CollectionAssert.AreEqual(new[] { 1, 3, 1 }, standings.Select(standing => standing.Rank).ToArray());
        CollectionAssert.AreEqual(new[] { 0.5, 0.0, 0.5 }, standings.Select(standing => standing.WinShare).ToArray());
    }

    [TestMethod]
    public void Rank_ThreeWayTie_EachGetsAThird()
    {
        List<PlayerStanding> standings = Rank(
            new FinalScore(0, 40, 2),
            new FinalScore(1, 40, 2),
            new FinalScore(2, 40, 2));

        Assert.IsTrue(standings.All(standing => standing.Rank == 1));
        foreach (PlayerStanding standing in standings)
        {
            Assert.AreEqual(1.0 / 3, standing.WinShare, 1e-12);
        }
    }

    [TestMethod]
    public void Rank_TieBelowFirst_SharesRank_NoWinShare()
    {
        List<PlayerStanding> standings = Rank(
            new FinalScore(0, 60, 0),
            new FinalScore(1, 40, 1),
            new FinalScore(2, 40, 1),
            new FinalScore(3, 10, 0));

        CollectionAssert.AreEqual(new[] { 1, 2, 2, 4 }, standings.Select(standing => standing.Rank).ToArray());
        Assert.AreEqual(1.0, standings.Sum(standing => standing.WinShare), 1e-12);
    }
}
