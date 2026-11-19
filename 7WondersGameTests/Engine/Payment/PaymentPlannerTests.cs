using _7Wonders.Engine.Cards.Data;
using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Payment.Dtos;
using _7Wonders.Engine.Payment.Services;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;
using _7Wonders.Engine.Wonders.Entities;
using _7Wonders.Engine.Wonders.Enums;
using _7Wonders.Tests.TestHelpers;

namespace _7Wonders.Tests.Engine.Payment;

/// <summary>
/// The payment planner: the cheapest way to pay, with either/or cards used the best way and purchases split
/// between the neighbours, and that planning changes nothing.
/// </summary>
[TestClass]
public class PaymentPlannerTests
{
    private static readonly Resource[] GoodsList =
    {
        Resource.Wood, Resource.Ore, Resource.Clay, Resource.Stone, Resource.Loom, Resource.Glass, Resource.Papyrus
    };

    private static readonly CardId[] BrownEitherOrCards =
    {
        CardId.TreeFarm, CardId.ForestCave, CardId.TimberYard, CardId.Excavation, CardId.ClayPit, CardId.Mine
    };

    /// <summary>
    /// Three players, all resources 0, in seat order: west, buyer, east. The buyer has the given coins, the others 0.
    /// </summary>
    private static List<PlayerState> SetUp(
        int coins, WonderBoardProgress? buyerWonder = null)
    {
        WonderBoardProgress buyersWonder = new(WonderId.GizahA);
        if (buyerWonder != null)
        {
            buyersWonder = buyerWonder;
        }

        List<WonderBoardProgress> wonders = new()
        {
            new WonderBoardProgress(WonderId.GizahA),
            buyersWonder,
            new WonderBoardProgress(WonderId.GizahA)
        };
        List<PlayerState> players = MockHelper.MockPlayers(wonders);
        foreach (PlayerState player in players)
        {
            player.Resources[Resource.Coins] = 0;
        }

        players[1].Resources[Resource.Coins] = coins;
        return players;
    }

    /// <summary>A cost of the given resources, one per unit (Wood, Wood is two wood).</summary>
    private static Dictionary<Resource, int> Cost(params Resource[] units)
    {
        Dictionary<Resource, int> cost = new();
        foreach (Resource unit in units)
        {
            cost[unit] = cost.GetValueOrDefault(unit) + 1;
        }
        return cost;
    }

    private static void Build(PlayerState player, params CardId[] cards)
    {
        foreach (CardId id in cards)
        {
            player.BuiltCards.Add(CardsDataset.GameCards[id]);
        }
    }

    private static Dictionary<Resource, int> ResourcesOf(PlayerState player)
    {
        return player.Resources.ToDictionary(
            resourceAmount => resourceAmount.Key,
            resourceAmount => resourceAmount.Value);
    }

