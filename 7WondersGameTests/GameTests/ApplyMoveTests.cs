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
    public class ApplyMoveTests
    {
        [TestMethod]
        public void ApplyMove_FirstPlayerMoveSet_GameStateUnchanged()
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

            // test player (no neighbors)
            Player testPlayer = (Player)testGame.Players[0].DeepCopy();
            // any random command
            Command testCommand = new Command("build_structure", cardDS.GameCards[CardId.Baths]);

            // Expected values
            int expTurn = testGame.Turn;
            Player expNextPlayer = testGame.Players[2]; // test player idx - 1;

            // Act
            Player? nextPlayer = testGame.ApplyMove(testPlayer, testCommand);

            // Evaluate
            Assert.IsTrue(testGame.PlayersReady[0]);
            Assert.AreEqual(expTurn, testGame.Turn);
            Assert.AreEqual(expNextPlayer.Id, nextPlayer?.Id);
        }

        [TestMethod]
        public void ApplyMove_LastPlayerMove_GameStateUpdatedCorrectly()
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

            // set player cards for situation
            testGame.Players[0].HandCards.Add(cardDS.GameCards[CardId.Baths]);
            testGame.Players[1].HandCards.Add(cardDS.GameCards[CardId.Walls]);
            testGame.Players[2].HandCards.Add(cardDS.GameCards[CardId.ClayPit]);

            // set other player moves
            testGame.PlayerCommands[1] = new Command("discard", cardDS.GameCards[CardId.Walls]);
            testGame.PlayersReady[1] = true;
            testGame.PlayerCommands[2] = new Command("discard", cardDS.GameCards[CardId.ClayPit]);
            testGame.PlayersReady[2] = true;

            // test player (no neighbors)
            Player testPlayer = (Player)testGame.Players[0].DeepCopy();
            // any random command
            Command testCommand = new Command("build_structure", cardDS.GameCards[CardId.Baths]);

            // Expected values
            int expTurn = testGame.Turn + 1;
            Player expNextPlayer = testGame.Players[2]; // test player idx - 1;

            // Act
            Player? nextPlayer = testGame.ApplyMove(testPlayer, testCommand);

            // Evaluate
            Assert.IsFalse(testGame.PlayersReady[0]);
            Assert.AreEqual(expTurn, testGame.Turn);
            Assert.AreEqual(expNextPlayer.Id, nextPlayer?.Id);
        }

        [TestMethod]
        public void ApplyMove_LastPlayerMove_LastAge1Turn_GameStateUpdatedCorrectly()
        {
            // Arrange
            Game testGame = new Game();
            testGame.Turn = 5;  // 6-th card being chosen but turns are counted from 0
            testGame.CreateDecks();
            List<Wonder> wonders = new List<Wonder>()
            {
                new GizahA(),
                new HalikarnassosA(),
                new BabylonA(),
            };
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(wonders);
            testGame.Players = players;
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set player cards for situation
            testGame.Players[0].HandCards.Add(cardDS.GameCards[CardId.Baths]);
            testGame.Players[1].HandCards.Add(cardDS.GameCards[CardId.Walls]);
            testGame.Players[2].HandCards.Add(cardDS.GameCards[CardId.ClayPit]);

            // set other player moves
            testGame.PlayerCommands[1] = new Command("discard", cardDS.GameCards[CardId.Walls]);
            testGame.PlayersReady[1] = true;
            testGame.PlayerCommands[2] = new Command("discard", cardDS.GameCards[CardId.ClayPit]);
            testGame.PlayersReady[2] = true;

            // test player (no neighbors)
            Player testPlayer = (Player)testGame.Players[0].DeepCopy();
            // any random command
            Command testCommand = new Command("build_structure", cardDS.GameCards[CardId.Baths]);

            // Expected values
            int expTurn = 7;
            Player expNextPlayer = testGame.Players[0]; // uncertain if correct but returns Players[0];

            // Act
            Player? nextPlayer = testGame.ApplyMove(testPlayer, testCommand);

            // Evaluate
            Assert.IsFalse(testGame.PlayersReady[0]);
            Assert.AreEqual(expTurn, testGame.Turn);
            Assert.AreEqual(expNextPlayer.Id, nextPlayer?.Id);
            Assert.IsTrue(testGame.Players[0].HandCards.Count == 7);
        }

        [TestMethod]
        public void ApplyMove_LastPlayerMove_LastAge2Turn_GameStateUpdatedCorrectly()
        {
            // Arrange
            Game testGame = new Game();
            testGame.Turn = 13;  // 6-th card being chosen but turns are counted from 0 and age 2
            testGame.CreateDecks();
            List<Wonder> wonders = new List<Wonder>()
            {
                new GizahA(),
                new HalikarnassosA(),
                new BabylonA(),
            };
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(wonders);
            testGame.Players = players;
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set player cards for situation
            testGame.Players[0].HandCards.Add(cardDS.GameCards[CardId.Baths]);
            testGame.Players[1].HandCards.Add(cardDS.GameCards[CardId.Walls]);
            testGame.Players[2].HandCards.Add(cardDS.GameCards[CardId.ClayPit]);

            // set other player moves
            testGame.PlayerCommands[1] = new Command("discard", cardDS.GameCards[CardId.Walls]);
            testGame.PlayersReady[1] = true;
            testGame.PlayerCommands[2] = new Command("discard", cardDS.GameCards[CardId.ClayPit]);
            testGame.PlayersReady[2] = true;

            // test player (no neighbors)
            Player testPlayer = (Player)testGame.Players[0].DeepCopy();
            // any random command
            Command testCommand = new Command("build_structure", cardDS.GameCards[CardId.Baths]);

            // Expected values
            int expTurn = 14;
            Player expNextPlayer = testGame.Players[0]; // uncertain if correct but returns Players[0];

            // Act
            Player? nextPlayer = testGame.ApplyMove(testPlayer, testCommand);

            // Evaluate
            Assert.IsFalse(testGame.PlayersReady[0]);
            Assert.AreEqual(expTurn, testGame.Turn);
            Assert.AreEqual(expNextPlayer.Id, nextPlayer?.Id);
            Assert.IsTrue(testGame.Players[0].HandCards.Count == 7);
        }

        [TestMethod]
        public void ApplyMove_LastPlayerMove_DiscardFreeNode_GameStateUpdatedCorrectly()
        {
            // Arrange
            Game testGame = new Game();
            testGame.Turn = 5;  // 6-th card being chosen but turns are counted from 0
            testGame.CreateDecks();
            List<Wonder> wonders = new List<Wonder>()
            {
                new HalikarnassosB(),
                new GizahA(),
                new BabylonA(),
            };
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(wonders);
            testGame.Players = players;
            CardsDataset cardDS = CardsDataset.GetInstance(players.Count);

            // set player resources for situation
            testGame.Players[0].Resources[Resource.Ore] += 2;

            // set Halikarnassos wonder to buildable for situation
            testGame.Players[0].CanBuildWonder = true;

            // set player cards for situation
            testGame.Players[0].HandCards.Add(cardDS.GameCards[CardId.Baths]);
            testGame.Players[1].HandCards.Add(cardDS.GameCards[CardId.Walls]);
            testGame.Players[2].HandCards.Add(cardDS.GameCards[CardId.ClayPit]);

            // set other player moves
            testGame.PlayerCommands[1] = new Command("discard", cardDS.GameCards[CardId.Walls]);
            testGame.PlayersReady[1] = true;
            testGame.PlayerCommands[2] = new Command("discard", cardDS.GameCards[CardId.ClayPit]);
            testGame.PlayersReady[2] = true;

            // test player (no neighbors)
            Player testPlayer = (Player)testGame.Players[0].DeepCopy();
            // any random command
            Command testCommand = new Command("build_wonder", cardDS.GameCards[CardId.Baths]);

            // Expected values
            int expTurn = 7;
            Player expNextPlayer = testGame.Players[0];

            // Act
            Player? nextPlayer = testGame.ApplyMove(testPlayer, testCommand);

            // Evaluate
            Assert.IsFalse(testGame.PlayersReady[0]);
            Assert.IsFalse(testGame.Players[0].CanBuildWonder);
            Assert.IsTrue(testGame.Players[0].DiscardFree);
            Assert.AreEqual(expTurn, testGame.Turn);
            Assert.AreEqual(expNextPlayer.Id, nextPlayer?.Id);
            Assert.IsTrue(testGame.Players[0].HandCards.Count == 0);
        }
    }
}
