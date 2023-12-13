using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using _7WondersGame.src.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // give resources for situation
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.TreeFarm]);
            players[1].Resources[Resource.Coins] = 4;
            players[2].PlayedCards.Add(cardDS.GameCards[CardId.TimberYard]);

            // set hand card list
            players[1].HandCards.Add(cardDS.GameCards[CardId.Caravansery]);

            // Expected values
            int expThisPlayerCoins = 4;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            List<Card> playableCards = players[1].GetPlayableCards(players[1].HandCards);
            players[1].PlayableCards = playableCards;

            bool canPlayCard = players[1].PlayableCards.Contains<Card>(cardDS.GameCards[CardId.Caravansery]);

            // Evaluate
            Assert.IsTrue(canPlayCard);
            // eval resources unchanged
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
            // eval usedOnDemandResource reset success
            Dictionary<string, int> expUsedOnDemandResources = new Dictionary<string, int>()
                {
                    { "UsedTreeFarm", -1},
                    { "UsedForestCave", -1},
                    { "UsedTimberYard", -1},
                    { "UsedExcavation", -1},
                    { "UsedMine", -1 },
                    { "UsedClayPit", -1},
                    { "UsedForum", -1},
                    { "UsedCaravansery", -1},
                    { "UsedRawExtra", -1},
                    { "UsedManufExtra", -1},
            };
            for (int i = 0; i < players.Count; ++i)
            {
                bool isMatch = players[0].UsedOnDemandResource.OrderBy(kvp => kvp.Key)
                    .SequenceEqual(expUsedOnDemandResources.OrderBy(kvp => kvp.Key));
                Assert.IsTrue(isMatch);
            }
        }

        [TestMethod]
        public void GetPlayableCards_Default_WhenSingleCardIsNotPlayable_ReturnsListWithoutCard()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // give resources for situation
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.TreeFarm]);
            players[1].Resources[Resource.Coins] = 4;

            // set hand card list
            players[1].HandCards.Add(cardDS.GameCards[CardId.Caravansery]);

            // Expected values
            int expThisPlayerCoins = 4;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            List<Card> playableCards = players[1].GetPlayableCards(players[1].HandCards);
            players[1].PlayableCards = playableCards;

            bool canPlayCard = players[1].PlayableCards.Contains<Card>(cardDS.GameCards[CardId.Caravansery]);

            // Evaluate
            Assert.IsFalse(canPlayCard);
            // eval resources unchanged
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
            // eval usedOnDemandResource reset success
            Dictionary<string, int> expUsedOnDemandResources = new Dictionary<string, int>()
                {
                    { "UsedTreeFarm", -1},
                    { "UsedForestCave", -1},
                    { "UsedTimberYard", -1},
                    { "UsedExcavation", -1},
                    { "UsedMine", -1 },
                    { "UsedClayPit", -1},
                    { "UsedForum", -1},
                    { "UsedCaravansery", -1},
                    { "UsedRawExtra", -1},
                    { "UsedManufExtra", -1},
            };
            for (int i = 0; i < players.Count; ++i)
            {
                bool isMatch = players[0].UsedOnDemandResource.OrderBy(kvp => kvp.Key)
                    .SequenceEqual(expUsedOnDemandResources.OrderBy(kvp => kvp.Key));
                Assert.IsTrue(isMatch);
            }
        }

        [TestMethod]
        public void GetPlayableCards_Default_WhenSingleCardIsNotPlayable_CantSellComercialResource_ReturnsListWithoutCard()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // give resources for situation
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.TreeFarm]);
            players[1].Resources[Resource.Coins] = 6;
            players[2].PlayedCards.Add(cardDS.GameCards[CardId.ClayPit]);
            players[2].PlayedCards.Add(cardDS.GameCards[CardId.Forum]); // has untradeable glass

            // set hand card list
            players[1].HandCards.Add(cardDS.GameCards[CardId.Temple]);

            // Expected values
            int expThisPlayerCoins = 6;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            List<Card> playableCards = players[1].GetPlayableCards(players[1].HandCards);
            players[1].PlayableCards = playableCards;

            bool canPlayCard = players[1].PlayableCards.Contains<Card>(cardDS.GameCards[CardId.Caravansery]);

            // Evaluate
            Assert.IsFalse(canPlayCard);
            // eval resources unchanged
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
            // eval usedOnDemandResource reset success
            Dictionary<string, int> expUsedOnDemandResources = new Dictionary<string, int>()
                {
                    { "UsedTreeFarm", -1},
                    { "UsedForestCave", -1},
                    { "UsedTimberYard", -1},
                    { "UsedExcavation", -1},
                    { "UsedMine", -1 },
                    { "UsedClayPit", -1},
                    { "UsedForum", -1},
                    { "UsedCaravansery", -1},
                    { "UsedRawExtra", -1},
                    { "UsedManufExtra", -1},
            };
            for (int i = 0; i < players.Count; ++i)
            {
                bool isMatch = players[0].UsedOnDemandResource.OrderBy(kvp => kvp.Key)
                    .SequenceEqual(expUsedOnDemandResources.OrderBy(kvp => kvp.Key));
                Assert.IsTrue(isMatch);
            }
        }

        [TestMethod]
        public void GetPlayableCards_Default_WhenMultipleCardsArePlayable_ReturnsListWithCards()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // give resources for situation
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.TreeFarm]);
            players[1].Resources[Resource.Coins] = 4;
            players[2].PlayedCards.Add(cardDS.GameCards[CardId.TimberYard]);

            // set hand card list
            players[1].HandCards.Add(cardDS.GameCards[CardId.Caravansery]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.Baths]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.Sawmill]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.Press]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.School]);  // not playable

            // Expected values
            int expThisPlayerCoins = 4;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            players[1].PlayableCards = players[1].GetPlayableCards(players[1].HandCards);

            // Evaluate
            bool canPlayCard;
            for (int i = 0; i < players[1].HandCards.Count - 1; i++)
            {
                canPlayCard = players[1].PlayableCards.Contains<Card>(players[1].HandCards[i]);
                Assert.IsTrue(canPlayCard);
            }
            // eval School card is unplayable
            canPlayCard = players[1].PlayableCards.Contains<Card>(cardDS.GameCards[CardId.School]);
            Assert.IsFalse(canPlayCard);
            // eval resources unchanged
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
            // eval usedOnDemandResource reset success
            Dictionary<string, int> expUsedOnDemandResources = new Dictionary<string, int>()
                {
                    { "UsedTreeFarm", -1},
                    { "UsedForestCave", -1},
                    { "UsedTimberYard", -1},
                    { "UsedExcavation", -1},
                    { "UsedMine", -1 },
                    { "UsedClayPit", -1},
                    { "UsedForum", -1},
                    { "UsedCaravansery", -1},
                    { "UsedRawExtra", -1},
                    { "UsedManufExtra", -1},
            };
            for (int i = 0; i < players.Count; ++i)
            {
                bool isMatch = players[0].UsedOnDemandResource.OrderBy(kvp => kvp.Key)
                    .SequenceEqual(expUsedOnDemandResources.OrderBy(kvp => kvp.Key));
                Assert.IsTrue(isMatch);
            }
        }

        [TestMethod]
        public void GetPlayableCards_AllCardsFree_WhenNoDuplicateRuleViolation_ReturnsMatchingList()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set hand card list
            players[1].HandCards.Add(cardDS.GameCards[CardId.Caravansery]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.Baths]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.Sawmill]); // could build with coins
            players[1].HandCards.Add(cardDS.GameCards[CardId.Press]);   // could build - no price
            players[1].HandCards.Add(cardDS.GameCards[CardId.School]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.Palace]);

            // Expected values
            List<Card> expP1PlayableCards = players[1].HandCards;

            // Test
            players[1].PlayableCards = players[1].GetPlayableCards(players[1].HandCards, true);

            // Evaluate
            bool isMatch = players[1].PlayableCards.SequenceEqual(expP1PlayableCards);
            Assert.IsTrue(isMatch);
        }

        [TestMethod]
        public void GetPlayableCards_AllCardsFree_WhenDuplicateRuleViolation_ReturnsCardsListWithoutViolation()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set played card list
            players[1].PlayedCards.Add(cardDS.GameCards[CardId.Caravansery]);
            players[1].PlayedCards.Add(cardDS.GameCards[CardId.Press2]);

            // set hand card list
            players[1].HandCards.Add(cardDS.GameCards[CardId.Caravansery]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.Baths]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.Sawmill]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.Press]);   // should be violation with Press2
            players[1].HandCards.Add(cardDS.GameCards[CardId.School]);
            players[1].HandCards.Add(cardDS.GameCards[CardId.Palace]);

            // Expected values
            List<Card> expP1PlayableCards = new()
            {
                cardDS.GameCards[CardId.Baths],
                cardDS.GameCards[CardId.Sawmill],
                cardDS.GameCards[CardId.School],
                cardDS.GameCards[CardId.Palace],
            };

            // Test
            players[1].PlayableCards = players[1].GetPlayableCards(players[1].HandCards, true);

            // Evaluate
            bool isMatch = players[1].PlayableCards.SequenceEqual(expP1PlayableCards);
            Assert.IsTrue(isMatch);
        }
    }
}
