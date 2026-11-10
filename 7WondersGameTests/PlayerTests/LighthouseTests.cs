using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests.PlayerTests
{
    /// <summary>
    /// The Lighthouse counts itself: it gives 1 coin (when built) and 1 VP (at the end)
    /// per yellow card in the city, including the Lighthouse (R9).
    /// </summary>
    [TestClass]
    public class LighthouseTests
    {
        private static PlayerState PlayerWithTwoYellowCards()
        {
            List<PlayerState> players = MockHelper.MockPlayers(
                new List<Wonder> { new GizahA(), new BabylonA(), new OlympiaA() });
            PlayerState player = players[0];
            player.PlayedCards.Add(CardsDataset.GameCards[CardId.Marketplace]);
            player.PlayedCards.Add(CardsDataset.GameCards[CardId.Tavern]);
            return player;
        }

        [TestMethod]
        public void BuildStructure_Lighthouse_CountsItselfForCoins()
        {
            var player = PlayerWithTwoYellowCards();
            Card lighthouse = CardsDataset.GameCards[CardId.Lighthouse];

            Assert.IsTrue(player.BuildStructure(lighthouse, new List<Card> { lighthouse }, true));

            // 3 starting coins + 3 yellow cards (Marketplace, Tavern, Lighthouse)
            Assert.AreEqual(6, player.Resources[Resource.Coins]);
            Assert.AreEqual(3, player.CalculateCommercialScore());
        }

        [TestMethod]
        public void ScoreAfterPlayingCard_Lighthouse_CountsItselfForCoins()
        {
            var player = PlayerWithTwoYellowCards();

            int score = player.ScoreAfterPlayingCard(3, CardsDataset.GameCards[CardId.Lighthouse]);

            // 3 VP from the Lighthouse + 6 coins / 3 = 2 VP (no shields anywhere, so no military points)
            Assert.AreEqual(5, score);
            Assert.AreEqual(3, player.Resources[Resource.Coins], "state restored");
        }
    }
}
