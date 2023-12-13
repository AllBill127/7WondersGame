using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using _7WondersGame.src.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGameTests.GameTests
{
    [TestClass]
    public class ExtraLoopTests
    {
        [TestMethod]
        public void ExtraLoop_DiscardFree_WithDiscardedCardsEmpty_DiscardFreeResetToFalse()
        {
            // Arrange
            Game testGame = new Game();
            List<Wonder> wonders = new List<Wonder>()
            {
                new GizahA(),
                new HalikarnassosA(),
                new BabylonA(),
            };
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(wonders);
            testGame.Players = players;
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set wonder stage and effect
            players[1].Board.Stage = 2;
            players[1].DiscardFree = true;

            // empty discarded cards list
            testGame.DiscardedCards.Clear();

            // Test
            testGame.ExtraLoop(players[1], discardFree:true);

            // Evaluate
            Assert.IsFalse(players[1].DiscardFree);
        }

        [TestMethod]
        public void ExtraLoop_DiscardFree_WithDiscardedExpensiveCards_DiscardFreeResetToFalse()
        {
            // Arrange
            Game testGame = new Game();
            List<Wonder> wonders = new List<Wonder>()
            {
                new GizahA(),
                new HalikarnassosA(),
                new BabylonA(),
            };
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(wonders);
            testGame.Players = players;
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set wonder stage and effect
            players[1].Board.Stage = 2;
            players[1].DiscardFree = true;

            // empty discarded cards list
            testGame.DiscardedCards.Clear();
            testGame.DiscardedCards.Add(cardDS.GameCards[CardId.Palace]);

            // Test
            testGame.ExtraLoop(players[1], discardFree: true);

            // Evaluate
            Assert.IsFalse(players[1].DiscardFree);
            Assert.IsFalse(testGame.DiscardedCards.Contains(cardDS.GameCards[CardId.Palace]));
            Assert.IsTrue(players[1].PlayedCards.Contains(cardDS.GameCards[CardId.Palace]));
        }

        // TODO:    test when PlaySeventh effect if PlayersReady[player.Id] == false after extra loop
    }
}
