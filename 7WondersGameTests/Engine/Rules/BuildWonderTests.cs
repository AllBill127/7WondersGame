using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Engine.Rules;

/// <summary>Building a wonder stage: the card leaves the hand and the trade is paid.</summary>
[TestClass]
public class BuildWonderTests
{

    [TestMethod]
    public void BuildWonder_WhenAvailable_ReturnsTrue()
    {
        // Arrange
        List<WonderBoardProgress> allWonders = MockHelper.MockWonders();
        List<WonderBoardProgress> selectWonders = allWonders.GetRange(0, 3);
        List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

        // give resources for situation
        players[0].BuiltCards.Add(CardsDataset.GameCards[CardId.ClayPool]);
        // manually add clay from clayPool card (building the card would add it; not done here)
        players[0].Resources[Resource.Clay] += 1;
        players[1].Resources[Resource.Coins] = 3;
        players[1].BuiltCards.Add(CardsDataset.GameCards[CardId.ClayPit]);

        // set hand card for situation
        Card player1Card = CardsDataset.GameCards[CardId.Baths];
        players[1].Hand.Add(player1Card);

        // Expected values
        int expThisPlayerCoins = 1;
        int expEastPlayerCoins = 5;
        int expWestPlayerCoins = 3;

        // Test
        players[1].CanBuildWonder();
        bool canBuildWonder = MoveTestHelper.BuildWonderStage(players[1], player1Card, players[1].Hand);

        // Evaluate
        Assert.IsTrue(canBuildWonder);
        Assert.IsFalse(players[1].CanBuildWonder());
        // eval hand card change
        bool cardInHand = players[1].Hand.Contains<Card>(player1Card);
        Assert.IsFalse(cardInHand);
        // eval resources change
        Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
        Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
        Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
    }
}