    [TestMethod]
    public void OwnProductionCovers_IsFree()
    {
        List<PlayerState> table = SetUp(coins: 0);
        PlayerState buyer = table[1];
        buyer.Resources[Resource.Wood] = 2;
        Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Wood)));
    }

    [TestMethod]
    public void BuysFromEachNeighbourTheResourceOnlyTheyHave()
    {
        List<PlayerState> table = SetUp(coins: 4);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        PlayerState east = table[2];
        Build(west, CardId.TreeFarm);
        Build(east, CardId.ClayPit);
        // west's Tree Farm is the only wood, so the clay has to come from east's Clay Pit
        Assert.AreEqual(
            new PaymentPlan(2, 2),
            PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Clay)));
    }

    [TestMethod]
    public void NeighboursForumCantBeBoughtFrom()
    {
        List<PlayerState> table = SetUp(coins: 10);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        PlayerState east = table[2];
        Build(west, CardId.TreeFarm);
        Build(east, CardId.ClayPit, CardId.Forum);
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Clay, Resource.Glass)));
    }

    [TestMethod]
    public void OwnCaravanseryAndForumAreFree()
    {
        List<PlayerState> table = SetUp(coins: 0);
        PlayerState buyer = table[1];
        Build(buyer, CardId.Caravansery, CardId.Forum);
        Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(buyer, Cost(Resource.Ore, Resource.Papyrus)));
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost(Resource.Ore, Resource.Ore)));
    }

    [TestMethod]
    public void OwnEitherOrCardsAreMatchedToTheCost()
    {
        // Tree Farm (wood/clay) + Forest Cave (wood/ore) pay wood + clay.
        // Taking wood from Tree Farm first would leave clay missing.
        List<PlayerState> table = SetUp(coins: 0);
        PlayerState buyer = table[1];
        Build(buyer, CardId.TreeFarm, CardId.ForestCave);
        Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Clay)));
    }

    [TestMethod]
    public void NeighbourEitherOrCardsAreMatchedToTheCost()
    {
        List<PlayerState> table = SetUp(coins: 4);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        Build(west, CardId.TreeFarm, CardId.ForestCave);
        Assert.AreEqual(
            new PaymentPlan(4, 0),
            PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Clay)));
    }

    [TestMethod]
    public void NeighbourEitherOrCardGivesOnlyOneResource()
    {
        List<PlayerState> table = SetUp(coins: 10);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        Build(west, CardId.TreeFarm);
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Clay)));
    }

    [TestMethod]
    public void PurchaseIsSplitBetweenNeighbours()
    {
        List<PlayerState> table = SetUp(coins: 4);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        PlayerState east = table[2];
        west.Resources[Resource.Wood] = 1;
        east.Resources[Resource.Wood] = 1;
        Assert.AreEqual(new PaymentPlan(2, 2), PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Wood)));

        buyer.Resources[Resource.Coins] = 3;
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Wood)), "4 coins needed");
    }

    [TestMethod]
    public void CheapUnitsFirstThenTheRestFromTheOtherNeighbour()
    {
        // East Trading Post: 1 coin per raw from the east. East has one wood, west has plenty.
        List<PlayerState> table = SetUp(coins: 3);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        PlayerState east = table[2];
        buyer.Abilities |= Ability.RawMaterialsCheapFromEastNeighbour;
        west.Resources[Resource.Wood] = 3;
        east.Resources[Resource.Wood] = 1;
        Assert.AreEqual(new PaymentPlan(2, 1), PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Wood)));
    }

    [TestMethod]
    public void EquallyCheap_BuysFromWest()
    {
        List<PlayerState> table = SetUp(coins: 2);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        PlayerState east = table[2];
        west.Resources[Resource.Stone] = 1;
        east.Resources[Resource.Stone] = 1;
        Assert.AreEqual(new PaymentPlan(2, 0), PaymentPlanner.Plan(buyer, Cost(Resource.Stone)));
    }

    [TestMethod]
    public void CheaperNeighbourWins()
    {
        List<PlayerState> table = SetUp(coins: 2);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        PlayerState east = table[2];
        buyer.Abilities |= Ability.RawMaterialsCheapFromEastNeighbour;
        west.Resources[Resource.Stone] = 1;
        east.Resources[Resource.Stone] = 1;
        Assert.AreEqual(new PaymentPlan(0, 1), PaymentPlanner.Plan(buyer, Cost(Resource.Stone)));
    }

    [TestMethod]
    public void OlympiaB_RawFromBothNeighboursCostsOne()
    {
        List<PlayerState> table = SetUp(coins: 2);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        PlayerState east = table[2];
        buyer.Abilities |= Ability.RawMaterialsCheapFromBothNeighbours;
        west.Resources[Resource.Ore] = 1;
        east.Resources[Resource.Ore] = 1;
        Assert.AreEqual(new PaymentPlan(1, 1), PaymentPlanner.Plan(buyer, Cost(Resource.Ore, Resource.Ore)));
    }

    [TestMethod]
    public void Marketplace_ManufacturedCostsOne_RawStillTwo()
    {
        List<PlayerState> table = SetUp(coins: 3);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        buyer.Abilities |= Ability.ManufacturedGoodsCheapFromBothNeighbours;
        west.Resources[Resource.Glass] = 1;
        west.Resources[Resource.Wood] = 1;
        Assert.AreEqual(
            new PaymentPlan(3, 0),
            PaymentPlanner.Plan(buyer, Cost(Resource.Glass, Resource.Wood)));
    }

    [TestMethod]
    public void CoinCostCountsAgainstTheBudget()
    {
        List<PlayerState> table = SetUp(coins: 2);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        west.Resources[Resource.Wood] = 1;
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Coins)));
        buyer.Resources[Resource.Coins] = 3;
        Assert.AreEqual(
            new PaymentPlan(2, 0),
            PaymentPlanner.Plan(buyer, Cost(Resource.Wood, Resource.Coins)));
        buyer.Resources[Resource.Coins] = 0;
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost(Resource.Coins)));
    }

    [TestMethod]
    public void AlexandriaWildcards()
    {
        List<PlayerState> table = SetUp(coins: 0, new WonderBoardProgress(WonderId.AlexandriaB));
        PlayerState buyer = table[1];
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost(Resource.Stone)));
        buyer.Wonder.BuiltStageCount = 1;   // any raw
        Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(buyer, Cost(Resource.Stone)));
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost(Resource.Stone, Resource.Clay)));
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost(Resource.Glass)));
        buyer.Wonder.BuiltStageCount = 2;   // and any manufactured good
        Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(buyer, Cost(Resource.Stone, Resource.Glass)));
    }

    [TestMethod]
    public void PlanningChangesNothing()
    {
        List<PlayerState> table = SetUp(coins: 6);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        PlayerState east = table[2];
        Build(west, CardId.TreeFarm, CardId.Mine);
        Build(east, CardId.ClayPit);
        Build(buyer, CardId.ForestCave);
        east.Resources[Resource.Stone] = 1;
        PlayerState[] players = { west, buyer, east };
        List<Dictionary<Resource, int>> resourcesBefore = new();
        List<List<Card>> cardsBefore = new();
        foreach (PlayerState player in players)
        {
            resourcesBefore.Add(ResourcesOf(player));
            cardsBefore.Add(new List<Card>(player.BuiltCards));
        }

        Dictionary<Resource, int> cost =
            Cost(Resource.Wood, Resource.Clay, Resource.Stone, Resource.Ore);
        Assert.IsNotNull(PaymentPlanner.Plan(buyer, cost));

        for (int seat = 0; seat < 3; seat++)
        {
            CollectionAssert.AreEquivalent(resourcesBefore[seat], ResourcesOf(players[seat]),
                                           $"resources of player {seat}");
            CollectionAssert.AreEqual(cardsBefore[seat], players[seat].BuiltCards, $"cards of player {seat}");
        }
    }

    [TestMethod]
    public void BuildCard_PaysWhatThePlanSays()
    {
        List<PlayerState> table = SetUp(coins: 4);
        PlayerState west = table[0];
        PlayerState buyer = table[1];
        PlayerState east = table[2];
        Build(west, CardId.TreeFarm);
        Build(east, CardId.TimberYard);
        Card caravansery = CardsDataset.GameCards[CardId.Caravansery];   // 2 wood
        buyer.Hand.Add(caravansery);

        Assert.IsTrue(MoveTestHelper.BuildCard(buyer, caravansery, buyer.Hand, false));
        Assert.AreEqual(0, buyer.Resources[Resource.Coins]);
        Assert.AreEqual(2, west.Resources[Resource.Coins]);
        Assert.AreEqual(2, east.Resources[Resource.Coins]);
    }

    // ===== cross-check against a brute-force search =====

    [TestMethod]
    public void MatchesBruteForce_OnRandomPositions()
    {
        Random random = new(20261110);
        WonderId[] boards = { WonderId.GizahA, WonderId.AlexandriaA, WonderId.AlexandriaB };

        for (int test = 0; test < 3000; test++)
        {
            WonderBoardProgress buyersWonder = new(boards[random.Next(boards.Length)]);
            buyersWonder.BuiltStageCount = random.Next(0, 3);
            List<PlayerState> table = SetUp(coins: random.Next(0, 9), buyersWonder);
            PlayerState west = table[0];
            PlayerState buyer = table[1];
            PlayerState east = table[2];
            foreach (PlayerState player in new[] { west, buyer, east })
            {
                GiveRandomResourcesAndCards(player, random);
            }

            GiveRandomDiscounts(buyer, random);
            Dictionary<Resource, int> cost = RandomCost(random);

            PaymentPlan? expected = BruteForcePayment.Plan(buyer, cost);
            PaymentPlan? actual = PaymentPlanner.Plan(buyer, cost);
            string costText = string.Join(",", cost.Select(resourceCost => $"{resourceCost.Value} {resourceCost.Key}"));
            Assert.AreEqual(expected, actual, $"case {test}: cost {costText}, coins {buyer.Resources[Resource.Coins]}");
        }
    }

    // a few goods of each kind, some brown either/or cards, sometimes a Caravansery or Forum
    private static void GiveRandomResourcesAndCards(PlayerState player, Random random)
    {
        foreach (Resource resource in GoodsList)
        {
            player.Resources[resource] = 0;
            if (random.Next(4) == 0)
            {
                player.Resources[resource] = random.Next(1, 3);
            }
        }

        foreach (CardId id in BrownEitherOrCards)
        {
            if (random.Next(3) == 0)
            {
                Build(player, id);
            }
        }

        if (random.Next(4) == 0)
        {
            Build(player, CardId.Caravansery);
        }

        if (random.Next(4) == 0)
        {
            Build(player, CardId.Forum);
        }
    }

    private static void GiveRandomDiscounts(PlayerState buyer, Random random)
    {
        if (random.Next(3) == 0)
        {
            buyer.Abilities |= Ability.RawMaterialsCheapFromWestNeighbour;
        }

        if (random.Next(3) == 0)
        {
            buyer.Abilities |= Ability.RawMaterialsCheapFromEastNeighbour;
        }

        if (random.Next(5) == 0)
        {
            buyer.Abilities |= Ability.RawMaterialsCheapFromBothNeighbours;
        }

        if (random.Next(3) == 0)
        {
            buyer.Abilities |= Ability.ManufacturedGoodsCheapFromBothNeighbours;
        }
    }

    // one to three kinds of goods, one to three of each, sometimes a coin
    private static Dictionary<Resource, int> RandomCost(Random random)
    {
        Dictionary<Resource, int> cost = new();
        int kinds = random.Next(1, 4);
        for (int kindIndex = 0; kindIndex < kinds; kindIndex++)
        {
            Resource good = GoodsList[random.Next(GoodsList.Length)];
            cost[good] = random.Next(1, 4);
        }

        if (random.Next(5) == 0)
        {
            cost[Resource.Coins] = 1;
        }

        return cost;
    }
}
