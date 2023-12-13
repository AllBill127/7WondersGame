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
    public class BuildStructureTests
    {

        [TestMethod]
        public void BuildStructure_WhenCardIsPlayable_ReturnsTrue()
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
            int expThisPlayerCoins = 0;
            int expEastPlayerCoins = 5;
            int expWestPlayerCoins = 5;

            // Test
            bool buildSuccess = players[1].BuildStructure(
                cardDS.GameCards[CardId.Caravansery],
                players[1].HandCards,
                false);

            // Evaluate
            Assert.IsTrue(buildSuccess);
            // eval hand card change
            bool cardInHand = players[1].HandCards.Contains<Card>(cardDS.GameCards[CardId.Caravansery]);
            Assert.IsFalse(cardInHand);
            bool cardInPlayed = players[1].PlayedCards.Contains<Card>(cardDS.GameCards[CardId.Caravansery]);
            Assert.IsTrue(cardInPlayed);
            // eval resource change
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
        public void BuildStructure_WhenCardIsPlayable_CostsOnlyCoins_ReturnsTrue()
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
            players[1].HandCards.Add(cardDS.GameCards[CardId.Sawmill]);

            // Expected values
            int expThisPlayerCoins = 3;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            bool buildSuccess = players[1].BuildStructure(
                cardDS.GameCards[CardId.Sawmill],
                players[1].HandCards,
                false);

            // Evaluate
            Assert.IsTrue(buildSuccess);
            // eval hand card change
            bool cardInHand = players[1].HandCards.Contains<Card>(cardDS.GameCards[CardId.Sawmill]);
            Assert.IsFalse(cardInHand);
            bool cardInPlayed = players[1].PlayedCards.Contains<Card>(cardDS.GameCards[CardId.Sawmill]);
            Assert.IsTrue(cardInPlayed);
            // eval resource change
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
        public void BuildStructure_WhenCardIsNotPlayable_CostsOnlyCoins_ReturnsTrue()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // give resources for situation
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.TreeFarm]);
            players[1].Resources[Resource.Coins] = 0;
            players[2].PlayedCards.Add(cardDS.GameCards[CardId.TimberYard]);

            // set hand card list
            players[1].HandCards.Add(cardDS.GameCards[CardId.Sawmill]);

            // Expected values
            int expThisPlayerCoins = 0;
            int expEastPlayerCoins = 3;
            int expWestPlayerCoins = 3;

            // Test
            bool buildSuccess = players[1].BuildStructure(
                cardDS.GameCards[CardId.Sawmill],
                players[1].HandCards,
                false);

            // Evaluate
            Assert.IsFalse(buildSuccess);
            // eval hand card change
            bool cardInHand = players[1].HandCards.Contains<Card>(cardDS.GameCards[CardId.Sawmill]);
            Assert.IsTrue(cardInHand);
            bool cardInPlayed = players[1].PlayedCards.Contains<Card>(cardDS.GameCards[CardId.Sawmill]);
            Assert.IsFalse(cardInPlayed);
            // eval resource change
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
        public void BuildStructure_WhenCardIsNotPlayable_CantSellComercialResource_ReturnsFalse()
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
            bool buildSuccess = players[1].BuildStructure(
                cardDS.GameCards[CardId.Temple],
                players[1].HandCards,
                false);

            // Evaluate
            Assert.IsFalse(buildSuccess);
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
        public void BuildStructure_Loom2_Adds_LoomResource()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set hand card list
            players[1].HandCards.Add(cardDS.GameCards[CardId.Loom2]);

            // Expected values
            int expP1ResourceLoom = 1;

            // Test
            bool buildSuccess = players[1].BuildStructure(
                cardDS.GameCards[CardId.Loom2],
                players[1].HandCards,
                false);

            // Evaluate
            Assert.IsTrue(buildSuccess);
            // eval resources unchanged
            Assert.AreEqual(expP1ResourceLoom, players[1].Resources[Resource.Loom]);
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
    }
}
