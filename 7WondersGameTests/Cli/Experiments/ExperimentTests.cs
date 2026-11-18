using _7WondersGame.Cli.Arguments;
using _7WondersGame.Cli.CommandLine;
using _7WondersGame.Cli.Experiments;
using _7WondersGame.Cli.Results;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Cli.Experiments;

/// <summary>Seat rotation and experiment files.</summary>
[TestClass]
public class ExperimentTests
{
    private const string ThreeRandom =
        "\"players\": \"RandomChoiceAI,RandomChoiceAI,RandomChoiceAI\", \"games\": 1, \"seed\": 1";

    private string _directory = "";

    /// <summary>Adds the test agent with settings to the catalog.</summary>
    [ClassInitialize]
    public static void RegisterTestAgent(TestContext context)
    {
        SettingsTestAgent.Register();
    }

    /// <summary>Makes an empty folder for the test's files.</summary>
    [TestInitialize]
    public void MakeFolder()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"experiment-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Removes the test's folder.</summary>
    [TestCleanup]
    public void RemoveFolder()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string Write(string name, string text)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, text);
        return path;
    }

    private static RunPlan Plan(params string[] args)
    {
        return RunPlan.FromCli(CliArgs.Parse(args.Prepend("run").ToArray()));
    }

    [TestMethod]
    public void Rotation_PlaysEveryDealInEverySeating()
    {
        RunPlan plan = Plan(
            "--players", "RandomChoiceAI,HeuristicAI,MctsAgent",
            "--games", "2",
            "--seed", "1",
            "--rotate", "yes");

        Assert.AreEqual(6, plan.Total);
        int[] deals = Enumerable.Range(0, 6).Select(gameIndex => plan.At(gameIndex).Deal).ToArray();
        int[] rotations = Enumerable.Range(0, 6).Select(gameIndex => plan.At(gameIndex).Rotation).ToArray();
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 1, 1 }, deals);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 0, 1, 2 }, rotations);
        Assert.AreEqual("RandomChoiceAI,HeuristicAI,MctsAgent", string.Join(",", plan.SeatsFor(0)));
        Assert.AreEqual(
            "MctsAgent,RandomChoiceAI,HeuristicAI",
            string.Join(",", plan.SeatsFor(1)),
            "the first listed agent moves to seat 1");
        Assert.AreEqual("HeuristicAI,MctsAgent,RandomChoiceAI", string.Join(",", plan.SeatsFor(2)));
    }

    [TestMethod]
    public void WithoutRotation_GamesAreNumberedAsBefore()
    {
        RunPlan plan = Plan(
            "--players", "SettingsTestAgent:c=0..1,RandomChoiceAI,RandomChoiceAI",
            "--steps", "3",
            "--games", "4",
            "--seed", "1");

        Assert.AreEqual(12, plan.Total);
        for (int gameIndex = 0; gameIndex < 12; gameIndex++)
        {
            GameSlot slot = plan.At(gameIndex);
            Assert.AreEqual((gameIndex / 4, gameIndex % 4, 0), (slot.Step, slot.Deal, slot.Rotation));
            Assert.AreEqual(plan.Sweep!.ValueAt(gameIndex / 4), slot.Value);
        }
    }

    [TestMethod]
    public void RotationAndASweep_Together()
    {
        RunPlan plan = Plan(
            "--players", "SettingsTestAgent:c=0..1,RandomChoiceAI,RandomChoiceAI,RandomChoiceAI",
            "--steps", "2",
            "--games", "3",
            "--seed", "1",
            "--rotate", "true");

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
        RunPlan fromCli = Plan(
            "--players", "SettingsTestAgent:c=0.3..2,RandomChoiceAI,RandomChoiceAI",
            "--games", "50",
            "--seed", "16",
            "--steps", "6",
            "--rotate", "yes");

        Assert.AreEqual(string.Join(",", fromCli.Players), string.Join(",", fromFile.Players));
        Assert.AreEqual(
            (fromCli.Games, fromCli.Seed, fromCli.Sweep, fromCli.Rotate),
            (fromFile.Games, fromFile.Seed, fromFile.Sweep, fromFile.Rotate));
        Assert.AreEqual(8, fromFile.Parallel, "the command line wins for --parallel");
        Assert.AreEqual(Path.Combine(_directory, "deals.txt"), fromFile.Dataset, "a dataset next to the file");
        Assert.AreEqual("C sweep", fromFile.Description);
        Assert.AreEqual(file, fromFile.Experiment);
    }

    [TestMethod]
    public void ExperimentFile_Mistakes_AreUsageErrors()
    {
        string ok = Write("ok.json", "{ " + ThreeRandom + " }");
        Assert.AreEqual(3, Plan("--experiment", ok).Players.Count);

        string missing = Path.Combine(_directory, "missing.json");
        string notJson = Write("a.json", "{ \"players\": ");
        string notAnObject = Write("b.json", "[1]");
        string unknownKey = Write("c.json", "{ " + ThreeRandom + ", \"gmaes\": 2 }");
        string keyTwice = Write("d.json", "{ " + ThreeRandom + ", \"Games\": 2 }");
        string notYesOrNo = Write("e.json", "{ " + ThreeRandom + ", \"rotate\": \"maybe\" }");
        string playerNotText = Write(
            "f.json",
            "{ \"players\": [\"RandomChoiceAI\", 2, \"RandomChoiceAI\"], \"games\": 1, \"seed\": 1 }");

        Assert.ThrowsException<UsageException>(
            () => Plan("--experiment", ok, "--games", "5"),
            "the file sets the games");
        Assert.ThrowsException<UsageException>(() => Plan("--experiment", missing), "missing");
        Assert.ThrowsException<UsageException>(() => Plan("--experiment", notJson), "not JSON");
        Assert.ThrowsException<UsageException>(() => Plan("--experiment", notAnObject), "not an object");
        Assert.ThrowsException<UsageException>(() => Plan("--experiment", unknownKey), "unknown key");
        Assert.ThrowsException<UsageException>(() => Plan("--experiment", keyTwice), "twice");
        Assert.ThrowsException<UsageException>(() => Plan("--experiment", notYesOrNo), "yes/no");
        Assert.ThrowsException<UsageException>(() => Plan("--experiment", playerNotText), "a player that isn't text");
    }

    [TestMethod]
    public async Task ARotatedRun_FromAFile_SeatsEveryAgentEverywhereOnTheSameDeals()
    {
        string file = Write(
            "exp.json",
            "{ \"players\": [\"HeuristicAI\", \"RandomChoiceAI\", \"MctsAgent:Iterations=10\"], " +
            "\"games\": 2, \"seed\": 9, \"rotate\": true, \"log\": \"warning\" }");
        string csv = Path.Combine(_directory, "out", "results.csv");

        CliArgs cli = CliArgs.Parse(new[] { "run", "--experiment", file, "--out", csv });
        int exit = await Commands.Run(cli, Array.Empty<string>());

        Assert.AreEqual(Commands.Ok, exit);
        var rows = ResultsCsvWriter.ReadRows(csv).ToList();
        Assert.AreEqual(2 * 3 * 3, rows.Count);
        foreach (var deal in rows.GroupBy(row => row["dealIndex"]))
        {
            // the deal (each seat's wonder) stays; the agents move round it
            Assert.AreEqual(3, deal.Select(row => row["gameIndex"]).Distinct().Count());
            foreach (var seat in deal.GroupBy(row => row["seat"]))
            {
                Assert.AreEqual(1, seat.Select(row => row["wonder"] + row["side"]).Distinct().Count());
                Assert.AreEqual(3, seat.Select(row => row["agent"]).Distinct().Count());
            }
        }

        string runId = rows[0]["runId"];
        string copiedFile = Path.Combine(_directory, "out", $"{runId}.experiment.json");
        Assert.AreEqual(File.ReadAllText(file), File.ReadAllText(copiedFile));
        RunManifest manifest = RunManifest.Load(Path.Combine(_directory, "out", $"{runId}.run.json"))!;
        Assert.AreEqual(file, manifest.Settings["experiment"]);
        Assert.AreEqual(2 * 3, manifest.GamesCompleted);
        Assert.AreEqual(Environment.ProcessorCount, manifest.Machine.LogicalCores, "where it ran");
        Assert.IsFalse(string.IsNullOrEmpty(manifest.Machine.Cpu));
        StringAssert.Contains(manifest.Machine.Runtime, ".NET");
    }
}
