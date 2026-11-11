using _7WondersGame.src.models;

namespace _7WondersGameTests.GameTests
{
    [TestClass]
    public class GameResultTests
    {
        private static List<PlayerStanding> Rank(params (int Id, int VictoryPoints, int Coins)[] players) =>
            GameResult.Rank(players.ToList());

        [TestMethod]
        public void Rank_HighestVPWins()
        {
            var s = Rank((0, 40, 0), (1, 50, 0), (2, 45, 0));

            CollectionAssert.AreEqual(new[] { 3, 1, 2 }, s.Select(x => x.Rank).ToArray());
            CollectionAssert.AreEqual(new[] { 0.0, 1.0, 0.0 }, s.Select(x => x.WinShare).ToArray());
        }

        [TestMethod]
        public void Rank_VPTie_MoreCoinsWins()
        {
            var s = Rank((0, 50, 3), (1, 50, 7), (2, 45, 20));

            CollectionAssert.AreEqual(new[] { 2, 1, 3 }, s.Select(x => x.Rank).ToArray());
            CollectionAssert.AreEqual(new[] { 0.0, 1.0, 0.0 }, s.Select(x => x.WinShare).ToArray());
        }

        [TestMethod]
        public void Rank_FullTieForFirst_SharesWin()
        {
            var s = Rank((0, 50, 4), (1, 30, 0), (2, 50, 4));

            CollectionAssert.AreEqual(new[] { 1, 3, 1 }, s.Select(x => x.Rank).ToArray());
            CollectionAssert.AreEqual(new[] { 0.5, 0.0, 0.5 }, s.Select(x => x.WinShare).ToArray());
        }

        [TestMethod]
        public void Rank_ThreeWayTie_EachGetsAThird()
        {
            var s = Rank((0, 40, 2), (1, 40, 2), (2, 40, 2));

            Assert.IsTrue(s.All(x => x.Rank == 1));
            foreach (var x in s)
                Assert.AreEqual(1.0 / 3, x.WinShare, 1e-12);
        }

        [TestMethod]
        public void Rank_TieBelowFirst_SharesRank_NoWinShare()
        {
            var s = Rank((0, 60, 0), (1, 40, 1), (2, 40, 1), (3, 10, 0));

            CollectionAssert.AreEqual(new[] { 1, 2, 2, 4 }, s.Select(x => x.Rank).ToArray());
            Assert.AreEqual(1.0, s.Sum(x => x.WinShare), 1e-12);
        }
    }
}
