using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using _7WondersGame.src.models;

namespace _7WondersGameTests.PlayerTests
{
    [TestClass]
    public class CheckCanBuilDWonderTests
    {

        [TestMethod]
        public void CheckCanBuildWonder_WhenAvailable_ReturnsTrue()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

            // give resources for situation
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.ClayPool]);
            players[0].Resources[Resource.Clay] += 1;    // manually add clay from clayPool card (it is done in BuildStructure() which is not called here)
            players[1].Resources[Resource.Coins] = 3;
            players[1].PlayedCards.Add(CardsDataset.GameCards[CardId.ClayPit]);

            // Expected values
            int expThisPlayerCoins = 3;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            bool canBuildWonder = players[1].CanBuildWonder();

            // Evaluate
            Assert.IsTrue(canBuildWonder);
            // eval resources unchanged
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
        }
    }
}
