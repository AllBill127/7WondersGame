using _7Wonders.Engine.Cards.Data;
using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;
using _7Wonders.Engine.Wonders.Entities;
using _7Wonders.Tests.TestHelpers;

namespace _7Wonders.Tests.Engine.Rules;

/// <summary>Which hand cards a player can build: paid, chained, traded for, or not at all.</summary>
[TestClass]
public class BuildableCardsTests
{

    [TestMethod]
    public void BuildableCards_Default_WhenSingleCardIsPlayable_ReturnsListWithCard()
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
        int expThisPlayerCoins = 4;
        int expEastPlayerCoins = 3;
        int expWestPlayerCoins = 3;

        // Test
        List<Card> buildableCards = BuildRules.BuildableCards(players[1], players[1].Hand);
        List<Card> buildable = buildableCards;

        bool canPlayCard = buildable.Contains<Card>(CardsDataset.GameCards[CardId.Caravansery]);

        // Evaluate
        Assert.IsTrue(canPlayCard);
        // eval resources unchanged
        Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
        Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
        Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
    }

    [TestMethod]
    public void BuildableCards_Default_WhenSingleCardIsNotPlayable_ReturnsListWithoutCard()
    {
        // Arrange
        List<WonderBoardProgress> allWonders = MockHelper.MockWonders();
        List<WonderBoardProgress> selectWonders = allWonders.GetRange(0, 3);
        List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

        // give resources for situation
        players[0].BuiltCards.Add(CardsDataset.GameCards[CardId.TreeFarm]);
        players[1].Resources[Resource.Coins] = 4;

        // set hand card list
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Caravansery]);

        // Expected values
        int expThisPlayerCoins = 4;
        int expEastPlayerCoins = 3;
        int expWestPlayerCoins = 3;

        // Test
        List<Card> buildableCards = BuildRules.BuildableCards(players[1], players[1].Hand);
        List<Card> buildable = buildableCards;

        bool canPlayCard = buildable.Contains<Card>(CardsDataset.GameCards[CardId.Caravansery]);

        // Evaluate
        Assert.IsFalse(canPlayCard);
        // eval resources unchanged
        Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
        Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
        Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
    }

    [TestMethod]
    public void BuildableCards_Default_WhenSingleCardIsNotPlayable_CantSellComercialResource_ReturnsListWithoutCard()
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
        List<Card> buildableCards = BuildRules.BuildableCards(players[1], players[1].Hand);
        List<Card> buildable = buildableCards;

        bool canPlayCard = buildable.Contains<Card>(CardsDataset.GameCards[CardId.Caravansery]);

        // Evaluate
        Assert.IsFalse(canPlayCard);
        // eval resources unchanged
        Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
        Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
        Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
    }

    [TestMethod]
    public void BuildableCards_Default_WhenMultipleCardsArePlayable_ReturnsListWithCards()
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
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Baths]);
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Sawmill]);
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Press]);
        players[1].Hand.Add(CardsDataset.GameCards[CardId.School]);  // not playable

        // Expected values
        int expThisPlayerCoins = 4;
        int expEastPlayerCoins = 3;
        int expWestPlayerCoins = 3;

        // Test
        List<Card> buildable = BuildRules.BuildableCards(players[1], players[1].Hand);

        // Evaluate
        bool canPlayCard;
        for (int cardIndex = 0; cardIndex < players[1].Hand.Count - 1; cardIndex++)
        {
            canPlayCard = buildable.Contains<Card>(players[1].Hand[cardIndex]);
            Assert.IsTrue(canPlayCard);
        }
        // eval School card is unplayable
        canPlayCard = buildable.Contains<Card>(CardsDataset.GameCards[CardId.School]);
        Assert.IsFalse(canPlayCard);
        // eval resources unchanged
        Assert.AreEqual(expEastPlayerCoins, players[0].Resources[Resource.Coins]);
        Assert.AreEqual(expThisPlayerCoins, players[1].Resources[Resource.Coins]);
        Assert.AreEqual(expWestPlayerCoins, players[2].Resources[Resource.Coins]);
    }

    [TestMethod]
    public void BuildableCards_AllCardsFree_WhenNoDuplicateRuleViolation_ReturnsMatchingList()
    {
        // Arrange
        List<WonderBoardProgress> allWonders = MockHelper.MockWonders();
        List<WonderBoardProgress> selectWonders = allWonders.GetRange(0, 3);
        List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

        // set hand card list
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Caravansery]);
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Baths]);
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Sawmill]); // could build with coins
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Press]);   // could build - no price
        players[1].Hand.Add(CardsDataset.GameCards[CardId.School]);
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Palace]);

        // Expected values
        List<Card> expectedBuildableCards = players[1].Hand;

        // Test
        List<Card> buildable = BuildRules.BuildableCards(players[1], players[1].Hand, true);

        // Evaluate
        bool isMatch = buildable.SequenceEqual(expectedBuildableCards);
        Assert.IsTrue(isMatch);
    }

    [TestMethod]
    public void BuildableCards_AllCardsFree_WhenDuplicateRuleViolation_ReturnsCardsListWithoutViolation()
    {
        // Arrange
        List<WonderBoardProgress> allWonders = MockHelper.MockWonders();
        List<WonderBoardProgress> selectWonders = allWonders.GetRange(0, 3);
        List<PlayerState> players = MockHelper.MockPlayers(selectWonders);

        // set played card list
        players[1].BuiltCards.Add(CardsDataset.GameCards[CardId.Caravansery]);
        players[1].BuiltCards.Add(CardsDataset.GameCards[CardId.Press2]);

        // set hand card list
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Caravansery]);
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Baths]);
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Sawmill]);
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Press]);   // should be violation with Press2
        players[1].Hand.Add(CardsDataset.GameCards[CardId.School]);
        players[1].Hand.Add(CardsDataset.GameCards[CardId.Palace]);

        // Expected values
        List<Card> expectedBuildableCards = new()
        {
            CardsDataset.GameCards[CardId.Baths],
            CardsDataset.GameCards[CardId.Sawmill],
            CardsDataset.GameCards[CardId.School],
            CardsDataset.GameCards[CardId.Palace],
        };

        // Test
        List<Card> buildable = BuildRules.BuildableCards(players[1], players[1].Hand, true);

        // Evaluate
        bool isMatch = buildable.SequenceEqual(expectedBuildableCards);
        Assert.IsTrue(isMatch);
    }
}
