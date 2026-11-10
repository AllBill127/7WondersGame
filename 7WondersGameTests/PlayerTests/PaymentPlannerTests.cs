using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests.PlayerTests
{
    /// <summary>
    /// The payment planner (E2): the cheapest way to pay, R7 (either/or cards used the best way) and
    /// R8 (purchases split between neighbours), and that planning changes nothing.
    /// </summary>
    [TestClass]
    public class PaymentPlannerTests
    {
        private static readonly Resource[] GoodsList =
            { Resource.Wood, Resource.Ore, Resource.Clay, Resource.Stone, Resource.Loom, Resource.Glass, Resource.Papyrus };

        /// <summary>Three players, all resources 0. Returns (west, buyer, east); the buyer has the given coins, the others 0.</summary>
        private static (Player West, Player Buyer, Player East) SetUp(int coins, Wonder? buyerWonder = null)
        {
            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(
                new List<Wonder> { new GizahA(), buyerWonder ?? new GizahA(), new GizahA() });
            foreach (Player p in players)
                p.Resources[Resource.Coins] = 0;
            players[1].Resources[Resource.Coins] = coins;
            return (players[0], players[1], players[2]);
        }

        private static Dictionary<Resource, int> Cost(params (Resource, int)[] parts) =>
            parts.ToDictionary(p => p.Item1, p => p.Item2);

        private static void Build(Player p, params CardId[] cards)
        {
            foreach (CardId id in cards)
                p.PlayedCards.Add(CardsDataset.GameCards[id]);
        }

        [TestMethod]
        public void OwnProductionCovers_IsFree()
        {
            var (_, buyer, _) = SetUp(coins: 0);
            buyer.Resources[Resource.Wood] = 2;
            Assert.AreEqual(PaymentPlan.Free, buyer.PlanPayment(Cost((Resource.Wood, 2))));
        }

        [TestMethod]
        public void BuysFromEachNeighbourTheResourceOnlyTheyHave()
        {
            var (west, buyer, east) = SetUp(coins: 4);
            Build(west, CardId.TreeFarm);
            Build(east, CardId.ClayPit);
            // west's Tree Farm is the only wood, so the clay has to come from east's Clay Pit
            Assert.AreEqual(new PaymentPlan(2, 2), buyer.PlanPayment(Cost((Resource.Wood, 1), (Resource.Clay, 1))));
        }

        [TestMethod]
        public void NeighboursForumCantBeBoughtFrom()
        {
            var (west, buyer, east) = SetUp(coins: 10);
            Build(west, CardId.TreeFarm);
            Build(east, CardId.ClayPit, CardId.Forum);
            Assert.IsNull(buyer.PlanPayment(Cost((Resource.Wood, 1), (Resource.Clay, 1), (Resource.Glass, 1))));
        }

        [TestMethod]
        public void OwnCaravanseryAndForumAreFree()
        {
            var (_, buyer, _) = SetUp(coins: 0);
            Build(buyer, CardId.Caravansery, CardId.Forum);
            Assert.AreEqual(PaymentPlan.Free, buyer.PlanPayment(Cost((Resource.Ore, 1), (Resource.Papyrus, 1))));
            Assert.IsNull(buyer.PlanPayment(Cost((Resource.Ore, 2))));
        }

        [TestMethod]
        public void R7_OwnEitherOrCardsAreMatchedToTheCost()
        {
            // The critique's example: Tree Farm (wood/clay) + Forest Cave (wood/ore) pay wood + clay.
            // Taking wood from Tree Farm first would leave clay missing.
            var (_, buyer, _) = SetUp(coins: 0);
            Build(buyer, CardId.TreeFarm, CardId.ForestCave);
            Assert.AreEqual(PaymentPlan.Free, buyer.PlanPayment(Cost((Resource.Wood, 1), (Resource.Clay, 1))));
        }

        [TestMethod]
        public void R7_NeighbourEitherOrCardsAreMatchedToTheCost()
        {
            var (west, buyer, _) = SetUp(coins: 4);
            Build(west, CardId.TreeFarm, CardId.ForestCave);
            Assert.AreEqual(new PaymentPlan(4, 0), buyer.PlanPayment(Cost((Resource.Wood, 1), (Resource.Clay, 1))));
        }

        [TestMethod]
        public void NeighbourEitherOrCardGivesOnlyOneResource()
        {
            var (west, buyer, _) = SetUp(coins: 10);
            Build(west, CardId.TreeFarm);
            Assert.IsNull(buyer.PlanPayment(Cost((Resource.Wood, 1), (Resource.Clay, 1))));
        }

        [TestMethod]
        public void R8_PurchaseIsSplitBetweenNeighbours()
        {
            var (west, buyer, east) = SetUp(coins: 4);
            west.Resources[Resource.Wood] = 1;
            east.Resources[Resource.Wood] = 1;
            Assert.AreEqual(new PaymentPlan(2, 2), buyer.PlanPayment(Cost((Resource.Wood, 2))));

            buyer.Resources[Resource.Coins] = 3;
            Assert.IsNull(buyer.PlanPayment(Cost((Resource.Wood, 2))), "4 coins needed");
        }

        [TestMethod]
        public void R8_CheapUnitsFirstThenTheRestFromTheOtherNeighbour()
        {
            // East Trading Post: 1 coin per raw from the east. East has one wood, west has plenty.
            var (west, buyer, east) = SetUp(coins: 3);
            buyer.RawCheapEast = true;
            west.Resources[Resource.Wood] = 3;
            east.Resources[Resource.Wood] = 1;
            Assert.AreEqual(new PaymentPlan(2, 1), buyer.PlanPayment(Cost((Resource.Wood, 2))));
        }

        [TestMethod]
        public void EquallyCheap_BuysFromWest()
        {
            var (west, buyer, east) = SetUp(coins: 2);
            west.Resources[Resource.Stone] = 1;
            east.Resources[Resource.Stone] = 1;
            Assert.AreEqual(new PaymentPlan(2, 0), buyer.PlanPayment(Cost((Resource.Stone, 1))));
        }

        [TestMethod]
        public void CheaperNeighbourWins()
        {
            var (west, buyer, east) = SetUp(coins: 2);
            buyer.RawCheapEast = true;
            west.Resources[Resource.Stone] = 1;
            east.Resources[Resource.Stone] = 1;
            Assert.AreEqual(new PaymentPlan(0, 1), buyer.PlanPayment(Cost((Resource.Stone, 1))));
        }

        [TestMethod]
        public void OlympiaB_RawFromBothNeighboursCostsOne()
        {
            var (west, buyer, east) = SetUp(coins: 2);
            buyer.WonderRawCheap = true;
            west.Resources[Resource.Ore] = 1;
            east.Resources[Resource.Ore] = 1;
            Assert.AreEqual(new PaymentPlan(1, 1), buyer.PlanPayment(Cost((Resource.Ore, 2))));
        }

        [TestMethod]
        public void Marketplace_ManufacturedCostsOne_RawStillTwo()
        {
            var (west, buyer, _) = SetUp(coins: 3);
            buyer.ManufCheap = true;
            west.Resources[Resource.Glass] = 1;
            west.Resources[Resource.Wood] = 1;
            Assert.AreEqual(new PaymentPlan(3, 0), buyer.PlanPayment(Cost((Resource.Glass, 1), (Resource.Wood, 1))));
        }

        [TestMethod]
        public void CoinCostCountsAgainstTheBudget()
        {
            var (west, buyer, _) = SetUp(coins: 2);
            west.Resources[Resource.Wood] = 1;
            Assert.IsNull(buyer.PlanPayment(Cost((Resource.Wood, 1), (Resource.Coins, 1))));
            buyer.Resources[Resource.Coins] = 3;
            Assert.AreEqual(new PaymentPlan(2, 0), buyer.PlanPayment(Cost((Resource.Wood, 1), (Resource.Coins, 1))));
            buyer.Resources[Resource.Coins] = 0;
            Assert.IsNull(buyer.PlanPayment(Cost((Resource.Coins, 1))));
        }

        [TestMethod]
        public void AlexandriaWildcards()
        {
            var (_, buyer, _) = SetUp(coins: 0, new AlexandriaB());
            Assert.IsNull(buyer.PlanPayment(Cost((Resource.Stone, 1))));
            buyer.Board.Stage = 1;   // any raw
            Assert.AreEqual(PaymentPlan.Free, buyer.PlanPayment(Cost((Resource.Stone, 1))));
            Assert.IsNull(buyer.PlanPayment(Cost((Resource.Stone, 1), (Resource.Clay, 1))));
            Assert.IsNull(buyer.PlanPayment(Cost((Resource.Glass, 1))));
            buyer.Board.Stage = 2;   // and any manufactured good
            Assert.AreEqual(PaymentPlan.Free, buyer.PlanPayment(Cost((Resource.Stone, 1), (Resource.Glass, 1))));
        }

        [TestMethod]
        public void PlanningChangesNothing()
        {
            var (west, buyer, east) = SetUp(coins: 6);
            Build(west, CardId.TreeFarm, CardId.Mine);
            Build(east, CardId.ClayPit);
            Build(buyer, CardId.ForestCave);
            east.Resources[Resource.Stone] = 1;
            var before = new[] { west, buyer, east }.Select(p => (p.Resources.ToDictionary(kv => kv.Key, kv => kv.Value), p.PlayedCards.ToList())).ToList();

            Assert.IsNotNull(buyer.PlanPayment(Cost((Resource.Wood, 1), (Resource.Clay, 1), (Resource.Stone, 1), (Resource.Ore, 1))));

            var after = new[] { west, buyer, east };
            for (int i = 0; i < 3; i++)
            {
                CollectionAssert.AreEquivalent(before[i].Item1, after[i].Resources, $"resources of player {i}");
                CollectionAssert.AreEqual(before[i].Item2, after[i].PlayedCards, $"cards of player {i}");
            }
        }

        [TestMethod]
        public void BuildStructure_PaysWhatThePlanSays()
        {
            var (west, buyer, east) = SetUp(coins: 4);
            Build(west, CardId.TreeFarm);
            Build(east, CardId.TimberYard);
            Card caravansery = CardsDataset.GameCards[CardId.Caravansery];   // 2 wood
            buyer.HandCards.Add(caravansery);

            Assert.IsTrue(buyer.BuildStructure(caravansery, buyer.HandCards, false));
            Assert.AreEqual(0, buyer.Resources[Resource.Coins]);
            Assert.AreEqual(2, west.Resources[Resource.Coins]);
            Assert.AreEqual(2, east.Resources[Resource.Coins]);
        }

        // ===== cross-check against a brute-force search =====

        [TestMethod]
        public void MatchesBruteForce_OnRandomPositions()
        {
            var rng = new Random(20261110);
            CardId[] brown = { CardId.TreeFarm, CardId.ForestCave, CardId.TimberYard, CardId.Excavation, CardId.ClayPit, CardId.Mine };
            Func<Wonder>[] boards = { () => new GizahA(), () => new AlexandriaA(), () => new AlexandriaB() };

            for (int test = 0; test < 3000; test++)
            {
                Wonder buyerBoard = boards[rng.Next(boards.Length)]();
                buyerBoard.Stage = rng.Next(0, 3);
                var (west, buyer, east) = SetUp(coins: rng.Next(0, 9), buyerBoard);
                foreach (Player p in new[] { west, buyer, east })
                {
                    foreach (Resource r in GoodsList)
                        p.Resources[r] = rng.Next(4) == 0 ? rng.Next(1, 3) : 0;
                    foreach (CardId id in brown)
                        if (rng.Next(3) == 0)
                            Build(p, id);
                    if (rng.Next(4) == 0) Build(p, CardId.Caravansery);
                    if (rng.Next(4) == 0) Build(p, CardId.Forum);
                }
                buyer.RawCheapWest = rng.Next(3) == 0;
                buyer.RawCheapEast = rng.Next(3) == 0;
                buyer.WonderRawCheap = rng.Next(5) == 0;
                buyer.ManufCheap = rng.Next(3) == 0;

                var cost = new Dictionary<Resource, int>();
                int kinds = rng.Next(1, 4);
                for (int k = 0; k < kinds; k++)
                    cost[GoodsList[rng.Next(GoodsList.Length)]] = rng.Next(1, 4);
                if (rng.Next(5) == 0)
                    cost[Resource.Coins] = 1;

                PaymentPlan? expected = BruteForce(buyer, cost);
                PaymentPlan? actual = buyer.PlanPayment(cost);
                Assert.AreEqual(expected, actual,
                    $"case {test}: cost {string.Join(",", cost.Select(kv => $"{kv.Value} {kv.Key}"))}, coins {buyer.Resources[Resource.Coins]}");
            }
        }

        /// <summary>
        /// Tries every choice (including "unused") for every either/or source, then every split of every
        /// purchase between the neighbours. Written from the rules, independently of the planner.
        /// </summary>
        private static PaymentPlan? BruteForce(Player buyer, Dictionary<Resource, int> cost)
        {
            int budget = buyer.Resources[Resource.Coins] - cost.GetValueOrDefault(Resource.Coins);
            if (budget < 0)
                return null;

            Resource[] raw = { Resource.Wood, Resource.Ore, Resource.Clay, Resource.Stone };
            Resource[] manufactured = { Resource.Loom, Resource.Glass, Resource.Papyrus };
            Resource[]? Options(CardId id, bool own) => id switch
            {
                CardId.TreeFarm => new[] { Resource.Wood, Resource.Clay },
                CardId.ForestCave => new[] { Resource.Wood, Resource.Ore },
                CardId.TimberYard => new[] { Resource.Wood, Resource.Stone },
                CardId.Excavation => new[] { Resource.Clay, Resource.Stone },
                CardId.ClayPit => new[] { Resource.Clay, Resource.Ore },
                CardId.Mine => new[] { Resource.Ore, Resource.Stone },
                CardId.Caravansery when own => raw,
                CardId.Forum when own => manufactured,
                _ => null,
            };

            // every source: (owner 0 = buyer, 1 = west, 2 = east, options)
            var sources = new List<(int Owner, Resource[] Options)>();
            Player[] owners = { buyer, buyer.PlayerWest, buyer.PlayerEast };
            for (int o = 0; o < 3; o++)
                foreach (Card c in owners[o].PlayedCards)
                    if (Options(c.Id, o == 0) is { } opts)
                        sources.Add((o, opts));
            WonderId board = buyer.Board.Id;
            int stage = buyer.Board.Stage;
            if (board == WonderId.AlexandriaA && stage >= 2 || board == WonderId.AlexandriaB && stage >= 1)
                sources.Add((0, raw));
            if (board == WonderId.AlexandriaB && stage >= 2)
                sources.Add((0, manufactured));
            // giving a source a resource the cost doesn't need is the same as leaving it unused
            sources = sources.Select(s => (s.Owner, s.Options.Where(r => cost.GetValueOrDefault(r) > 0).ToArray()))
                .Where(s => s.Item2.Length > 0).ToList();

            int bestTotal = int.MaxValue, bestEast = int.MaxValue;
            var supply = new int[3, 7];
            for (int o = 0; o < 3; o++)
                foreach (Resource r in GoodsList)
                    supply[o, (int)r] = owners[o].Resources[r];

            int Price(int seller, Resource r)
            {
                bool isRaw = raw.Contains(r);
                bool cheap = isRaw
                    ? buyer.WonderRawCheap || (seller == 1 ? buyer.RawCheapWest : buyer.RawCheapEast)
                    : buyer.ManufCheap;
                return cheap ? 1 : 2;
            }

            void Assign(int i)
            {
                if (i < sources.Count)
                {
                    Assign(i + 1);   // unused
                    foreach (Resource r in sources[i].Options)
                    {
                        supply[sources[i].Owner, (int)r]++;
                        Assign(i + 1);
                        supply[sources[i].Owner, (int)r]--;
                    }
                    return;
                }
                // every split of every missing resource
                var goods = GoodsList.Where(r => cost.GetValueOrDefault(r) > supply[0, (int)r]).ToList();
                void Split(int g, int total, int toEast)
                {
                    if (g == goods.Count)
                    {
                        if (total < bestTotal || total == bestTotal && toEast < bestEast)
                            (bestTotal, bestEast) = (total, toEast);
                        return;
                    }
                    Resource r = goods[g];
                    int n = cost[r] - supply[0, (int)r];
                    for (int fromWest = 0; fromWest <= n; fromWest++)
                    {
                        int fromEast = n - fromWest;
                        if (fromWest > supply[1, (int)r] || fromEast > supply[2, (int)r])
                            continue;
                        int e = fromEast * Price(2, r);
                        Split(g + 1, total + fromWest * Price(1, r) + e, toEast + e);
                    }
                }
                Split(0, 0, 0);
            }
            Assign(0);

            if (bestTotal == int.MaxValue || bestTotal > budget)
                return null;
            return new PaymentPlan(bestTotal - bestEast, bestEast);
        }
    }
}
