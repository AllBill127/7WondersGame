using _7WondersGame.src.cli;
using _7WondersGame.src.models;
using _7WondersGame.src.models.MCTS;
using Serilog.Events;

namespace _7WondersGameTests.GameTests
{
    /// <summary>
    /// Step 3b: the command line (options, agent text, ranges, log levels).
    /// </summary>
    [TestClass]
    public class CommandLineTests
    {
        [TestMethod]
        public void NoArguments_MeansHelp()
        {
            Assert.AreEqual("help", CliArgs.Parse(Array.Empty<string>()).Command);
        }

        [TestMethod]
        public void Options_InBothForms()
        {
            CliArgs cli = CliArgs.Parse(new[] { "RUN", "--games", "5", "--Seed=0x10" });

            Assert.AreEqual("run", cli.Command);
            Assert.AreEqual(5, cli.RequireInt("games", 1));
            Assert.AreEqual(16UL, cli.GetSeed("seed"));
        }

        [TestMethod]
        public void BadOptions_AreUsageErrors()
        {
            Assert.ThrowsException<UsageException>(() => CliArgs.Parse(new[] { "run", "--games" }));
            Assert.ThrowsException<UsageException>(() => CliArgs.Parse(new[] { "run", "--games", "1", "--games", "2" }));
            Assert.ThrowsException<UsageException>(() => CliArgs.Parse(new[] { "run", "games" }));
            Assert.ThrowsException<UsageException>(() => CliArgs.Parse(new[] { "run", "--gmaes", "1" }).AllowOnly("games"));
            Assert.ThrowsException<UsageException>(() => CliArgs.Parse(new[] { "run", "--games", "0" }).RequireInt("games", 1));
            Assert.ThrowsException<UsageException>(() => CliArgs.Parse(new[] { "run" }).Require("players"));
        }

        [TestMethod]
        public void HelpTopic()
        {
            CliArgs cli = CliArgs.Parse(new[] { "help", "run" });
            Assert.AreEqual("run", cli.Get("topic"));
            StringAssert.Contains(HelpText.For("run"), "--players");
            Assert.IsTrue(CliArgs.Parse(new[] { "replay", "--help" }).HelpRequested);
        }

        [TestMethod]
        public void AgentSpec_SettingsAndAliases()
        {
            List<AgentSpec> specs = AgentSpec.ParseList("DeterministicAI, RandomChoiceAI ,MCTSAIv10:c=1.5:CWL=2:iterations=500");

            Assert.AreEqual(3, specs.Count);
            var mcts = AgentCatalog.Create(specs[2], 2);
            Assert.IsInstanceOfType(mcts, typeof(MCTSAIv10));
            Assert.AreEqual(2, mcts.Id);
            Assert.AreEqual("{\"EXPLORATION_WEIGHT\":1.5,\"EXPLORATION_WEIGHT_WL\":2,\"MaxIterations\":500}", mcts.ConfigJson());
        }

        [TestMethod]
        public void AgentSpec_Defaults_AreTheClassDefaults()
        {
            var mcts = AgentCatalog.Create(AgentSpec.Parse("mctsaiv10"), 0);
            Assert.AreEqual("{\"EXPLORATION_WEIGHT\":136,\"EXPLORATION_WEIGHT_WL\":5,\"MaxIterations\":20000}", mcts.ConfigJson());
        }

        [TestMethod]
        public void AgentSpec_Mistakes_AreUsageErrors()
        {
            Assert.ThrowsException<UsageException>(() => AgentCatalog.Create(AgentSpec.Parse("Nobody"), 0));
            Assert.ThrowsException<UsageException>(() => AgentCatalog.Create(AgentSpec.Parse("MCTSAIv10:x=1"), 0));
            Assert.ThrowsException<UsageException>(() => AgentCatalog.Create(AgentSpec.Parse("MCTSAIv10:c=abc"), 0));
            Assert.ThrowsException<UsageException>(() => AgentCatalog.Create(AgentSpec.Parse("DeterministicAI:c=1"), 0));
            Assert.ThrowsException<UsageException>(() => AgentSpec.Parse("MCTSAIv10:c"));
        }

        [TestMethod]
        public void EveryCatalogAgent_CanBeCreated()
        {
            foreach (var entry in AgentCatalog.Entries)
            {
                var player = AgentCatalog.Create(AgentSpec.Parse(entry.Name), 1);
                Assert.AreEqual(entry.Name, player.GetType().Name);
            }
        }

