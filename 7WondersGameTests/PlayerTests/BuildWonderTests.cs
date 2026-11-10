using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using _7WondersGame.src.models;

namespace _7WondersGameTests.PlayerTests
{
    [TestClass]
    public class BuildWonderTests
    {

        [TestMethod]
        public void BuildWonder_WhenAvailable_ReturnsTrue()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);

            // give resources for situation
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.ClayPool]);
            players[0].Resources[Resource.Clay] += 1;    // manually add clay from clayPool card (it is done in BuildStructure() which is not called here)
            players[1].Resources[Resource.Coins] = 3;
            players[1].PlayedCards.Add(CardsDataset.GameCards[CardId.ClayPit]);

            // set hand card for situation
            Card player1Card = CardsDataset.GameCards[CardId.Baths];
            players[1].HandCards.Add(player1Card);

            // Expected values
            int expThisPlayerCoins = 1;
            int expEastPlayerCoins = 5;
            int expWestPlayerCoins = 3;

            // Test
            players[1].CheckCanBuildWonder();
            bool canBuildWonder = players[1].BuildWonder(player1Card, players[1].HandCards);

            // Evaluate
            Assert.IsTrue(canBuildWonder);
            Assert.IsFalse(players[1].CanBuildWonder);
            // eval hand card change
            bool cardInHand = players[1].HandCards.Contains<Card>(player1Card);
            Assert.IsFalse(cardInHand);
            // eval resources change
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
        }
    }
}
