using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests.PlayerTests
{
    [TestClass]
    public class CalculateScientificScoreTests
    {
        /// <summary>
        /// Wildcards come from the Scientists Guild (ExtraScience) and Babylon A stage 2.
        /// Each one must go on whichever symbol gives the most VP (R6).
        /// </summary>
        [DataTestMethod]
        // k = 0
        [DataRow(0, 0, 0, 0, 0)]
        [DataRow(2, 1, 1, 0, 13)]
        [DataRow(3, 3, 3, 0, 48)]
        // k = 1
        [DataRow(0, 0, 0, 1, 1)]
        [DataRow(2, 0, 0, 1, 9)]
        [DataRow(1, 1, 0, 1, 10)]
        [DataRow(3, 2, 1, 1, 31)]   // code-comment case: the wildcard completes a second set (old code gave 28)
        // k = 2
        [DataRow(0, 0, 0, 2, 4)]
        [DataRow(2, 2, 0, 2, 26)]
        [DataRow(3, 1, 0, 2, 26)]   // both on gear
        [DataRow(2, 1, 0, 2, 18)]   // split: one gear, one tablet (old code gave 17)
        public void CalculateScientificScore_PlacesWildcardsBest(int gear, int compass, int tablet, int wildcards, int expected)
        {
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(
                new List<Wonder> { new BabylonA(), new GizahA(), new GizahA() });
            Player player = players[0];
            player.Resources[Resource.Gear] = gear;
            player.Resources[Resource.Compass] = compass;
            player.Resources[Resource.Tablet] = tablet;
            if (wildcards >= 1) player.ExtraScience = true;
            if (wildcards >= 2) player.Board.Stage = 2;

            Assert.AreEqual(expected, player.CalculateScientificScore());
        }

        [TestMethod]
        public void BestScienceScore_MatchesEveryPlacementByHand()
        {
            for (int g = 0; g <= 4; g++)
                for (int c = 0; c <= 4; c++)
                    for (int t = 0; t <= 4; t++)
                    {
                        int best1 = new[] { Player.ScienceScore(g + 1, c, t), Player.ScienceScore(g, c + 1, t), Player.ScienceScore(g, c, t + 1) }.Max();
                        Assert.AreEqual(best1, Player.BestScienceScore(g, c, t, 1), $"{g},{c},{t} k=1");
                        Assert.IsTrue(Player.BestScienceScore(g, c, t, 2) >= best1, $"{g},{c},{t} k=2 never worse than k=1");
                    }
        }
    }
}
