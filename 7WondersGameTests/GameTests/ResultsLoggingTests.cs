using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;

namespace _7WondersGameTests.GameTests
{
    /// <summary>
    /// B2: one CSV row per (game, seat), written by a single appending writer.
    /// </summary>
    [TestClass]
    public class ResultsLoggingTests
    {
        private string _dir = "";

        [TestInitialize]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "7w-results-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TestCleanup]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        [TestMethod]
        public void Loop_WithRecord_GivesOneResultPerSeat()
        {
            Game game = NewGame(3);
            game.InitGame();
            var wonders = game.Players.Select(p => p.Board.Id.ToString()).ToList();

            Dictionary<int, int> scores = game.Loop(record: true);

            List<SeatResult> results = game.Results!;
            Assert.AreEqual(3, results.Count);
            for (int seat = 0; seat < 3; seat++)
            {
                SeatResult r = results[seat];
                Assert.AreEqual(seat, r.Seat);
                Assert.AreEqual(scores[seat], r.VictoryPoints);
                Assert.AreEqual(wonders[seat], r.Wonder + r.Side);
                Assert.IsTrue(r.Side == "A" || r.Side == "B");
            }
            Assert.AreEqual("RandomChoiceAI", results[0].Agent);
            Assert.AreEqual("DeterministicAI", results[1].Agent);
            Assert.AreEqual(1.0, results.Sum(r => r.WinShare), 1e-9, "win shares add up to 1");
            Assert.IsTrue(results.Any(r => r.Rank == 1));
        }

        [TestMethod]
        public void Loop_WithoutRecord_KeepsNoResults()
        {
            Game game = NewGame(3);
            game.InitGame();
            game.Loop();
            Assert.IsNull(game.Results);
        }

        [TestMethod]
        public void ConfigJson_ListsTheAgentsOwnSettings()
        {
            Assert.AreEqual("{}", new RandomChoiceAI().ConfigJson());
            Assert.AreEqual("{\"C\":2.5,\"Iterations\":100}", new SettingsTestAgent { C = 2.5 }.ConfigJson());
        }

        [TestMethod]
        public void FormatRow_QuotesJsonAndUsesInvariantNumbers()
        {
            var seat = new SeatResult(2, "SomeAgent", "{\"c\":1.5,\"n\":2}", "Gizah", "B", 51, 7, 1, 0.5, "{\"searches\":3,\"iterations\":30}");

            string row = ResultsCsvWriter.FormatRow("run1", 4, 1, 18446744073709551615UL, seat);

            Assert.AreEqual("run1,4,1,18446744073709551615,2,SomeAgent,\"{\"\"c\"\":1.5,\"\"n\"\":2}\",Gizah,B,51,7,1,0.5," +
                            "\"{\"\"searches\"\":3,\"\"iterations\"\":30}\"", row);
        }

        [TestMethod]
        public void Writer_WritesHeaderOnceAndAppendsAcrossRuns()
        {
            string path = Path.Combine(_dir, "sub", "results.csv");
            SeatResult[] game = Seats(3);

            using (var w = new ResultsCsvWriter(path))
                w.WriteGame("runA", 0, 0, 11, game);
            using (var w = new ResultsCsvWriter(path))
                w.WriteGame("runB", 0, 0, 12, game);

            string[] lines = File.ReadAllLines(path);
            Assert.AreEqual(ResultsCsvWriter.Header, lines[0]);
            Assert.AreEqual(1 + 6, lines.Length);
            Assert.AreEqual(3, lines.Count(l => l.StartsWith("runA,")));
            Assert.AreEqual(3, lines.Count(l => l.StartsWith("runB,")));
        }

        [TestMethod]
        public void Writer_RefusesAFileWithADifferentHeader()
        {
            string path = Path.Combine(_dir, "old.csv");
            File.WriteAllText(path, "Player_0,Player_1\n1,2\n");

            Assert.ThrowsException<InvalidOperationException>(() => new ResultsCsvWriter(path));
        }

        [TestMethod]
        public void Writer_KeepsEachGamesRowsWholeUnderParallelWrites()
        {
            string path = Path.Combine(_dir, "parallel.csv");
            using (var w = new ResultsCsvWriter(path))
                Parallel.For(0, 200, i => w.WriteGame("run", i, i, (ulong)i, Seats(3)));

            var rows = File.ReadAllLines(path).Skip(1).Select(l => l.Split(',')).ToList();
            Assert.AreEqual(600, rows.Count);
            Assert.IsTrue(rows.All(r => r.Length == ResultsCsvWriter.Columns.Length));
            // a game's 3 rows are written together
            for (int k = 0; k < rows.Count; k += 3)
            {
                Assert.AreEqual(rows[k][1], rows[k + 1][1]);
                Assert.AreEqual(rows[k][1], rows[k + 2][1]);
                CollectionAssert.AreEqual(new[] { "0", "1", "2" }, rows.Skip(k).Take(3).Select(r => r[4]).ToArray());
            }
        }

        // ===== helpers =====

        private static Game NewGame(ulong seed)
        {
            Game game = new Game(seed);
            game.Agents.Add(new RandomChoiceAI());
            game.Agents.Add(new DeterministicAI());
            game.Agents.Add(new RandomChoiceAI());
            return game;
        }

        private static SeatResult[] Seats(int n) =>
            Enumerable.Range(0, n)
                .Select(s => new SeatResult(s, "RandomChoiceAI", "{}", "Rhodos", "A", 40 + s, 3, n - s, s == n - 1 ? 1 : 0))
                .ToArray();
    }
}
