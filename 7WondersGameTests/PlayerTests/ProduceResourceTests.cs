using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace _7WondersGameTests.PlayerTests
{
    [TestClass]
    public class ProduceResourceTests
    {
        [TestMethod]
        public void ProduceMultipleResource_WhenOnlyNeighborsHaveIt_ReturnsTrue()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // give resources for situation
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.TreeFarm]);
            players[1].Resources[Resource.Coins] = 4;
            players[2].PlayedCards.Add(cardDS.GameCards[CardId.ClayPit]);

            // make required resource list
            Dictionary<Resource, int> neededResources = new Dictionary<Resource, int>()
            {
                { Resource.Wood, 1 },
                { Resource.Clay, 1 },
            };

            // Expected values
            int expThisPlayerCoins = 0;
            int expEastPlayerCoins = 5;
            int expWestPlayerCoins = 5;

            // Test
            bool canProduce = false;
            foreach (var resourceKvp in neededResources)
            {
                canProduce = players[1].ProduceResource(resourceKvp.Key, resourceKvp.Value);
            }

            // Evaluate
            Assert.IsTrue(canProduce);
            // eval resource change
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
        }

        [TestMethod]
        public void ProduceMultipleResource_WhenOnlyNeighborsHaveIt_CantSellCommercialResource_ReturnsFalse()
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

            // make required resource list
            Dictionary<Resource, int> neededResources = new Dictionary<Resource, int>()
            {
                { Resource.Wood, 1 },
                { Resource.Clay, 1 },
                { Resource.Glass, 1 },
            };

            // Expected values
            int expThisPlayerCoins = 2; // paid for wood and clay
            int expEastPlayerCoins = 5; // sold wood
            int expWestPlayerCoins = 5; // sold clay

            // Test
            bool canProduce = false;
            foreach (var resourceKvp in neededResources)
            {
                canProduce = players[1].ProduceResource(resourceKvp.Key, resourceKvp.Value);
            }

            // Evaluate
            Assert.IsFalse(canProduce);
            // eval resource change
            Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
            // eval usedOnDemandResource not reset
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
                Assert.IsFalse(isMatch);
            }
        }

        // TODO:    test when getOnDemand default resources are more than needed (return of line 821 and evaluation on line 984 (missing < 0))
    }
}