using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests.GameTests
{
    [TestClass]
    public class RotateHandCardsTests
    {
        [TestMethod]
        public void RotateHandCards_CorrectlyRotatesCardListsClockwise()
        {
            // Arrange
            Game testGame = new Game();
            testGame.Era = 1;

            List<Wonder> wonders = MockHelper.MockWonders().GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(wonders);
            testGame.Players = players;

            // add some cards
            testGame.Players[0].HandCards.Add(CardsDataset.GameCards[CardId.ClayPool]);
            testGame.Players[0].HandCards.Add(CardsDataset.GameCards[CardId.Pantheon]);
            testGame.Players[0].HandCards.Add(CardsDataset.GameCards[CardId.Dispensary]);

            testGame.Players[1].HandCards.Add(CardsDataset.GameCards[CardId.OreVein]);
            testGame.Players[1].HandCards.Add(CardsDataset.GameCards[CardId.Baths]);
            testGame.Players[1].HandCards.Add(CardsDataset.GameCards[CardId.Lodge]);

            testGame.Players[2].HandCards.Add(CardsDataset.GameCards[CardId.Quarry]);
            testGame.Players[2].HandCards.Add(CardsDataset.GameCards[CardId.Statue]);
            testGame.Players[2].HandCards.Add(CardsDataset.GameCards[CardId.School]);

            // Expected results
            List<Card> expP0cards = testGame.Players[1].HandCards.Select(e => e).ToList();
            List<Card> expP1cards = testGame.Players[2].HandCards.Select(e => e).ToList();
            List<Card> expP2cards = testGame.Players[0].HandCards.Select(e => e).ToList();

            // Test
            testGame.RotateHandCards();

            // Evaluate
            bool isMatch = players[0].HandCards.SequenceEqual(expP0cards);
            Assert.IsTrue(isMatch);
            isMatch = players[1].HandCards.SequenceEqual(expP1cards);
            Assert.IsTrue(isMatch);
            isMatch = players[2].HandCards.SequenceEqual(expP2cards);
            Assert.IsTrue(isMatch);
        }
    }
}
