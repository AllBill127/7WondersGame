using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Engine.Rules;

/// <summary>Checking whether a player can build its next wonder stage.</summary>
[TestClass]
public class CheckCanBuildWonderTests
{

    [TestMethod]
    public void CheckCanBuildWonder_WhenAvailable_ReturnsTrue()
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

        // Expected values
        int expThisPlayerCoins = 3;
        int expEastPlayerCoins = 3;
        int expWestPlayerCoins = 3;

        // Test
        bool canBuildWonder = BuildRules.CanBuildWonderStage(players[1]);

        // Evaluate
        Assert.IsTrue(canBuildWonder);
        // eval resources unchanged
        Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
        Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
        Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
    }
}
