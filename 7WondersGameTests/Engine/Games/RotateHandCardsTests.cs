using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Engine.Games;

/// <summary>Passing the hands round the table.</summary>
[TestClass]
public class RotateHandCardsTests
{
    [TestMethod]
    public void RotateHandCards_CorrectlyRotatesCardListsClockwise()
    {
        // Arrange
        Game testGame = new Game(seed: 0);
        testGame.Era = 1;

        List<WonderBoardProgress> wonders = MockHelper.MockWonders().GetRange(0, 3);
        List<PlayerState> players = MockHelper.MockPlayers(wonders);
        testGame.Players = players;

        // add some cards
        testGame.Players[0].Hand.Add(CardsDataset.GameCards[CardId.ClayPool]);
        testGame.Players[0].Hand.Add(CardsDataset.GameCards[CardId.Pantheon]);
        testGame.Players[0].Hand.Add(CardsDataset.GameCards[CardId.Dispensary]);

        testGame.Players[1].Hand.Add(CardsDataset.GameCards[CardId.OreVein]);
        testGame.Players[1].Hand.Add(CardsDataset.GameCards[CardId.Baths]);
        testGame.Players[1].Hand.Add(CardsDataset.GameCards[CardId.Lodge]);

        testGame.Players[2].Hand.Add(CardsDataset.GameCards[CardId.Quarry]);
        testGame.Players[2].Hand.Add(CardsDataset.GameCards[CardId.Statue]);
        testGame.Players[2].Hand.Add(CardsDataset.GameCards[CardId.School]);

        // Expected results
        List<Card> expectedHandOfSeat0 = new List<Card>(testGame.Players[1].Hand);
        List<Card> expectedHandOfSeat1 = new List<Card>(testGame.Players[2].Hand);
        List<Card> expectedHandOfSeat2 = new List<Card>(testGame.Players[0].Hand);

        // Test
        testGame.RotateHandCards();

        // Evaluate
        bool isMatch = players[0].Hand.SequenceEqual(expectedHandOfSeat0);
        Assert.IsTrue(isMatch);
        isMatch = players[1].Hand.SequenceEqual(expectedHandOfSeat1);
        Assert.IsTrue(isMatch);
        isMatch = players[2].Hand.SequenceEqual(expectedHandOfSeat2);
        Assert.IsTrue(isMatch);
    }
}
