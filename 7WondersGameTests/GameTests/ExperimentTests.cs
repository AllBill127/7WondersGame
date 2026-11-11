using _7WondersGame.src.cli;
using _7WondersGame.src.models;

namespace _7WondersGameTests.GameTests
{
    /// <summary>Step 7 (B3): seat rotation and experiment files.</summary>
    [TestClass]
    public class ExperimentTests
    {
        [ClassInitialize]
        public static void RegisterTestAgent(TestContext _) => SettingsTestAgent.Register();

        private string _dir = "";

        [TestInitialize]
        public void MakeFolder() => Directory.CreateDirectory(_dir = Path.Combine(Path.GetTempPath(), $"experiment-{Guid.NewGuid():N}"));

        [TestCleanup]
        public void RemoveFolder() => Directory.Delete(_dir, recursive: true);

        private string Write(string name, string text)
        {
            string path = Path.Combine(_dir, name);
            File.WriteAllText(path, text);
            return path;
        }

        private static RunPlan Plan(params string[] args) => RunPlan.FromCli(CliArgs.Parse(args.Prepend("run").ToArray()));

        private const string ThreeRandom = "\"players\": \"RandomChoiceAI,RandomChoiceAI,RandomChoiceAI\", \"games\": 1, \"seed\": 1";

