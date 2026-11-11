using _7WondersGame.src.models;
using _7WondersGame.src.models.Agents;

namespace _7WondersGameTests.GameTests
{
    /// <summary>Step 7 (X8): dataset files say their player count, and every deal is checked when read.</summary>
    [TestClass]
    public class DatasetFileTests
    {
        /// <summary>The deal <see cref="Game.InitGame"/> makes, as a dataset deal.</summary>
        private static List<int>[] Deal(int players, ulong seed)
        {
            var game = new Game(seed) { Agents = Enumerable.Range(0, players).Select(_ => (IAgent)new RandomChoiceAI()).ToList() };
            game.InitGame();
            var deal = new List<int>[4];
            for (int era = 0; era < 3; era++)
                deal[era] = game.Decks[era].Select(c => (int)c.Id).ToList();
            deal[3] = game.Players.Select(p => (int)p.Board.Id).ToList();
            return deal;
        }

        private static string[] Lines(int players, params List<int>[][] deals)
        {
            var lines = new List<string> { $"Players: {players}" };
            for (int i = 0; i < deals.Length; i++)
            {
                lines.Add($"Game {i}:");
                for (int era = 0; era < 3; era++)
                {
                    lines.Add($"Deck{era + 1}:");
                    lines.Add(string.Join(", ", deals[i][era]));
                }
                lines.Add("Wonders:");
                lines.Add(string.Join(", ", deals[i][3]));
            }
            return lines.ToArray();
        }

        [TestMethod]
        public async Task WrittenDeals_ReadBack()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dataset-{Guid.NewGuid():N}.txt");
            try
            {
                var deals = new List<List<int>[]> { Deal(4, 1), Deal(4, 2) };
                await Filer.WriteGameDatasetToFile(path, 4, deals);

                var (players, read) = await Filer.ReadGameDatasetFromFile(path);

                Assert.AreEqual(4, players);
                Assert.AreEqual(2, read.Count);
                for (int i = 0; i < 2; i++)
                    for (int part = 0; part < 4; part++)
                        CollectionAssert.AreEqual(deals[i][part], read[i][part]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void EveryDealTheGameMakes_IsValid()
        {
            for (int players = 3; players <= 7; players++)
                for (ulong seed = 0; seed < 20; seed++)
                    Filer.CheckDeal(Deal(players, seed), players, $"{players} players, seed {seed}");
        }

        [TestMethod]
        public void TheCommittedDatasets_AreValid()
        {
            DirectoryInfo dir = new(AppContext.BaseDirectory);
            while (dir.GetFiles("7WondersGame.sln").Length == 0)
                dir = dir.Parent ?? throw new AssertInconclusiveException("repository root not found");

            FileInfo[] files = dir.GetFiles("gameDataset_*.txt");
            Assert.IsTrue(files.Length > 0);
            foreach (FileInfo file in files)
            {
                var (players, deals) = Filer.Parse(File.ReadAllLines(file.FullName), file.Name);
                StringAssert.StartsWith(file.Name, $"gameDataset_{players}p_{deals.Count}g");
            }
        }

        [TestMethod]
        public void AFileWithoutTheHeader_IsRefused()
        {
            string[] lines = Lines(3, Deal(3, 1));
            var ex = Assert.ThrowsException<InvalidDataException>(() => Filer.Parse(lines.Skip(1).ToArray(), "old.txt"));
            StringAssert.Contains(ex.Message, "Players:");
        }

        [TestMethod]
        public void DealsForAnotherPlayerCount_AreRefused()
        {
            string[] lines = Lines(3, Deal(4, 1));
            Assert.ThrowsException<InvalidDataException>(() => Filer.Parse(lines, "wrong.txt"));
        }

        [DataTestMethod]
        [DataRow("duplicate card")]
        [DataRow("wrong era")]
        [DataRow("unknown card")]
        [DataRow("same wonder twice")]
        [DataRow("one guild twice")]
        public void ABadDeal_IsRefused(string fault)
        {
            List<int>[] deal = Deal(3, 5);
            switch (fault)
            {
                case "duplicate card": deal[0][0] = deal[0][1]; break;
                case "wrong era": (deal[0][0], deal[1][0]) = (deal[1][0], deal[0][0]); break;
                case "unknown card": deal[2][0] = 999; break;
                case "same wonder twice": deal[3][1] = deal[3][0] % 7 + 7; break;
                case "one guild twice":
                    var guilds = deal[2].Where(id => CardsDataset.GuildCards.ContainsKey((CardId)id)).ToList();
                    deal[2][deal[2].IndexOf(guilds[0])] = guilds[1];
                    break;
            }
            Assert.ThrowsException<InvalidDataException>(() => Filer.CheckDeal(deal, 3, fault));
        }
    }
}
