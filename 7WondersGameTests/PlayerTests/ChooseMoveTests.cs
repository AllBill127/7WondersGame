using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

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
                new Wonder(WonderId.GizahA),
                new Wonder(WonderId.OlympiaA),
                new Wonder(WonderId.BabylonA),
            };
            List<PlayerState> players = MockHelper.MockPlayers(wonders);
            testGame.Players = players;

            // set wonder stage and effect
            players[1].Board.Stage = 2;
            players[1].FreeCardOnce = true;

            // set played cards for situation
            players[1].PlayedCards.Add(CardsDataset.GameCards[CardId.Forum]);

            // set hand cards for situation
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Forum]);

            // Expected values
            ActionType expP1Action = ActionType.Discard;

            // Test
            Command move = new PriorityRandomChoiceAI(ActionPriority.Wonder).Choose(new GameView(testGame, 1));

            // Evaluate
            Assert.AreEqual(expP1Action, move.Action);
        }

        [TestMethod]
        public void ChooseMoveCommand_WhenOlympiaA2_NoResourceToBuildCard_SetCommandToBuildHandFree()
        {
            // Arrange
            Game testGame = new Game();
            List<Wonder> wonders = new List<Wonder>()
            {
                new Wonder(WonderId.GizahA),
                new Wonder(WonderId.OlympiaA),
                new Wonder(WonderId.BabylonA),
            };
            List<PlayerState> players = MockHelper.MockPlayers(wonders);
            testGame.Players = players;

            // set wonder stage and effect
            players[1].Board.Stage = 2;
            players[1].FreeCardOnce = true;

            // set played cards for situation
            players[1].PlayedCards.Add(CardsDataset.GameCards[CardId.Forum]);

            // set hand cards for situation
            players[1].HandCards.Add(CardsDataset.GameCards[CardId.Palace]);

            // Expected values
            ActionType expP1Action = ActionType.BuildFreeOnce;

            // Test
            Command move = new PriorityRandomChoiceAI(ActionPriority.Wonder).Choose(new GameView(testGame, 1));

            // Evaluate
            Assert.AreEqual(expP1Action, move.Action);
        }
    }
}
