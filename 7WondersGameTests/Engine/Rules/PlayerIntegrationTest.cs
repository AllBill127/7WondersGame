using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Engine.Rules;

/// <summary>Finding the playable cards and building one, with trade between neighbours.</summary>
[TestClass]
public class PlayerIntegrationTest
{

    [TestMethod]
    public void GetPlayable_And_BuildCard_WhenCardIsPlayable_Test()
    {
        // Arrange
        List<WonderBoardProgress> allWonders = MockHelper.MockWonders();
        List<WonderBoardProgress> selectWonders = allWonders.GetRange(0, 3);
        List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

        // give resources for situation
        players[0].BuiltCards.Add(CardsDataset.GameCards[CardId.TreeFarm]);
        players[1].Resources[Resource.Coins] = 4;
        players[2].BuiltCards.Add(CardsDataset.GameCards[CardId.TimberYard]);

        // set hand card list
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Caravansery]);

        // Expected values
        int expThisPlayerCoins = 0;
        int expEastPlayerCoins = 5;
        int expWestPlayerCoins = 5;

        // Test
        List<Card> buildable = BuildRules.BuildableCards(players[1], players[1].Hand);
        bool canPlayCard = buildable.Contains<Card>(CardsDataset.GameCards[CardId.Caravansery]);
        Assert.IsTrue(canPlayCard);

        bool buildSuccess = MoveTestHelper.BuildCard(players[1],
            CardsDataset.GameCards[CardId.Caravansery],
            players[1].Hand,
            false);

        // Evaluate
        Assert.IsTrue(buildSuccess);
        // eval hand card change
        bool cardInHand = players[1].Hand.Contains<Card>(CardsDataset.GameCards[CardId.Caravansery]);
        Assert.IsFalse(cardInHand);
        bool cardInPlayed = players[1].BuiltCards.Contains<Card>(CardsDataset.GameCards[CardId.Caravansery]);
        Assert.IsTrue(cardInPlayed);
        // eval resource change
        Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
        Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
        Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
    }

    [TestMethod]
    public void GetPlayable_And_BuildCard_WhenCardIsNotPlayable_Test()
    {
        // Arrange
        List<WonderBoardProgress> allWonders = MockHelper.MockWonders();
        List<WonderBoardProgress> selectWonders = allWonders.GetRange(0, 3);
        List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

        // give resources for situation
        players[0].BuiltCards.Add(CardsDataset.GameCards[CardId.TreeFarm]);
        players[1].Resources[Resource.Coins] = 6;
        players[2].BuiltCards.Add(CardsDataset.GameCards[CardId.ClayPit]);
        players[2].BuiltCards.Add(CardsDataset.GameCards[CardId.Forum]); // has untradeable glass

        // set hand card list
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Temple]);

        // Expected values
        int expThisPlayerCoins = 6;
        int expEastPlayerCoins = 3;
        int expWestPlayerCoins = 3;

        // Test
        List<Card> buildable = BuildRules.BuildableCards(players[1], players[1].Hand);
        bool canPlayCard = buildable.Contains<Card>(CardsDataset.GameCards[CardId.Temple]);
        Assert.IsFalse(canPlayCard);

        bool buildSuccess = MoveTestHelper.BuildCard(players[1],
            CardsDataset.GameCards[CardId.Temple],
            players[1].Hand,
            false);

        // Evaluate
        Assert.IsFalse(buildSuccess);
        // eval resources unchanged
        Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
        Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
        Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
    }

    [TestMethod]
    public void Multiplayer_GetPlayable_And_BuildCard_Test()
    {
        // Arrange
        List<WonderBoardProgress> allWonders = MockHelper.MockWonders();
        List<WonderBoardProgress> selectWonders = allWonders.GetRange(0, 3);
        List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

        // give resources for situation
        players[0].BuiltCards.Add(CardsDataset.GameCards[CardId.TreeFarm]);
        players[1].BuiltCards.Add(CardsDataset.GameCards[CardId.Mine]);
        players[1].Resources[Resource.Coins] = 6;
        players[2].BuiltCards.Add(CardsDataset.GameCards[CardId.ForestCave]);
        players[2].BuiltCards.Add(CardsDataset.GameCards[CardId.Forum]);

        // set hand card for situation
        Card player0Card = CardsDataset.GameCards[CardId.Baths];
        Card player1Card = CardsDataset.GameCards[CardId.Caravansery];
        Card player2Card = CardsDataset.GameCards[CardId.Dispensary];

        players[0].Hand.Add(player0Card);
        players[1].Hand.Add(player1Card);
        players[2].Hand.Add(player2Card);

        // Expected values
        int expPlayer0Coins = 3;
        int expPlayer1Coins = 6;
        int expPlayer2Coins = 3;

        // Test
        AssertCanPlay(players[0], player0Card);
        AssertCanPlay(players[1], player1Card);
        AssertCanPlay(players[2], player2Card);

        bool build0Success = BuildFromHand(players[0], player0Card);
        bool build1Success = BuildFromHand(players[1], player1Card);
        bool build2Success = BuildFromHand(players[2], player2Card);

        // Evaluate
        Assert.IsTrue(build0Success);
        Assert.IsTrue(build1Success);
        Assert.IsTrue(build2Success);
        // eval resources unchanged
        Assert.AreEqual(expPlayer0Coins, players[0].Resources[Resource.Coins]);
        Assert.AreEqual(expPlayer1Coins, players[1].Resources[Resource.Coins]);
        Assert.AreEqual(expPlayer2Coins, players[2].Resources[Resource.Coins]);
    }
    private static void AssertCanPlay(PlayerState player, Card card)
    {
        List<Card> buildable = BuildRules.BuildableCards(player, player.Hand);
        Assert.IsTrue(buildable.Contains<Card>(card));
    }

    // a paid build (not free) of a card from the player's own hand
    private static bool BuildFromHand(PlayerState player, Card card)
    {
        return MoveTestHelper.BuildCard(player, card, player.Hand, false);
    }
}
