using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using _7WondersGame.src.models;

namespace _7WondersGameTests.PlayerTests
{
    /// <summary>The score after a card is built and the era ends (a battle; Olympia B copies a guild).</summary>
    [TestClass]
    public class ScoreAfterBuildingCardTests
    {
        private static int ScoreAfterBuilding(PlayerState player, int era, CardId id)
        {
            Card card = CardsDataset.GameCards[id];
            Assert.IsTrue(player.BuildStructure(card, new List<Card> { card }, true));
            player.Battle(era);
            player.CopyGuild();
            return player.CalculateScore();
        }

        private static List<PlayerState> ThreePlayersWithoutCoins()
        {
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<PlayerState> players = MockHelper.MockPlayers(allWonders.GetRange(0, 3));

            // set coins to 0 so they don't add to the score
            players[1].Resources[Resource.Coins] = 0;
            return players;
        }

        [TestMethod]
        public void CivilianCard_Era1()
        {
            List<PlayerState> players = ThreePlayersWithoutCoins();

            Assert.AreEqual(8, ScoreAfterBuilding(players[1], 1, CardId.Palace));
        }

        [TestMethod]
        public void StrategistsGuild_Era3()
        {
            List<PlayerState> players = ThreePlayersWithoutCoins();
            players[1].Resources[Resource.Shields] = 3;

            Assert.AreEqual(10, ScoreAfterBuilding(players[1], 3, CardId.Strategists));
        }

        [TestMethod]
        public void MilitaryCard_Era3()
        {
            List<PlayerState> players = ThreePlayersWithoutCoins();

            Assert.AreEqual(10, ScoreAfterBuilding(players[1], 3, CardId.Walls));
        }
    }
}
