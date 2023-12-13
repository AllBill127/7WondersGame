using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGameTests.PlayerTests
{
    [TestClass]
    public class ChooseMoveTests
    {
        // TODO:    test ChooseMoveCard() when
        //          - cardList is empty
        //          - at least one card in cards list

        // TODO:    test ChooseMoveCommand when
        //          - build_structure -> PlayableCards contains player command argument
        //          - any other command -> HandCards contain player command argumnt
        //          - no PlayableCards -> player command is skip_move (check also when using OlympiaA stage2 (playableCards should be hand cards and action = discard))

        [TestMethod]
        public void ChooseMoveCommand_WhenOlympiaA2_AndPlayableCardsViolateNoDuplicateRule_SetCommandToDiscard()
        {
            // Arrange
            Game testGame = new Game();
            List<Wonder> wonders = new List<Wonder>()
            {
                new GizahA(),
                new OlympiaA(),
                new BabylonA(),
            };
            List<Player> players = MockHelper.MockPriorityAIPlayers(wonders);
            testGame.Players = players;
            testGame.PlayerCommands = new Command[players.Count];
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set wonder stage and effect
            players[1].Board.Stage = 2;
            players[1].FreeCardOnce = true;

            // set played cards for situation
            players[1].PlayedCards.Add(cardDS.GameCards[CardId.Forum]);

            // set hand cards for situation
            players[1].HandCards.Add(cardDS.GameCards[CardId.Forum]);

            // Expected values
            string expP1Subcommand = "discard";

            // Test
            players[1].ChooseMoveCommand(testGame);

            // Evaluate
            Assert.AreEqual(expP1Subcommand, testGame.PlayerCommands[players[1].Id].Subcommand);
        }

        [TestMethod]
        public void ChooseMoveCommand_WhenOlympiaA2_NoResourceToBuildCard_SetCommandToBuildHandFree()
        {
            // Arrange
            Game testGame = new Game();
            List<Wonder> wonders = new List<Wonder>()
            {
                new GizahA(),
                new OlympiaA(),
                new BabylonA(),
            };
            List<Player> players = MockHelper.MockPriorityAIPlayers(wonders);
            testGame.Players = players;
            testGame.PlayerCommands = new Command[players.Count];
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set wonder stage and effect
            players[1].Board.Stage = 2;
            players[1].FreeCardOnce = true;

            // set played cards for situation
            players[1].PlayedCards.Add(cardDS.GameCards[CardId.Forum]);

            // set hand cards for situation
            players[1].HandCards.Add(cardDS.GameCards[CardId.Palace]);

            // Expected values
            string expP1Subcommand = "build_hand_free";

            // Test
            players[1].ChooseMoveCommand(testGame);

            // Evaluate
            Assert.AreEqual(expP1Subcommand, testGame.PlayerCommands[players[1].Id].Subcommand);
        }
    }
}
