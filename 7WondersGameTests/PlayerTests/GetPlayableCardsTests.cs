using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using _7WondersGame.src.models;

namespace _7WondersGameTests.PlayerTests
{
    [TestClass]
    public class GetPlayableCardsTests
    {

        [TestMethod]
        public void GetPlayableCards_Default_WhenSingleCardIsPlayable_ReturnsListWithCard()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

            // give resources for situation
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.TreeFarm]);
            players[1].Resources[Resource.Coins] = 4;
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.TimberYard]);

            // set hand card list
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Caravansery]);

            // Expected values
            int expThisPlayerCoins = 4;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            List<Card> playableCards = players[1].GetPlayableCards(players[1].HandCards);
            List<Card> playable1 = playableCards;

            bool canPlayCard = playable1.Contains<Card>(CardsDataset.GameCards[CardId.Caravansery]);

            // Evaluate
            Assert.IsTrue(canPlayCard);
            // eval resources unchanged
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
        }

        [TestMethod]
        public void GetPlayableCards_Default_WhenSingleCardIsNotPlayable_ReturnsListWithoutCard()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

            // give resources for situation
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.TreeFarm]);
            players[1].Resources[Resource.Coins] = 4;

            // set hand card list
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Caravansery]);

            // Expected values
            int expThisPlayerCoins = 4;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            List<Card> playableCards = players[1].GetPlayableCards(players[1].HandCards);
            List<Card> playable1 = playableCards;

            bool canPlayCard = playable1.Contains<Card>(CardsDataset.GameCards[CardId.Caravansery]);

            // Evaluate
            Assert.IsFalse(canPlayCard);
            // eval resources unchanged
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
        }

        [TestMethod]
        public void GetPlayableCards_Default_WhenSingleCardIsNotPlayable_CantSellComercialResource_ReturnsListWithoutCard()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

            // give resources for situation
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.TreeFarm]);
            players[1].Resources[Resource.Coins] = 6;
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.ClayPit]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.Forum]); // has untradeable glass

            // set hand card list
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Temple]);

            // Expected values
            int expThisPlayerCoins = 6;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            List<Card> playableCards = players[1].GetPlayableCards(players[1].HandCards);
            List<Card> playable1 = playableCards;

            bool canPlayCard = playable1.Contains<Card>(CardsDataset.GameCards[CardId.Caravansery]);

            // Evaluate
            Assert.IsFalse(canPlayCard);
            // eval resources unchanged
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
        }

        [TestMethod]
        public void GetPlayableCards_Default_WhenMultipleCardsArePlayable_ReturnsListWithCards()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

            // give resources for situation
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.TreeFarm]);
            players[1].Resources[Resource.Coins] = 4;
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.TimberYard]);

            // set hand card list
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Caravansery]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Baths]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Sawmill]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Press]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.School]);  // not playable

            // Expected values
            int expThisPlayerCoins = 4;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            List<Card> playable1 = players[1].GetPlayableCards(players[1].HandCards);

            // Evaluate
            bool canPlayCard;
            for (int i = 0; i < players[1].HandCards.Count - 1; i++)
            {
                canPlayCard = playable1.Contains<Card>(players[1].HandCards[i]);
                Assert.IsTrue(canPlayCard);
            }
            // eval School card is unplayable
            canPlayCard = playable1.Contains<Card>(CardsDataset.GameCards[CardId.School]);
            Assert.IsFalse(canPlayCard);
            // eval resources unchanged
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
        }

        [TestMethod]
        public void GetPlayableCards_AllCardsFree_WhenNoDuplicateRuleViolation_ReturnsMatchingList()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

            // set hand card list
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Caravansery]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Baths]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Sawmill]); // could build with coins
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Press]);   // could build - no price
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.School]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Palace]);

            // Expected values
            List<Card> expP1PlayableCards = players[1].HandCards;

            // Test
            List<Card> playable1 = players[1].GetPlayableCards(players[1].HandCards, true);

            // Evaluate
            bool isMatch = playable1.SequenceEqual(expP1PlayableCards);
            Assert.IsTrue(isMatch);
        }

        [TestMethod]
        public void GetPlayableCards_AllCardsFree_WhenDuplicateRuleViolation_ReturnsCardsListWithoutViolation()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

            // set played card list
            players[1].PlayedCards.Add(CardsDataset.GameCards[CardId.Caravansery]);
            players[1].PlayedCards.Add(CardsDataset.GameCards[CardId.Press2]);

            // set hand card list
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Caravansery]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Baths]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Sawmill]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Press]);   // should be violation with Press2
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.School]);
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Palace]);

            // Expected values
            List<Card> expP1PlayableCards = new()
            {
                CardsDataset.GameCards[CardId.Baths],
                CardsDataset.GameCards[CardId.Sawmill],
                CardsDataset.GameCards[CardId.School],
                CardsDataset.GameCards[CardId.Palace],
            };

            // Test
            List<Card> playable1 = players[1].GetPlayableCards(players[1].HandCards, true);

            // Evaluate
            bool isMatch = playable1.SequenceEqual(expP1PlayableCards);
            Assert.IsTrue(isMatch);
        }
    }
}