        [TestMethod]
        public void Rotation_PlaysEveryDealInEverySeating()
        {
            RunPlan plan = Plan("--players", "RandomChoiceAI,HeuristicAI,DeterministicAI", "--games", "2", "--seed", "1", "--rotate", "yes");

            Assert.AreEqual(6, plan.Total);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 1, 1 }, Enumerable.Range(0, 6).Select(i => plan.At(i).Deal).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0, 1, 2 }, Enumerable.Range(0, 6).Select(i => plan.At(i).Rotation).ToArray());
            Assert.AreEqual("RandomChoiceAI,HeuristicAI,DeterministicAI", string.Join(",", plan.SeatsFor(0)));
            Assert.AreEqual("DeterministicAI,RandomChoiceAI,HeuristicAI", string.Join(",", plan.SeatsFor(1)), "the first listed agent moves to seat 1");
            Assert.AreEqual("HeuristicAI,DeterministicAI,RandomChoiceAI", string.Join(",", plan.SeatsFor(2)));
        }

        [TestMethod]
        public void WithoutRotation_GamesAreNumberedAsBefore()
        {
            RunPlan plan = Plan("--players", "SettingsTestAgent:c=0..1,RandomChoiceAI,RandomChoiceAI", "--steps", "3", "--games", "4", "--seed", "1");

            Assert.AreEqual(12, plan.Total);
            for (int i = 0; i < 12; i++)
            {
                GameSlot slot = plan.At(i);
                Assert.AreEqual((i / 4, i % 4, 0), (slot.Step, slot.Deal, slot.Rotation));
                Assert.AreEqual(plan.Sweep!.ValueAt(i / 4), slot.Value);
            }
        }

        [TestMethod]
        public void RotationAndASweep_Together()
        {
            RunPlan plan = Plan("--players", "SettingsTestAgent:c=0..1,RandomChoiceAI,RandomChoiceAI,RandomChoiceAI",
                "--steps", "2", "--games", "3", "--seed", "1", "--rotate", "true");

            Assert.AreEqual(3 * 4 * 2, plan.Total);
            GameSlot last = plan.At(plan.Total - 1);
            Assert.AreEqual((1, 2, 3, 1.0), (last.Step, last.Deal, last.Rotation, last.Value!.Value));
        }

        [TestMethod]
        public void AnExperimentFile_GivesTheSamePlanAsTheCommandLine()
        {
            Write("deals.txt", "not read here");
            string file = Write("exp.json", @"{
                // comments and trailing commas are fine
                ""description"": ""C sweep"",
                ""Players"": [""SettingsTestAgent:c=0.3..2"", ""RandomChoiceAI"", ""RandomChoiceAI""],
                ""games"": 50,
                ""seed"": ""0x10"",
                ""steps"": 6,
                ""rotate"": true,
                ""dataset"": ""deals.txt"",
                ""parallel"": 2,
            }");

            RunPlan fromFile = Plan("--experiment", file, "--parallel", "8");
            RunPlan fromCli = Plan("--players", "SettingsTestAgent:c=0.3..2,RandomChoiceAI,RandomChoiceAI", "--games", "50",
                "--seed", "16", "--steps", "6", "--rotate", "yes");

            Assert.AreEqual(string.Join(",", fromCli.Players), string.Join(",", fromFile.Players));
            Assert.AreEqual((fromCli.Games, fromCli.Seed, fromCli.Sweep, fromCli.Rotate),
                (fromFile.Games, fromFile.Seed, fromFile.Sweep, fromFile.Rotate));
            Assert.AreEqual(8, fromFile.Parallel, "the command line wins for --parallel");
            Assert.AreEqual(Path.Combine(_dir, "deals.txt"), fromFile.Dataset, "a dataset next to the file");
            Assert.AreEqual("C sweep", fromFile.Description);
            Assert.AreEqual(file, fromFile.Experiment);
        }

        [TestMethod]
        public void ExperimentFile_Mistakes_AreUsageErrors()
        {
            string ok = Write("ok.json", "{ " + ThreeRandom + " }");
            Assert.AreEqual(3, Plan("--experiment", ok).Players.Count);

            Assert.ThrowsException<UsageException>(() => Plan("--experiment", ok, "--games", "5"), "the file sets the games");
            Assert.ThrowsException<UsageException>(() => Plan("--experiment", Path.Combine(_dir, "missing.json")), "missing");
            Assert.ThrowsException<UsageException>(() => Plan("--experiment", Write("a.json", "{ \"players\": ")), "not JSON");
            Assert.ThrowsException<UsageException>(() => Plan("--experiment", Write("b.json", "[1]")), "not an object");
            Assert.ThrowsException<UsageException>(() => Plan("--experiment", Write("c.json", "{ " + ThreeRandom + ", \"gmaes\": 2 }")), "unknown key");
            Assert.ThrowsException<UsageException>(() => Plan("--experiment", Write("d.json", "{ " + ThreeRandom + ", \"Games\": 2 }")), "twice");
            Assert.ThrowsException<UsageException>(() => Plan("--experiment", Write("e.json", "{ " + ThreeRandom + ", \"rotate\": \"maybe\" }")), "yes/no");
            Assert.ThrowsException<UsageException>(() => Plan("--experiment", Write("f.json",
                "{ \"players\": [\"RandomChoiceAI\", 2, \"RandomChoiceAI\"], \"games\": 1, \"seed\": 1 }")), "a player that isn't text");
        }

        [TestMethod]
        public async Task ARotatedRun_FromAFile_SeatsEveryAgentEverywhereOnTheSameDeals()
        {
            string file = Write("exp.json",
                "{ \"players\": [\"HeuristicAI\", \"RandomChoiceAI\", \"PriorityWonderRandomChoiceAI\"], \"games\": 2, \"seed\": 9, \"rotate\": true, \"log\": \"warning\" }");
            string csv = Path.Combine(_dir, "out", "results.csv");

            int exit = await Commands.Run(CliArgs.Parse(new[] { "run", "--experiment", file, "--out", csv }), Array.Empty<string>());

            Assert.AreEqual(Commands.Ok, exit);
            var rows = ResultsCsvWriter.ReadRows(csv).ToList();
            Assert.AreEqual(2 * 3 * 3, rows.Count);
            foreach (var deal in rows.GroupBy(r => r["dealIndex"]))
            {
                // the deal (each seat's wonder) stays; the agents move round it
                Assert.AreEqual(3, deal.Select(r => r["gameIndex"]).Distinct().Count());
                foreach (var seat in deal.GroupBy(r => r["seat"]))
                {
                    Assert.AreEqual(1, seat.Select(r => r["wonder"] + r["side"]).Distinct().Count());
                    Assert.AreEqual(3, seat.Select(r => r["agent"]).Distinct().Count());
                }
            }
            string runId = rows[0]["runId"];
            Assert.AreEqual(File.ReadAllText(file), File.ReadAllText(Path.Combine(_dir, "out", $"{runId}.experiment.json")));
            RunManifest manifest = RunManifest.Load(Path.Combine(_dir, "out", $"{runId}.run.json"))!;
            Assert.AreEqual(file, manifest.Settings["experiment"]);
            Assert.AreEqual(2 * 3, manifest.GamesCompleted);
        }
    }
}
