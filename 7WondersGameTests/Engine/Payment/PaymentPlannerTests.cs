using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Payment.Dtos;
using _7WondersGame.Engine.Payment.Services;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGame.Engine.Wonders.Enums;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Engine.Payment;

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
    /// Three players, all resources 0. Returns (west, buyer, east); the buyer has the given coins, the others 0.
    /// </summary>
    private static (PlayerState West, PlayerState Buyer, PlayerState East) SetUp(int coins, Wonder? buyerWonder = null)
    {
        Wonder buyersBoard = new Wonder(WonderId.GizahA);
        if (buyerWonder != null)
        {
            buyersBoard = buyerWonder;
        }

        List<Wonder> wonders = new List<Wonder>
        {
            new Wonder(WonderId.GizahA),
            buyersBoard,
            new Wonder(WonderId.GizahA)
        };
        List<PlayerState> players = MockHelper.MockPlayers(wonders);
        foreach (PlayerState player in players)
        {
            player.Resources[Resource.Coins] = 0;
        }

        players[1].Resources[Resource.Coins] = coins;
        return (players[0], players[1], players[2]);
    }

    private static Dictionary<Resource, int> Cost(params (Resource, int)[] parts)
    {
        return parts.ToDictionary(part => part.Item1, part => part.Item2);
    }

    private static void Build(PlayerState player, params CardId[] cards)
    {
        foreach (CardId id in cards)
        {
            player.PlayedCards.Add(CardsDataset.GameCards[id]);
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
        var (_, buyer, _) = SetUp(coins: 0);
        buyer.Resources[Resource.Wood] = 2;
        Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 2))));
    }

    [TestMethod]
    public void BuysFromEachNeighbourTheResourceOnlyTheyHave()
    {
        var (west, buyer, east) = SetUp(coins: 4);
        Build(west, CardId.TreeFarm);
        Build(east, CardId.ClayPit);
        // west's Tree Farm is the only wood, so the clay has to come from east's Clay Pit
        Assert.AreEqual(
            new PaymentPlan(2, 2),
            PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 1), (Resource.Clay, 1))));
    }

    [TestMethod]
    public void NeighboursForumCantBeBoughtFrom()
    {
        var (west, buyer, east) = SetUp(coins: 10);
        Build(west, CardId.TreeFarm);
        Build(east, CardId.ClayPit, CardId.Forum);
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 1), (Resource.Clay, 1), (Resource.Glass, 1))));
    }

    [TestMethod]
    public void OwnCaravanseryAndForumAreFree()
    {
        var (_, buyer, _) = SetUp(coins: 0);
        Build(buyer, CardId.Caravansery, CardId.Forum);
        Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(buyer, Cost((Resource.Ore, 1), (Resource.Papyrus, 1))));
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost((Resource.Ore, 2))));
    }

    [TestMethod]
    public void OwnEitherOrCardsAreMatchedToTheCost()
    {
        // Tree Farm (wood/clay) + Forest Cave (wood/ore) pay wood + clay.
        // Taking wood from Tree Farm first would leave clay missing.
        var (_, buyer, _) = SetUp(coins: 0);
        Build(buyer, CardId.TreeFarm, CardId.ForestCave);
        Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 1), (Resource.Clay, 1))));
    }

    [TestMethod]
    public void NeighbourEitherOrCardsAreMatchedToTheCost()
    {
        var (west, buyer, _) = SetUp(coins: 4);
        Build(west, CardId.TreeFarm, CardId.ForestCave);
        Assert.AreEqual(
            new PaymentPlan(4, 0),
            PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 1), (Resource.Clay, 1))));
    }

    [TestMethod]
    public void NeighbourEitherOrCardGivesOnlyOneResource()
    {
        var (west, buyer, _) = SetUp(coins: 10);
        Build(west, CardId.TreeFarm);
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 1), (Resource.Clay, 1))));
    }

    [TestMethod]
    public void PurchaseIsSplitBetweenNeighbours()
    {
        var (west, buyer, east) = SetUp(coins: 4);
        west.Resources[Resource.Wood] = 1;
        east.Resources[Resource.Wood] = 1;
        Assert.AreEqual(new PaymentPlan(2, 2), PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 2))));

        buyer.Resources[Resource.Coins] = 3;
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 2))), "4 coins needed");
    }

    [TestMethod]
    public void CheapUnitsFirstThenTheRestFromTheOtherNeighbour()
    {
        // East Trading Post: 1 coin per raw from the east. East has one wood, west has plenty.
        var (west, buyer, east) = SetUp(coins: 3);
        buyer.Abilities |= Ability.RawCheapEast;
        west.Resources[Resource.Wood] = 3;
        east.Resources[Resource.Wood] = 1;
        Assert.AreEqual(new PaymentPlan(2, 1), PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 2))));
    }

    [TestMethod]
    public void EquallyCheap_BuysFromWest()
    {
        var (west, buyer, east) = SetUp(coins: 2);
        west.Resources[Resource.Stone] = 1;
        east.Resources[Resource.Stone] = 1;
        Assert.AreEqual(new PaymentPlan(2, 0), PaymentPlanner.Plan(buyer, Cost((Resource.Stone, 1))));
    }

    [TestMethod]
    public void CheaperNeighbourWins()
    {
        var (west, buyer, east) = SetUp(coins: 2);
        buyer.Abilities |= Ability.RawCheapEast;
        west.Resources[Resource.Stone] = 1;
        east.Resources[Resource.Stone] = 1;
        Assert.AreEqual(new PaymentPlan(0, 1), PaymentPlanner.Plan(buyer, Cost((Resource.Stone, 1))));
    }

    [TestMethod]
    public void OlympiaB_RawFromBothNeighboursCostsOne()
    {
        var (west, buyer, east) = SetUp(coins: 2);
        buyer.Abilities |= Ability.WonderRawCheap;
        west.Resources[Resource.Ore] = 1;
        east.Resources[Resource.Ore] = 1;
        Assert.AreEqual(new PaymentPlan(1, 1), PaymentPlanner.Plan(buyer, Cost((Resource.Ore, 2))));
    }

    [TestMethod]
    public void Marketplace_ManufacturedCostsOne_RawStillTwo()
    {
        var (west, buyer, _) = SetUp(coins: 3);
        buyer.Abilities |= Ability.ManufCheap;
        west.Resources[Resource.Glass] = 1;
        west.Resources[Resource.Wood] = 1;
        Assert.AreEqual(
            new PaymentPlan(3, 0),
            PaymentPlanner.Plan(buyer, Cost((Resource.Glass, 1), (Resource.Wood, 1))));
    }

    [TestMethod]
    public void CoinCostCountsAgainstTheBudget()
    {
        var (west, buyer, _) = SetUp(coins: 2);
        west.Resources[Resource.Wood] = 1;
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 1), (Resource.Coins, 1))));
        buyer.Resources[Resource.Coins] = 3;
        Assert.AreEqual(
            new PaymentPlan(2, 0),
            PaymentPlanner.Plan(buyer, Cost((Resource.Wood, 1), (Resource.Coins, 1))));
        buyer.Resources[Resource.Coins] = 0;
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost((Resource.Coins, 1))));
    }

    [TestMethod]
    public void AlexandriaWildcards()
    {
        var (_, buyer, _) = SetUp(coins: 0, new Wonder(WonderId.AlexandriaB));
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost((Resource.Stone, 1))));
        buyer.Board.Stage = 1;   // any raw
        Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(buyer, Cost((Resource.Stone, 1))));
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost((Resource.Stone, 1), (Resource.Clay, 1))));
        Assert.IsNull(PaymentPlanner.Plan(buyer, Cost((Resource.Glass, 1))));
        buyer.Board.Stage = 2;   // and any manufactured good
        Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(buyer, Cost((Resource.Stone, 1), (Resource.Glass, 1))));
    }

    [TestMethod]
    public void PlanningChangesNothing()
    {
        var (west, buyer, east) = SetUp(coins: 6);
        Build(west, CardId.TreeFarm, CardId.Mine);
        Build(east, CardId.ClayPit);
        Build(buyer, CardId.ForestCave);
        east.Resources[Resource.Stone] = 1;
        PlayerState[] players = { west, buyer, east };
        var resourcesBefore = new List<Dictionary<Resource, int>>();
        var cardsBefore = new List<List<Card>>();
        foreach (PlayerState player in players)
        {
            resourcesBefore.Add(ResourcesOf(player));
            cardsBefore.Add(new List<Card>(player.PlayedCards));
        }

        var cost = Cost((Resource.Wood, 1), (Resource.Clay, 1), (Resource.Stone, 1), (Resource.Ore, 1));
        Assert.IsNotNull(PaymentPlanner.Plan(buyer, cost));

        for (int seat = 0; seat < 3; seat++)
        {
            CollectionAssert.AreEquivalent(resourcesBefore[seat], ResourcesOf(players[seat]),
                                           $"resources of player {seat}");
            CollectionAssert.AreEqual(cardsBefore[seat], players[seat].PlayedCards, $"cards of player {seat}");
        }
    }

    [TestMethod]
    public void BuildCard_PaysWhatThePlanSays()
    {
        var (west, buyer, east) = SetUp(coins: 4);
        Build(west, CardId.TreeFarm);
        Build(east, CardId.TimberYard);
        Card caravansery = CardsDataset.GameCards[CardId.Caravansery];   // 2 wood
        buyer.HandCards.Add(caravansery);

        Assert.IsTrue(MoveTestHelper.BuildCard(buyer, caravansery, buyer.HandCards, false));
        Assert.AreEqual(0, buyer.Resources[Resource.Coins]);
        Assert.AreEqual(2, west.Resources[Resource.Coins]);
        Assert.AreEqual(2, east.Resources[Resource.Coins]);
    }

    // ===== cross-check against a brute-force search =====

    [TestMethod]
    public void MatchesBruteForce_OnRandomPositions()
    {
        var random = new Random(20261110);
        WonderId[] boards = { WonderId.GizahA, WonderId.AlexandriaA, WonderId.AlexandriaB };

        for (int test = 0; test < 3000; test++)
        {
            Wonder buyerBoard = new Wonder(boards[random.Next(boards.Length)]);
            buyerBoard.Stage = random.Next(0, 3);
            var (west, buyer, east) = SetUp(coins: random.Next(0, 9), buyerBoard);
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
            buyer.Abilities |= Ability.RawCheapWest;
        }

        if (random.Next(3) == 0)
        {
            buyer.Abilities |= Ability.RawCheapEast;
        }

        if (random.Next(5) == 0)
        {
            buyer.Abilities |= Ability.WonderRawCheap;
        }

        if (random.Next(3) == 0)
        {
            buyer.Abilities |= Ability.ManufCheap;
        }
    }

    // one to three kinds of goods, one to three of each, sometimes a coin
    private static Dictionary<Resource, int> RandomCost(Random random)
    {
        var cost = new Dictionary<Resource, int>();
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