        [TestMethod]
        public void ResultRow_RebuildsTheSameAgent()
        {
            var original = AgentCatalog.Create(AgentSpec.Parse("MCTSAIv10:c=0.1:cwl=1:iterations=1234"), 2);
            AgentSpec fromRow = AgentCatalog.FromResultRow(original.GetType().Name, original.ConfigJson());

            Assert.AreEqual(original.ConfigJson(), AgentCatalog.Create(fromRow, 2).ConfigJson());
        }

        [TestMethod]
        public void Range_GivesASweep()
        {
            var specs = AgentSpec.ParseList("DeterministicAI,RandomChoiceAI,MCTSAIv10:c=0..2:cwl=1");
            Sweep? sweep = AgentCatalog.Validate(specs, 5);

            Assert.IsNotNull(sweep);
            Assert.AreEqual(2, sweep.Seat);
            CollectionAssert.AreEqual(new[] { 0, 0.5, 1, 1.5, 2 }, Enumerable.Range(0, 5).Select(sweep.ValueAt).ToArray());
            StringAssert.Contains(AgentCatalog.Create(specs[2], 2, sweep.ValueAt(3)).ConfigJson(), "\"EXPLORATION_WEIGHT\":1.5");
        }

        [TestMethod]
        public void Range_MatchesTheOldCSweepValues()
        {
            // old SearchOptimalCParallel: c = minC + k * ((maxC - minC) / iterations), iterations + 1 values
            double minC = 0.3, maxC = 2.1;
            int iterations = 7;
            var sweep = new Sweep(2, "c", minC, maxC, iterations + 1);
            for (int k = 0; k <= iterations; k++)
                Assert.AreEqual(minC + k * ((maxC - minC) / iterations), sweep.ValueAt(k));
        }

        [TestMethod]
        public void Range_Mistakes_AreUsageErrors()
        {
            var oneRange = AgentSpec.ParseList("DeterministicAI,RandomChoiceAI,MCTSAIv10:c=0..2");
            Assert.ThrowsException<UsageException>(() => AgentCatalog.Validate(oneRange, null), "range without --steps");
            Assert.ThrowsException<UsageException>(() => AgentCatalog.Validate(
                AgentSpec.ParseList("DeterministicAI,RandomChoiceAI,RandomChoiceAI"), 3), "--steps without a range");
            Assert.ThrowsException<UsageException>(() => AgentCatalog.Validate(
                AgentSpec.ParseList("MCTSAIv10:c=0..1,RandomChoiceAI,MCTSAIv10:c=0..2"), 3), "two ranges");
        }

        [TestMethod]
        public void LogLevel_Precedence()
        {
            string? saved = Environment.GetEnvironmentVariable(Logging.EnvVar);
            try
            {
                Environment.SetEnvironmentVariable(Logging.EnvVar, "verbose");
                Assert.AreEqual(LogEventLevel.Verbose, Logging.Resolve(null).Level, "environment variable");
                Assert.AreEqual(LogEventLevel.Information, Logging.Resolve("info").Level, "--log wins");
                Assert.AreEqual(LogEventLevel.Debug, Logging.Resolve(null, LogEventLevel.Debug).Level, "replay's default");

                Environment.SetEnvironmentVariable(Logging.EnvVar, null);
                Assert.AreNotEqual(Logging.EnvVar, Logging.Resolve(null).Source, "falls back to the build");
                Assert.ThrowsException<UsageException>(() => Logging.Resolve("loud"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(Logging.EnvVar, saved);
            }
        }

        [TestMethod]
        public void ResultsCsv_RowsReadBack()
        {
            var seat = new SeatResult(1, "MCTSAIv10", "{\"a\":1,\"b\":\"x,y\"}", "Gizah", "B", 50, 3, 1, 0.5);
            string row = ResultsCsvWriter.FormatRow("run1", 4, 2, 99, seat);

            List<string> fields = ResultsCsvWriter.SplitRow(row);

            Assert.AreEqual(ResultsCsvWriter.Columns.Length, fields.Count);
            Assert.AreEqual(seat.ConfigJson, fields[Array.IndexOf(ResultsCsvWriter.Columns, "configJson")]);
            Assert.AreEqual("50", fields[Array.IndexOf(ResultsCsvWriter.Columns, "vp")]);
        }
    }
}
