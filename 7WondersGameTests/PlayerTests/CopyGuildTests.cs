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
    public class CopyGuildTests
    {

        [TestMethod]
        public void CopyGuild_EffectActive_And_Single_GuildCopyable_ReturnsTrue_PlayerPlayedCardsContainGuild()
        {
            // Arrange
            List<Wonder> wonders = new List<Wonder>()
            {
                new GizahA(),
                new OlympiaB(),
                new BabylonA(),
            };
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(wonders);

            // set Babylon A stage 3 for CopyGuild effect
            players[1].Board.Stage = 3;

            // set cards for situation
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.Workers]);
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.LumberYard]);
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.ClayPit]);

            // Test
            bool copyGuildSuccess = players[1].CopyGuild();

            // Evaluate
            Assert.IsTrue(copyGuildSuccess);
            bool containsGuild = players[1].PlayedCards.Contains(cardDS.GameCards[CardId.Workers]);
            Assert.IsTrue(containsGuild);
            bool noCardChange = players[0].PlayedCards.Contains(cardDS.GameCards[CardId.Workers]);
            Assert.IsTrue(noCardChange);
        }

        [TestMethod]
        public void CopyGuild_EffectActive_And_Multiple_GuildsCopyable_ReturnsTrue_PlayerPlayedCardsContainBestGuild()
        {
            // Arrange
            List<Wonder> wonder = new List<Wonder>()
            {
                new GizahA(),
                new OlympiaB(),
                new BabylonA(),
            };
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(wonder);

            // set Babylon A stage 3 for CopyGuild effect
            players[1].Board.Stage = 3;

            // set cards for situation
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.Workers]);
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.ClayPit]);

            players[2].PlayedCards.Add(cardDS.GameCards[CardId.Spies]);
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.Stables]);
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.Walls]);
            players[0].PlayedCards.Add(cardDS.GameCards[CardId.Circus]);

            // Test
            bool copyGuildSuccess = players[1].CopyGuild();

            // Evaluate
            Assert.IsTrue(copyGuildSuccess);
            bool containsGuild = players[1].PlayedCards.Contains(cardDS.GameCards[CardId.Spies]);
            Assert.IsTrue(containsGuild);
            bool noCardChangeP1 = players[0].PlayedCards.Contains(cardDS.GameCards[CardId.Workers]);
            Assert.IsTrue(noCardChangeP1);
            bool noCardChangeP2 = players[2].PlayedCards.Contains(cardDS.GameCards[CardId.Spies]);
            Assert.IsTrue(noCardChangeP2);
        }

        [TestMethod]
        public void CopyGuild_EffectActive_And_No_GuildCopyable_ReturnsFalse_PlayerPlayedCardsDoesntContainGuild()
        {
            // Arrange
            List<Wonder> wonder = new List<Wonder>()
            {
                new GizahA(),
                new OlympiaB(),
                new BabylonA(),
            };
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(wonder);

            // set Babylon A stage 3 for CopyGuild effect
            players[1].Board.Stage = 3;

            // Expected values
            int expP1PlayedCardsLen = players[1].PlayedCards.Count;

            // Test
            bool copyGuildSuccess = players[1].CopyGuild();

            // Evaluate
            Assert.IsFalse(copyGuildSuccess);
            Assert.AreEqual(expP1PlayedCardsLen, players[1].PlayedCards.Count);
        }
    }
}
