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
    public class ScoreAfterPlayingCardTests
    {
        [TestMethod]
        public void ScoreAfterPlayingCivilianCard_NoLongTermChangesTest()
        {
            // Arrange
            Game testGame = new Game();
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockDeterministicAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set coins to 0 so its not added to the score
            players[1].Resources[Resource.Coins] = 0;

            // Expected values
            int expVictoryTokens = players[1].VictoryTokens;
            int expDefeatTokens = players[1].DefeatTokens;
            int expVictoryPoints = players[1].VictoryPoints;
            Dictionary<Resource, int> expResources = players[1].Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            List<Card> expPlayedCards = players[1].PlayedCards.Select(e => e).ToList();
            bool expExtraScience = players[1].ExtraScience;
            int expectedScore = 8;

            // Test
            DeterministicAI p1 = (DeterministicAI)players[1];
            int score = p1.ScoreAfterPlayingCard(testGame, cardDS.GameCards[CardId.Palace]);

            // Evaluate
            Assert.AreEqual(expectedScore, score);

            // evaluate no changes to original player 
            Assert.AreEqual(expVictoryTokens, p1.VictoryTokens);
            Assert.AreEqual(expDefeatTokens, p1.DefeatTokens);
            Assert.AreEqual(expVictoryPoints, p1.VictoryPoints);
            Assert.AreEqual(expExtraScience, p1.ExtraScience);
            bool isResourceMatch = p1.Resources.OrderBy(kvp => kvp.Key)
                    .SequenceEqual(expResources.OrderBy(kvp => kvp.Key));
            Assert.IsTrue(isResourceMatch);
            bool isPlayedCardsMatch = p1.PlayedCards.SequenceEqual(expPlayedCards);
            Assert.IsTrue(isPlayedCardsMatch);
        }

        [TestMethod]
        public void ScoreAfterPlayingStrategyGuildCard_NoLongTermChangesTest()
        {
            // Arrange
            Game testGame = new Game();
            testGame.Era = 3;
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockDeterministicAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set coins to 0 so its not added to the score
            players[1].Resources[Resource.Coins] = 0;

            // set cards for situation
            players[1].Resources[Resource.Shields] = 3;

            // Expected values
            int expVictoryTokens = players[1].VictoryTokens;
            int expDefeatTokens = players[1].DefeatTokens;
            int expVictoryPoints = players[1].VictoryPoints;
            Dictionary<Resource, int> expResources = players[1].Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            List<Card> expPlayedCards = players[1].PlayedCards.Select(e => e).ToList();
            bool expExtraScience = players[1].ExtraScience;
            int expectedScore = 10;

            // Test
            DeterministicAI p1 = (DeterministicAI)players[1];
            int score = p1.ScoreAfterPlayingCard(testGame, cardDS.GameCards[CardId.Strategists]);

            // Evaluate
            Assert.AreEqual(expectedScore, score);

            // evaluate no changes to original player 
            Assert.AreEqual(expVictoryTokens, p1.VictoryTokens);
            Assert.AreEqual(expDefeatTokens, p1.DefeatTokens);
            Assert.AreEqual(expVictoryPoints, p1.VictoryPoints);
            Assert.AreEqual(expExtraScience, p1.ExtraScience);
            bool isResourceMatch = p1.Resources.OrderBy(kvp => kvp.Key)
                    .SequenceEqual(expResources.OrderBy(kvp => kvp.Key));
            Assert.IsTrue(isResourceMatch);
            bool isPlayedCardsMatch = p1.PlayedCards.SequenceEqual(expPlayedCards);
            Assert.IsTrue(isPlayedCardsMatch);
        }

        [TestMethod]
        public void ScoreAfterPlayingMilitaryCard_NoLongTermChangesTest()
        {
            // Arrange
            Game testGame = new Game();
            testGame.Era = 3;
            List<Wonder> allWonders = MockHelper.MockWonders();
            List<Wonder> selectWonders = allWonders.GetRange(0, 3);
            List<Player> players = MockHelper.MockDeterministicAIPlayers(selectWonders);
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set coins to 0 so its not added to the score
            players[1].Resources[Resource.Coins] = 0;

            // Expected values
            int expVictoryTokens = players[1].VictoryTokens;
            int expDefeatTokens = players[1].DefeatTokens;
            int expVictoryPoints = players[1].VictoryPoints;
            Dictionary<Resource, int> expResources = players[1].Resources.ToDictionary(entry => entry.Key, entry => entry.Value);
            List<Card> expPlayedCards = players[1].PlayedCards.Select(e => e).ToList();
            bool expExtraScience = players[1].ExtraScience;
            int expectedScore = 10;

            // Test
            DeterministicAI p1 = (DeterministicAI)players[1];
            int score = p1.ScoreAfterPlayingCard(testGame, cardDS.GameCards[CardId.Walls]);

            // Evaluate
            Assert.AreEqual(expectedScore, score);

            // evaluate no changes to original player 
            Assert.AreEqual(expVictoryTokens, p1.VictoryTokens);
            Assert.AreEqual(expDefeatTokens, p1.DefeatTokens);
            Assert.AreEqual(expVictoryPoints, p1.VictoryPoints);
            Assert.AreEqual(expExtraScience, p1.ExtraScience);
            bool isResourceMatch = p1.Resources.OrderBy(kvp => kvp.Key)
                    .SequenceEqual(expResources.OrderBy(kvp => kvp.Key));
            Assert.IsTrue(isResourceMatch);
            bool isPlayedCardsMatch = p1.PlayedCards.SequenceEqual(expPlayedCards);
            Assert.IsTrue(isPlayedCardsMatch);
        }
    }
}
