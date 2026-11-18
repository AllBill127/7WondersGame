using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGame.Engine.Wonders.Enums;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Engine.Rules;

/// <summary>
/// The Lighthouse counts itself: it gives 1 coin (when built) and 1 VP (at the end)
/// per yellow card in the city, including the Lighthouse.
/// </summary>
[TestClass]
public class LighthouseTests
{
    private static PlayerState PlayerWithTwoYellowCards()
    {
        List<Wonder> wonders = new List<Wonder>
        {
            new Wonder(WonderId.GizahA),
            new Wonder(WonderId.BabylonA),
            new Wonder(WonderId.OlympiaA)
        };
        List<PlayerState> players = MockHelper.MockPlayers(wonders);
        PlayerState player = players[0];
        player.PlayedCards.Add(CardsDataset.GameCards[CardId.Marketplace]);
        player.PlayedCards.Add(CardsDataset.GameCards[CardId.Tavern]);
        return player;
    }

    [TestMethod]
    public void BuildCard_Lighthouse_CountsItselfForCoins()
    {
        var player = PlayerWithTwoYellowCards();
        Card lighthouse = CardsDataset.GameCards[CardId.Lighthouse];

        Assert.IsTrue(MoveTestHelper.BuildCard(player, lighthouse, new List<Card> { lighthouse }, true));

        // 3 starting coins + 3 yellow cards (Marketplace, Tavern, Lighthouse)
        Assert.AreEqual(6, player.Resources[Resource.Coins]);
        Assert.AreEqual(3, player.CalculateCommercialScore());
    }

    [TestMethod]
    public void BuildCard_Lighthouse_CountsItselfInTheScore()
    {
        var player = PlayerWithTwoYellowCards();
        Card lighthouse = CardsDataset.GameCards[CardId.Lighthouse];

        Assert.IsTrue(MoveTestHelper.BuildCard(player, lighthouse, new List<Card> { lighthouse }, true));
        player.Battle(3);

        // 3 VP from the Lighthouse + 6 coins / 3 = 2 VP (no shields anywhere, so no military points)
        Assert.AreEqual(5, player.CalculateScore());
    }
}
