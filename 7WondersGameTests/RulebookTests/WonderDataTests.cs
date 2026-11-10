using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests.RulebookTests
{
    /// <summary>
    /// Wonder board data checked against the 1st edition (2010) rulebook.
    /// The rows are typed from the rulebook, not copied from the Wonder classes.
    /// </summary>
    [TestClass]
    public class WonderDataTests
    {
        private static readonly Resource[] Raw = { Resource.Wood, Resource.Ore, Resource.Clay, Resource.Stone };
        private static readonly Resource[] Manufactured = { Resource.Loom, Resource.Glass, Resource.Papyrus };

        [DataTestMethod]
        [DataRow("GizahA", "Stone")]          [DataRow("GizahB", "Stone")]
        [DataRow("BabylonA", "Clay")]         [DataRow("BabylonB", "Clay")]
        [DataRow("OlympiaA", "Wood")]         [DataRow("OlympiaB", "Wood")]
        [DataRow("RhodosA", "Ore")]           [DataRow("RhodosB", "Ore")]
        [DataRow("EphesosA", "Papyrus")]      [DataRow("EphesosB", "Papyrus")]
        [DataRow("AlexandriaA", "Glass")]     [DataRow("AlexandriaB", "Glass")]
        [DataRow("HalikarnassosA", "Loom")]   [DataRow("HalikarnassosB", "Loom")]
        public void Board_ProducesRulebookResource(string wonder, string resource)
        {
            Assert.AreEqual(Enum.Parse<Resource>(resource), CreateWonder(wonder).Production);
        }

        [DataTestMethod]
        [DataRow("GizahA", 3)]          [DataRow("GizahB", 4)]
        [DataRow("BabylonA", 3)]        [DataRow("BabylonB", 3)]
        [DataRow("OlympiaA", 3)]        [DataRow("OlympiaB", 3)]
        [DataRow("RhodosA", 3)]         [DataRow("RhodosB", 2)]
        [DataRow("EphesosA", 3)]        [DataRow("EphesosB", 3)]
        [DataRow("AlexandriaA", 3)]     [DataRow("AlexandriaB", 3)]
        [DataRow("HalikarnassosA", 3)]  [DataRow("HalikarnassosB", 3)]
        public void Board_HasRulebookStageCount(string wonder, int stages)
        {
            PlayerState player = SetUpPlayer(wonder);
            for (int s = 0; s < stages; s++)
            {
                GiveResources(player, 10);
                Assert.IsTrue(player.Board.AddStage(player), $"stage {s + 1}");
            }

            GiveResources(player, 10);
            player.Resources[Resource.Coins] = 100;
            Assert.IsFalse(player.CanBuildWonder(), "one stage too many");
        }

        // Columns: wonder, stage, cost, VP, coins, shields, other effect.
        [DataTestMethod]
        [DataRow("GizahA", 1, "2 Stone",            3, 0, 0, "")]
        [DataRow("GizahA", 2, "3 Wood",             5, 0, 0, "")]
        [DataRow("GizahA", 3, "4 Stone",            7, 0, 0, "")]
        [DataRow("GizahB", 1, "2 Wood",             3, 0, 0, "")]
        [DataRow("GizahB", 2, "3 Stone",            5, 0, 0, "")]
        [DataRow("GizahB", 3, "3 Clay",             5, 0, 0, "")]
        [DataRow("GizahB", 4, "4 Stone, Papyrus",   7, 0, 0, "")]
        [DataRow("BabylonA", 1, "2 Clay",           3, 0, 0, "")]
        [DataRow("BabylonA", 2, "3 Wood",           0, 0, 0, "ScienceWildcard")]
        [DataRow("BabylonA", 3, "4 Clay",           7, 0, 0, "")]
        [DataRow("BabylonB", 1, "Clay, Loom",       3, 0, 0, "")]
        [DataRow("BabylonB", 2, "2 Wood, Glass",    0, 0, 0, "PlaySeventh")]
        [DataRow("BabylonB", 3, "3 Clay, Papyrus",  0, 0, 0, "ScienceWildcard")]
        [DataRow("OlympiaA", 1, "2 Wood",           3, 0, 0, "")]
        [DataRow("OlympiaA", 2, "2 Stone",          0, 0, 0, "FreeBuildOncePerAge")]
        [DataRow("OlympiaA", 3, "2 Ore",            7, 0, 0, "")]
        [DataRow("OlympiaB", 1, "2 Wood",           0, 0, 0, "RawTradingDiscount")]
        [DataRow("OlympiaB", 2, "2 Stone",          5, 0, 0, "")]
        [DataRow("OlympiaB", 3, "2 Ore, Loom",      0, 0, 0, "CopyGuild")]
        [DataRow("RhodosA", 1, "2 Wood",            3, 0, 0, "")]
        [DataRow("RhodosA", 2, "3 Clay",            0, 0, 2, "")]
        [DataRow("RhodosA", 3, "4 Ore",             7, 0, 0, "")]
        [DataRow("RhodosB", 1, "3 Stone",           3, 3, 1, "")]
        [DataRow("RhodosB", 2, "4 Ore",             4, 4, 1, "")]
        [DataRow("EphesosA", 1, "2 Stone",          3, 0, 0, "")]
        [DataRow("EphesosA", 2, "2 Wood",           0, 9, 0, "")]
        [DataRow("EphesosA", 3, "2 Papyrus",        7, 0, 0, "")]
        [DataRow("EphesosB", 1, "2 Stone",          2, 4, 0, "")]
        [DataRow("EphesosB", 2, "2 Wood",           3, 4, 0, "")]
        [DataRow("EphesosB", 3, "Papyrus, Loom, Glass", 5, 4, 0, "")]
        [DataRow("AlexandriaA", 1, "2 Stone",       3, 0, 0, "")]
        [DataRow("AlexandriaA", 2, "2 Ore",         0, 0, 0, "RawWildcard")]
        [DataRow("AlexandriaA", 3, "2 Glass",       7, 0, 0, "")]
        [DataRow("AlexandriaB", 1, "2 Clay",        0, 0, 0, "RawWildcard")]
        [DataRow("AlexandriaB", 2, "2 Wood",        0, 0, 0, "ManufacturedWildcard")]
        [DataRow("AlexandriaB", 3, "3 Stone",       7, 0, 0, "")]
        [DataRow("HalikarnassosA", 1, "2 Clay",     3, 0, 0, "")]
        [DataRow("HalikarnassosA", 2, "3 Ore",      0, 0, 0, "BuildFromDiscard")]
        [DataRow("HalikarnassosA", 3, "2 Loom",     7, 0, 0, "")]
        [DataRow("HalikarnassosB", 1, "2 Ore",      2, 0, 0, "BuildFromDiscard")]
        [DataRow("HalikarnassosB", 2, "3 Clay",     1, 0, 0, "BuildFromDiscard")]
        [DataRow("HalikarnassosB", 3, "Glass, Papyrus, Loom", 0, 0, 0, "BuildFromDiscard")]
        public void Stage_MatchesRulebook(string wonder, int stage, string cost, int vp, int coins, int shields, string effect)
        {
            PlayerState player = SetUpPlayer(wonder);
            for (int s = 1; s < stage; s++)
            {
                GiveResources(player, 10);
                Assert.IsTrue(player.Board.AddStage(player), $"building earlier stage {s}");
            }
            int builtStages = stage - 1;
            Dictionary<Resource, int> costDict = ParseCost(cost);

            // Exactly the rulebook cost is enough (no coins, so nothing can be bought).
            SetResources(player, costDict);
            Assert.IsTrue(player.CanBuildWonder(), "exact cost should be enough");

            // One less of any resource is not enough, unless the board's own wildcard covers it.
            foreach (Resource r in costDict.Keys)
            {
                bool wildcardCovers =
                    Raw.Contains(r) && (wonder == "AlexandriaA" && builtStages >= 2 || wonder == "AlexandriaB" && builtStages >= 1) ||
                    Manufactured.Contains(r) && wonder == "AlexandriaB" && builtStages >= 2;
                if (wildcardCovers) continue;

                var lessOne = new Dictionary<Resource, int>(costDict);
                lessOne[r]--;
                SetResources(player, lessOne);
                Assert.IsFalse(player.CanBuildWonder(), $"one {r} short should not be enough");
            }

            // Build it and check the rewards.
            SetResources(player, costDict);
            int vpBefore = player.Board.WonderPoints;
            int shieldsBefore = player.Resources[Resource.Shields];
            int scienceBefore = player.CalculateScientificScore();
            player.PlaySeventh = false;
            player.FreeCardOnce = false;
            player.DiscardFree = false;
            player.WonderRawCheap = false;

            Assert.IsTrue(player.Board.AddStage(player), "build stage");
            Assert.AreEqual(stage, player.Board.Stage, "stage number");
            Assert.AreEqual(vp, player.Board.WonderPoints - vpBefore, "VP");
            Assert.AreEqual(coins, player.Resources[Resource.Coins], "coins");
            Assert.AreEqual(shields, player.Resources[Resource.Shields] - shieldsBefore, "shields");

            Assert.AreEqual(effect == "PlaySeventh", player.PlaySeventh, "play seventh card");
            Assert.AreEqual(effect == "FreeBuildOncePerAge", player.FreeCardOnce, "free build once per age");
            Assert.AreEqual(effect == "BuildFromDiscard", player.DiscardFree, "build from discard");
            Assert.AreEqual(effect == "RawTradingDiscount", player.WonderRawCheap, "raw trading discount");
            Assert.AreEqual(effect == "ScienceWildcard" ? 1 : 0, player.CalculateScientificScore() - scienceBefore, "science wildcard");

            if (effect == "RawWildcard" || effect == "ManufacturedWildcard")
            {
                Resource[] covered = effect == "RawWildcard" ? Raw : Manufactured;
                foreach (Resource r in covered)
                {
                    SetResources(player, new Dictionary<Resource, int>());
                    Assert.AreEqual(PaymentPlan.Free, player.PlanPayment(new Dictionary<Resource, int> { { r, 1 } }), $"wildcard gives {r}");
                }
            }

            if (effect == "CopyGuild")
            {
                // The east neighbour's Workers Guild is worth 1 VP per brown card next door.
                player.PlayerEast.PlayedCards.Add(CardsDataset.GameCards[CardId.Workers]);
                player.PlayerWest.PlayedCards.Add(CardsDataset.GameCards[CardId.LumberYard]);
                Assert.IsTrue(player.CopyGuild(), "copy a neighbour's guild");
            }
        }

        // ===== helpers =====

        private static Wonder CreateWonder(string name)
        {
            Type type = typeof(Wonder).Assembly.GetType("_7WondersGame.src.models.Wonders." + name)!;
            return (Wonder)Activator.CreateInstance(type)!;
        }

        private static PlayerState SetUpPlayer(string wonder)
        {
            List<PlayerState> players = MockHelper.MockPlayers(
                new List<Wonder> { CreateWonder(wonder), new GizahA(), new GizahA() });
            foreach (PlayerState p in players)
                p.Resources[Resource.Coins] = 0;
            return players[0];
        }

        private static void GiveResources(PlayerState player, int amount)
        {
            foreach (Resource r in Raw.Concat(Manufactured))
                player.Resources[r] = amount;
        }

        /// <summary>Sets raw and manufactured resources to exactly these amounts, and coins to 0.</summary>
        private static void SetResources(PlayerState player, Dictionary<Resource, int> amounts)
        {
            GiveResources(player, 0);
            foreach (var kvp in amounts)
                player.Resources[kvp.Key] = kvp.Value;
            player.Resources[Resource.Coins] = 0;
        }

        private static Dictionary<Resource, int> ParseCost(string cost)
        {
            var result = new Dictionary<Resource, int>();
            foreach (string part in cost.Split(", "))
            {
                string[] words = part.Split(' ');
                int amount = words.Length == 2 ? int.Parse(words[0]) : 1;
                result[Enum.Parse<Resource>(words[^1])] = amount;
            }
            return result;
        }
    }
}
