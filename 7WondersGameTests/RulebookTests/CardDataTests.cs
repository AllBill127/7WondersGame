using System.Reflection;
using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests.RulebookTests
{
    /// <summary>
    /// Card data checked against the 1st edition (2010) rulebook card list.
    /// The rows below are typed from the rulebook, not copied from CardsDataset,
    /// so a typo in the dataset makes a row fail.
    /// </summary>
    [TestClass]
    public class CardDataTests
    {
        // Columns: name, era, type, cost, chained from, copies for 3 4 5 6 7 players.
        // Cost is written like the rulebook: "2 Ore, Glass". "" means free.
        // Guilds are not in the fixed deck (players + 2 are drawn at random), so their copies are 0.
        [DataTestMethod]
        // ===== Age I =====
        [DataRow("Lumber Yard",         1, "Materials",    "",            "", "1 2 2 2 2")]
        [DataRow("Stone Pit",           1, "Materials",    "",            "", "1 1 2 2 2")]
        [DataRow("Clay Pool",           1, "Materials",    "",            "", "1 1 2 2 2")]
        [DataRow("Ore Vein",            1, "Materials",    "",            "", "1 2 2 2 2")]
        [DataRow("Tree Farm",           1, "Materials",    "1 Coin",      "", "0 0 0 1 1")]
        [DataRow("Excavation",          1, "Materials",    "1 Coin",      "", "0 1 1 1 1")]
        [DataRow("Clay Pit",            1, "Materials",    "1 Coin",      "", "1 1 1 1 1")]
        [DataRow("Timber Yard",         1, "Materials",    "1 Coin",      "", "1 1 1 1 1")]
        [DataRow("Forest Cave",         1, "Materials",    "1 Coin",      "", "0 0 1 1 1")]
        [DataRow("Mine",                1, "Materials",    "1 Coin",      "", "0 0 0 1 1")]
        [DataRow("Loom",                1, "Manufactured", "",            "", "1 1 1 2 2")]
        [DataRow("Glassworks",          1, "Manufactured", "",            "", "1 1 1 2 2")]
        [DataRow("Press",               1, "Manufactured", "",            "", "1 1 1 2 2")]
        [DataRow("Pawnshop",            1, "Civilian",     "",            "", "0 1 1 1 2")]
        [DataRow("Baths",               1, "Civilian",     "Stone",       "", "1 1 1 1 2")]
        [DataRow("Altar",               1, "Civilian",     "",            "", "1 1 2 2 2")]
        [DataRow("Theater",             1, "Civilian",     "",            "", "1 1 1 2 2")]
        [DataRow("Tavern",              1, "Commercial",   "",            "", "0 1 2 2 3")]
        [DataRow("East Trading Post",   1, "Commercial",   "",            "", "1 1 1 1 2")]
        [DataRow("West Trading Post",   1, "Commercial",   "",            "", "1 1 1 1 2")]
        [DataRow("Marketplace",         1, "Commercial",   "",            "", "1 1 1 2 2")]
        [DataRow("Stockade",            1, "Military",     "Wood",        "", "1 1 1 1 2")]
        [DataRow("Barracks",            1, "Military",     "Ore",         "", "1 1 2 2 2")]
        [DataRow("Guard Tower",         1, "Military",     "Clay",        "", "1 2 2 2 2")]
        [DataRow("Apothecary",          1, "Scientific",   "Loom",        "", "1 1 2 2 2")]
        [DataRow("Workshop",            1, "Scientific",   "Glass",       "", "1 1 1 1 2")]
        [DataRow("Scriptorium",         1, "Scientific",   "Papyrus",     "", "1 2 2 2 2")]
        // ===== Age II =====
        [DataRow("Sawmill",             2, "Materials",    "1 Coin",      "", "1 2 2 2 2")]
        [DataRow("Quarry",              2, "Materials",    "1 Coin",      "", "1 2 2 2 2")]
        [DataRow("Brickyard",           2, "Materials",    "1 Coin",      "", "1 2 2 2 2")]
        [DataRow("Foundry",             2, "Materials",    "1 Coin",      "", "1 2 2 2 2")]
        [DataRow("Loom",                2, "Manufactured", "",            "", "1 1 2 2 2")]
        [DataRow("Glassworks",          2, "Manufactured", "",            "", "1 1 2 2 2")]
        [DataRow("Press",               2, "Manufactured", "",            "", "1 1 2 2 2")]
        [DataRow("Aqueduct",            2, "Civilian",     "3 Stone",            "Baths",       "1 1 1 1 2")]
        [DataRow("Temple",              2, "Civilian",     "Wood, Clay, Glass",  "Altar",       "1 1 1 2 2")]
        [DataRow("Statue",              2, "Civilian",     "2 Ore, Wood",        "Theater",     "1 1 1 1 2")]
        [DataRow("Courthouse",          2, "Civilian",     "2 Clay, Loom",       "Scriptorium", "1 1 2 2 2")]
        [DataRow("Forum",               2, "Commercial",   "2 Clay",             "East Trading Post|West Trading Post", "1 1 1 2 3")]
        [DataRow("Caravansery",         2, "Commercial",   "2 Wood",             "Marketplace", "1 1 2 3 3")]
        [DataRow("Vineyard",            2, "Commercial",   "",                   "",            "1 1 1 2 2")]
        [DataRow("Bazar",               2, "Commercial",   "",                   "",            "0 1 1 1 2")]
        [DataRow("Walls",               2, "Military",     "3 Stone",            "",            "1 1 1 1 2")]
        [DataRow("Training Ground",     2, "Military",     "2 Ore, Wood",        "",            "0 1 1 2 3")]
        [DataRow("Stables",             2, "Military",     "Clay, Wood, Ore",    "Apothecary",  "1 1 2 2 2")]
        [DataRow("Archery Range",       2, "Military",     "2 Wood, Ore",        "Workshop",    "1 1 1 2 2")]
        [DataRow("Dispensary",          2, "Scientific",   "2 Ore, Glass",       "Apothecary",  "1 2 2 2 2")]
        [DataRow("Laboratory",          2, "Scientific",   "2 Clay, Papyrus",    "Workshop",    "1 1 2 2 2")]
        [DataRow("Library",             2, "Scientific",   "2 Stone, Loom",      "Scriptorium", "1 1 1 2 2")]
        [DataRow("School",              2, "Scientific",   "Wood, Papyrus",      "",            "1 1 1 1 2")]
        // ===== Age III =====
        [DataRow("Pantheon",            3, "Civilian",     "2 Clay, Ore, Loom, Glass, Papyrus",             "Temple",          "1 1 1 2 2")]
        [DataRow("Gardens",             3, "Civilian",     "2 Clay, Wood",                                  "Statue",          "1 2 2 2 2")]
        [DataRow("Town Hall",           3, "Civilian",     "2 Stone, Ore, Glass",                           "",                "1 1 2 3 3")]
        [DataRow("Palace",              3, "Civilian",     "Wood, Ore, Clay, Stone, Loom, Glass, Papyrus",  "",                "1 1 1 1 2")]
        [DataRow("Senate",              3, "Civilian",     "2 Wood, Stone, Ore",                            "Library",         "1 1 2 2 2")]
        [DataRow("Haven",               3, "Commercial",   "Wood, Ore, Loom",                               "Forum",           "1 2 2 2 2")]
        [DataRow("Lighthouse",          3, "Commercial",   "Stone, Glass",                                  "Caravansery",     "1 1 1 2 2")]
        [DataRow("Chamber of Commerce", 3, "Commercial",   "2 Clay, Papyrus",                               "",                "0 1 1 2 2")]
        [DataRow("Arena",               3, "Commercial",   "2 Stone, Ore",                                  "Dispensary",      "1 1 2 2 3")]
        [DataRow("Fortifications",      3, "Military",     "3 Ore, Stone",                                  "Walls",           "1 1 1 1 2")]
        [DataRow("Circus",              3, "Military",     "3 Stone, Ore",                                  "Training Ground", "0 1 2 3 3")]
        [DataRow("Arsenal",             3, "Military",     "2 Wood, Ore, Loom",                             "",                "1 2 2 2 3")]
        [DataRow("Siege Workshop",      3, "Military",     "3 Clay, Wood",                                  "Laboratory",      "1 1 2 2 2")]
        [DataRow("Lodge",               3, "Scientific",   "2 Clay, Loom, Papyrus",                         "Dispensary",      "1 1 1 2 2")]
        [DataRow("Observatory",         3, "Scientific",   "2 Ore, Glass, Loom",                            "Laboratory",      "1 1 1 1 2")]
        [DataRow("University",          3, "Scientific",   "2 Wood, Papyrus, Glass",                        "Library",         "1 2 2 2 2")]
        [DataRow("Academy",             3, "Scientific",   "3 Stone, Glass",                                "School",          "1 1 1 1 2")]
        [DataRow("Study",               3, "Scientific",   "Wood, Papyrus, Loom",                           "School",          "1 1 2 2 2")]
        // ===== Guilds =====
        [DataRow("Workers Guild",       3, "Guild",        "2 Ore, Clay, Stone, Wood",  "", "0 0 0 0 0")]
        [DataRow("Craftsmens Guild",    3, "Guild",        "2 Ore, 2 Stone",            "", "0 0 0 0 0")]
        [DataRow("Traders Guild",       3, "Guild",        "Loom, Papyrus, Glass",      "", "0 0 0 0 0")]
        [DataRow("Philosophers Guild",  3, "Guild",        "3 Clay, Loom, Papyrus",     "", "0 0 0 0 0")]
        [DataRow("Spies Guild",         3, "Guild",        "3 Clay, Glass",             "", "0 0 0 0 0")]
        [DataRow("Strategists Guild",   3, "Guild",        "2 Ore, Stone, Loom",        "", "0 0 0 0 0")]
        [DataRow("Shipowners Guild",    3, "Guild",        "3 Wood, Glass, Papyrus",    "", "0 0 0 0 0")]
        [DataRow("Scientists Guild",    3, "Guild",        "2 Wood, 2 Ore, Papyrus",    "", "0 0 0 0 0")]
        [DataRow("Magistrates Guild",   3, "Guild",        "3 Wood, Stone, Loom",       "", "0 0 0 0 0")]
        [DataRow("Builders Guild",      3, "Guild",        "2 Stone, 2 Clay, Glass",    "", "0 0 0 0 0")]
        public void Card_MatchesRulebook(string name, int era, string type, string cost, string chainFrom, string copies)
        {
            Card card = FindCard(DatasetFor(3), name, era);

            Assert.AreEqual(Enum.Parse<CardType>(type), card.Type, "type");
            CollectionAssert.AreEquivalent(ParseCost(cost), card.Cost.Where(kvp => kvp.Value > 0).ToList(), "cost");

            if (chainFrom == "")
                Assert.AreEqual(CardId.None, card.FreeWithId, "chained from");
            else if (chainFrom == "East Trading Post|West Trading Post")
                Assert.AreEqual(CardId.AnyTradingPost, card.FreeWithId, "chained from");
            else
                Assert.AreEqual(chainFrom, DatasetFor(3).GameCards[card.FreeWithId].Name, "chained from");

            int[] expectedCopies = copies.Split(' ').Select(int.Parse).ToArray();
            for (int players = 3; players <= 7; players++)
            {
                Card cardForCount = FindCard(DatasetFor(players), name, era);
                Assert.AreEqual(expectedCopies[players - 3], cardForCount.NumCardsInGame, $"copies with {players} players");
            }
        }

        [TestMethod]
        public void Dataset_HasEveryRulebookCardAndNoOthers()
        {
            // 27 + 23 + 18 cards in the three ages (Loom, Glassworks and Press in both I and II), plus 10 guilds.
            Assert.AreEqual(27 + 23 + 18 + 10, DatasetFor(3).GameCards.Count);
        }

        /// <summary>
        /// Rulebook setup: Age I and II decks hold 7 cards per player.
        /// Age III holds 7 per player including players + 2 guilds.
        /// </summary>
        [DataTestMethod]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        [DataRow(7)]
        public void DeckSizes_MatchRulebook(int players)
        {
            CardsDataset ds = DatasetFor(players);
            int CountEra(int era) => ds.Cards.Values.Where(c => c.Era == era).Sum(c => c.NumCardsInGame);

            Assert.AreEqual(7 * players, CountEra(1), "Age I");
            Assert.AreEqual(7 * players, CountEra(2), "Age II");
            Assert.AreEqual(7 * players - (players + 2), CountEra(3), "Age III without guilds");
        }

        [DataTestMethod]
        [DataRow("Pawnshop",    3)]
        [DataRow("Baths",       3)]
        [DataRow("Altar",       2)]
        [DataRow("Theater",     2)]
        [DataRow("Aqueduct",    5)]
        [DataRow("Temple",      3)]
        [DataRow("Statue",      4)]
        [DataRow("Courthouse",  4)]
        [DataRow("Pantheon",    7)]
        [DataRow("Gardens",     5)]
        [DataRow("Town Hall",   6)]
        [DataRow("Palace",      8)]
        [DataRow("Senate",      6)]
        public void CivilianCard_GivesRulebookVP(string name, int vp)
        {
            Player player = BuildForFree(name);
            Assert.AreEqual(vp, player.CalculateCivilianScore());
        }

        [DataTestMethod]
        [DataRow("Stockade",        1)]
        [DataRow("Barracks",        1)]
        [DataRow("Guard Tower",     1)]
        [DataRow("Walls",           2)]
        [DataRow("Training Ground", 2)]
        [DataRow("Stables",         2)]
        [DataRow("Archery Range",   2)]
        [DataRow("Fortifications",  3)]
        [DataRow("Circus",          3)]
        [DataRow("Arsenal",         3)]
        [DataRow("Siege Workshop",  3)]
        public void MilitaryCard_GivesRulebookShields(string name, int shields)
        {
            Player player = BuildForFree(name);
            Assert.AreEqual(shields, player.Resources[Resource.Shields]);
        }

        [DataTestMethod]
        [DataRow("Apothecary",  "Compass")]
        [DataRow("Workshop",    "Gear")]
        [DataRow("Scriptorium", "Tablet")]
        [DataRow("Dispensary",  "Compass")]
        [DataRow("Laboratory",  "Gear")]
        [DataRow("Library",     "Tablet")]
        [DataRow("School",      "Tablet")]
        [DataRow("Lodge",       "Compass")]
        [DataRow("Observatory", "Gear")]
        [DataRow("University",  "Tablet")]
        [DataRow("Academy",     "Compass")]
        [DataRow("Study",       "Gear")]
        public void ScienceCard_GivesRulebookSymbol(string name, string symbol)
        {
            Player player = BuildForFree(name);
            Resource expected = Enum.Parse<Resource>(symbol);
            foreach (Resource r in new[] { Resource.Gear, Resource.Compass, Resource.Tablet })
                Assert.AreEqual(r == expected ? 1 : 0, player.Resources[r], r.ToString());
        }

        [DataTestMethod]
        [DataRow("Lumber Yard", 1, "Wood",    1)]
        [DataRow("Stone Pit",   1, "Stone",   1)]
        [DataRow("Clay Pool",   1, "Clay",    1)]
        [DataRow("Ore Vein",    1, "Ore",     1)]
        [DataRow("Sawmill",     2, "Wood",    2)]
        [DataRow("Quarry",      2, "Stone",   2)]
        [DataRow("Brickyard",   2, "Clay",    2)]
        [DataRow("Foundry",     2, "Ore",     2)]
        [DataRow("Loom",        1, "Loom",    1)]
        [DataRow("Glassworks",  1, "Glass",   1)]
        [DataRow("Press",       1, "Papyrus", 1)]
        public void SingleResourceCard_ProducesRulebookAmount(string name, int era, string resource, int amount)
        {
            Player player = BuildForFree(name, era);
            Assert.AreEqual(amount, player.Resources[Enum.Parse<Resource>(resource)]);
        }

        // ===== helpers =====

        private static readonly Dictionary<int, CardsDataset> datasets = new();

        /// <summary>
        /// CardsDataset is a singleton fixed to the player count of its first use,
        /// so build a private instance per player count.
        /// </summary>
        private static CardsDataset DatasetFor(int players)
        {
            lock (datasets)
            {
                if (!datasets.TryGetValue(players, out var ds))
                {
                    ds = (CardsDataset)Activator.CreateInstance(typeof(CardsDataset),
                        BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { players }, null)!;
                    datasets[players] = ds;
                }
                return ds;
            }
        }

        private static Card FindCard(CardsDataset ds, string name, int era)
        {
            var matches = ds.GameCards.Values.Where(c => c.Name == name && c.Era == era).ToList();
            Assert.AreEqual(1, matches.Count, $"cards named '{name}' in age {era}");
            return matches[0];
        }

        private static List<KeyValuePair<Resource, int>> ParseCost(string cost)
        {
            var result = new List<KeyValuePair<Resource, int>>();
            if (cost == "") return result;

            foreach (string part in cost.Split(", "))
            {
                string[] words = part.Split(' ');
                int amount = words.Length == 2 ? int.Parse(words[0]) : 1;
                string name = words[^1];
                Resource resource = name == "Coin" ? Resource.Coins : Enum.Parse<Resource>(name);
                result.Add(new KeyValuePair<Resource, int>(resource, amount));
            }
            return result;
        }

        private static Player BuildForFree(string name, int era = 0)
        {
            Card card = era == 0
                ? DatasetFor(3).GameCards.Values.Single(c => c.Name == name)
                : FindCard(DatasetFor(3), name, era);

            List<Player> players = MockHelper.MockRandomChoiceAIPlayers(
                new List<Wonder> { new GizahA(), new BabylonA(), new OlympiaA() });
            Player player = players[0];

            Assert.IsTrue(player.BuildStructure(card, new List<Card> { card }, true));
            return player;
        }
    }
}
