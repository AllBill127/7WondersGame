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
    public class PlayerIntegrationTest
    {

        [TestMethod]
        public void GetPlayable_And_BuildStructure_WhenCardIsPlayable_Test()
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
            players[1].PlayableCards = players[1].GetPlayableCards(players[1].HandCards);
            bool canPlayCard = players[1].PlayableCards.Contains<Card>(cardDS.GameCards[CardId.Caravansery]);
            Assert.IsTrue(canPlayCard);

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
        public void GetPlayable_And_BuildStructure_WhenCardIsNotPlayable_Test()
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
            players[1].PlayableCards = players[1].GetPlayableCards(players[1].HandCards);
            bool canPlayCard = players[1].PlayableCards.Contains<Card>(cardDS.GameCards[CardId.Temple]);
            Assert.IsFalse(canPlayCard);

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
        public void Multiplayer_GetPlayable_And_BuildStructure_Test()
        {
            // Arrange
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // give resources for situation
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.TreeFarm]);
            players[1].PlayedCards.Add(cardDS.GameCards[CardId.Mine]);
            players[1].Resources[Resource.Coins] = 6;
            players[2].PlayedCards.Add(cardDS.GameCards[CardId.ForestCave]);
            players[2].PlayedCards.Add(cardDS.GameCards[CardId.Forum]);

            // set hand card for situation
            Card player0Card = cardDS.GameCards[CardId.Baths];
            Card player1Card = cardDS.GameCards[CardId.Caravansery];
            Card player2Card = cardDS.GameCards[CardId.Dispensary];

            players[0].HandCards.Add(player0Card);
            players[1].HandCards.Add(player1Card);
            players[2].HandCards.Add(player2Card);

            // Expected values
            int expPlayer0Coins = 3;
            int expPlayer1Coins = 6;
            int expPlayer2Coins = 3;

            // Test
            players[0].PlayableCards = players[0].GetPlayableCards(players[0].HandCards);
            bool canPlayCard = players[0].PlayableCards.Contains<Card>(player0Card);
            Assert.IsTrue(canPlayCard);
            players[1].PlayableCards = players[1].GetPlayableCards(players[1].HandCards);
            canPlayCard = players[1].PlayableCards.Contains<Card>(player1Card);
            Assert.IsTrue(canPlayCard);
            players[2].PlayableCards = players[2].GetPlayableCards(players[2].HandCards);
            canPlayCard = players[2].PlayableCards.Contains<Card>(player2Card);
            Assert.IsTrue(canPlayCard);

            bool build0Success = players[0].BuildStructure(
                player0Card,
                players[0].HandCards,
                false);
            bool build1Success = players[1].BuildStructure(
                player1Card,
                players[1].HandCards,
                false);
            bool build2Success = players[2].BuildStructure(
                player2Card,
                players[2].HandCards,
                false);

            // Evaluate
            Assert.IsTrue(build0Success);
            Assert.IsTrue(build1Success);
            Assert.IsTrue(build2Success);
            // eval resources unchanged
            Assert.AreEqual(expPlayer0Coins, players[0].Resources[Resource.Coins]);
            Assert.AreEqual(expPlayer1Coins, players[1].Resources[Resource.Coins]);
            Assert.AreEqual(expPlayer2Coins, players[2].Resources[Resource.Coins]);
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
