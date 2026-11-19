using _7Wonders.Agents.Common.Entities;
using _7Wonders.Agents.Heuristic;
using _7Wonders.Agents.RandomChoice;
using _7Wonders.Engine.Cards.Data;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Moves.Dtos;
using _7Wonders.Engine.Moves.Enums;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Wonders.Entities;
using _7Wonders.Engine.Wonders.Enums;
using _7Wonders.Tests.TestHelpers;

namespace _7Wonders.Tests.Agents;

/// <summary>The moves the random and heuristic agents choose when Olympia A's free build is available.</summary>
[TestClass]
public class ChooseMoveTests
{
    [TestMethod]
    public void ChooseMove_WhenOlympiaA2_AndBuildableCardsViolateNoDuplicateRule_ChoosesDiscard()
    {
        // Arrange
        Game testGame = new(seed: 0);
        List<WonderBoardProgress> wonders = new()
        {
            new WonderBoardProgress(WonderId.GizahA),
            new WonderBoardProgress(WonderId.OlympiaA),
            new WonderBoardProgress(WonderId.BabylonA),
        };
        List<PlayerState> players = MockHelper.MockPlayers(wonders);
        testGame.Players = players;

        // set wonder stage and effect
        players[1].Wonder.BuiltStageCount = 2;
        players[1].OlympiaFreeBuildAvailable = true;

        // set played cards for situation
        players[1].BuiltCards.Add(CardsDataset.GameCards[CardId.Forum]);

        // set hand cards for situation
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Forum]);

        // Expected values
        MoveAction expectedAction = MoveAction.Discard;

        // Test
        Move move = new RandomAgent().Choose(new GameView(testGame, 1));

        // Evaluate
        Assert.AreEqual(expectedAction, move.Action);
    }

    [TestMethod]
    public void ChooseMove_WhenOlympiaA2_NoResourceToBuildCard_ChoosesOlympiaFreeBuild()
    {
        // Arrange
        Game testGame = new(seed: 0);
        List<WonderBoardProgress> wonders = new()
        {
            new WonderBoardProgress(WonderId.GizahA),
            new WonderBoardProgress(WonderId.OlympiaA),
            new WonderBoardProgress(WonderId.BabylonA),
        };
        List<PlayerState> players = MockHelper.MockPlayers(wonders);
        testGame.Players = players;

        // set wonder stage and effect
        players[1].Wonder.BuiltStageCount = 2;
        players[1].OlympiaFreeBuildAvailable = true;

        // set played cards for situation
        players[1].BuiltCards.Add(CardsDataset.GameCards[CardId.Forum]);

        // set hand cards for situation
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Palace]);

        // Expected values
        MoveAction expectedAction = MoveAction.BuildWithOlympiaFreeBuild;

        // Test
        Move move = new HeuristicAgent().Choose(new GameView(testGame, 1));

        // Evaluate
        Assert.AreEqual(expectedAction, move.Action);
    }
}
