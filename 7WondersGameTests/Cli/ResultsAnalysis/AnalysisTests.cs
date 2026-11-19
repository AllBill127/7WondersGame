using _7WondersGame.Cli.Agents;
using _7WondersGame.Cli.Arguments;
using _7WondersGame.Cli.CommandLine;
using _7WondersGame.Cli.Experiments;
using _7WondersGame.Cli.Results;
using _7WondersGame.Cli.ResultsAnalysis;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Cli.ResultsAnalysis;

/// <summary>The 'analyze' command: Wilson intervals, paired differences, groups.</summary>
[TestClass]
public class AnalysisTests
{
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
        _directory = Path.Combine(Path.GetTempPath(), $"analysis-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Removes the test's folder.</summary>
    [TestCleanup]
    public void RemoveFolder()
    {
        Directory.Delete(_directory, recursive: true);
    }

    // a winner (any win share) is ranked first, everyone else second
    private static ResultRow Row(
        string seed,
        long game,
        int seat,
        string agent,
        int victoryPoints,
        double winShare,
        string configJson = "{}")
    {
        int rank = 2;
        if (winShare > 0)
        {
            rank = 1;
        }

        return new ResultRow("run", game, seed, seat, agent, configJson, "GizahA", victoryPoints, rank, winShare, 3);
    }

    [TestMethod]
    public void Wilson_MatchesTheTextbookValues()
    {
        (double low, double high) = Analysis.Wilson(50, 100);
        Assert.AreEqual(0.4038, low, 1e-4);
        Assert.AreEqual(0.5962, high, 1e-4);

        (low, high) = Analysis.Wilson(0, 10);
        Assert.AreEqual(0, low, 1e-12, "never below 0, unlike p +- 1.96 se");
        Assert.AreEqual(0.2775, high, 1e-4);

        (low, high) = Analysis.Wilson(10, 10);
        Assert.AreEqual(0.7225, low, 1e-4);
        Assert.AreEqual(1, high, 1e-12);
    }

    [TestMethod]
    public void MeanSe_IsTheStandardErrorOfTheMean()
    {
        (double mean, double standardError) = Analysis.MeanAndStandardError(new[] { 1.0, 2, 3, 4 });
        Assert.AreEqual(2.5, mean, 1e-12);
        Assert.AreEqual(Math.Sqrt(5.0 / 3 / 4), standardError, 1e-12);
        Assert.AreEqual((7.0, 0.0), Analysis.MeanAndStandardError(new[] { 7.0 }));
    }

    [TestMethod]
    public void Groups_CountTiesAsShares_AndCompareThePairsDealByDeal()
    {
        // three deals; A beats B by 2, 4 and 6 VP; one tie for first between A and C
        List<ResultRow> rows = new()
        {
            Row("s1", 0, 0, "A", 50, 1), Row("s1", 0, 1, "B", 48, 0), Row("s1", 0, 2, "C", 40, 0),
            Row("s2", 1, 0, "A", 50, 0.5), Row("s2", 1, 1, "B", 46, 0), Row("s2", 1, 2, "C", 50, 0.5),
            Row("s3", 2, 0, "A", 40, 0), Row("s3", 2, 1, "B", 34, 0), Row("s3", 2, 2, "C", 45, 1),
        };

        List<GroupStatistics> groups = Analysis.Groups(rows, GroupBy.Agent);

        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, groups.Select(group => group.Label).ToArray());
        Assert.AreEqual(1.5, groups[0].Wins, 1e-12);
        Assert.AreEqual(0.5, groups[0].WinRate, 1e-12);
        Assert.AreEqual(1.0 / 3, groups[0].FairShare, 1e-12);

        PairStatistics pair = Analysis.Compare(rows, GroupBy.Agent, groups[0], groups[1])!;
        Assert.AreEqual(3, pair.Deals);
        Assert.AreEqual(4, pair.VictoryPointDifference, 1e-12);
        double expectedStandardError = Analysis.MeanAndStandardError(new[] { 2.0, 4, 6 }).StandardError;
        Assert.AreEqual(expectedStandardError, pair.VictoryPointDifferenceStandardError, 1e-12);
        Assert.AreEqual(0.5, pair.WinRateDifference, 1e-12);
        Assert.IsTrue(pair.VictoryPointDifferenceIsSignificant);

        Assert.AreEqual(3, Analysis.Pairs(rows, GroupBy.Agent, groups, null).Count);
        List<PairStatistics> vsB = Analysis.Pairs(rows, GroupBy.Agent, groups, 2);
        string[] pairLabels = vsB.Select(pair => $"{pair.GroupA.Label}-{pair.GroupB.Label}").ToArray();
        CollectionAssert.AreEqual(new[] { "A-B", "C-B" }, pairLabels);
    }

    [TestMethod]
    public void Compare_UsesOnlyTheDealsBothPlayed_AveragingRepeats()
    {
        // a sweep: value x plays deals s1, s2; value y plays s2 (twice, e.g. two seatings) and s3
        List<ResultRow> rows = new()
        {
            Row("s1", 0, 0, "x", 50, 1), Row("s2", 1, 0, "x", 40, 0),
            Row("s2", 2, 0, "y", 30, 0), Row("s2", 3, 1, "y", 36, 0), Row("s3", 4, 0, "y", 60, 1),
        };
        List<GroupStatistics> groups = Analysis.Groups(rows, GroupBy.Agent);

        PairStatistics pair = Analysis.Compare(rows, GroupBy.Agent, groups[0], groups[1])!;

        Assert.AreEqual(1, pair.Deals);
        Assert.AreEqual(40 - 33, pair.VictoryPointDifference, 1e-12);
        Assert.IsFalse(pair.VictoryPointDifferenceIsSignificant, "one deal tells nothing about chance");
    }

    [TestMethod]
    public void PlayerLabels_NameTheSettingsThatDiffer_AndKeepTheSweptAgentApart()
    {
        Dictionary<string, string> labels = Analysis.PlayerLabels(new[]
        {
            ("k1", "MctsAgent", "{\"ExplorationConstant\":0.3,\"IterationBudget\":1000}", true),
            ("k2", "MctsAgent", "{\"ExplorationConstant\":1.2,\"IterationBudget\":1000}", true),
            ("k3", "HeuristicAgent", "{\"CoinValueInVictoryPoints\":0.75}", false),
            ("k4", "HeuristicAgent", "{\"CoinValueInVictoryPoints\":0.75}", true),
            ("k5", "HeuristicAgent", "{\"CoinValueInVictoryPoints\":0.25}", true),
        });

        Assert.AreEqual("MctsAgent ExplorationConstant=0.3", labels["k1"]);
        Assert.AreEqual("MctsAgent ExplorationConstant=1.2", labels["k2"]);
        Assert.AreEqual("HeuristicAgent", labels["k3"], "the opponents, named apart from the swept values");
        Assert.AreEqual("HeuristicAgent CoinValueInVictoryPoints=0.75", labels["k4"]);
        Assert.AreEqual("HeuristicAgent CoinValueInVictoryPoints=0.25", labels["k5"]);
    }

    [TestMethod]
    public void SweptSeat_FollowsTheRotation_AsTheRunPlansIt()
    {
        RunPlan plan = RunPlan.FromCli(CliArguments.Parse(new[]
        {
            "run", "--players", "RandomAgent,SettingsTestAgent:Weight=0..1,RandomAgent,RandomAgent",
            "--steps", "3", "--games", "2", "--seed", "1", "--rotate", "yes",
        }));
        SweptSeat swept = new(plan.Sweep!.Seat, plan.Games, plan.Rotations);

        for (int gameIndex = 0; gameIndex < plan.Total; gameIndex++)
        {
            List<AgentSpecification> seats = plan.SeatsFor(plan.At(gameIndex).Rotation);
            int seat = swept.SeatIn(gameIndex, seats.Count);
            Assert.AreEqual("SettingsTestAgent", seats[seat].Name, $"game {gameIndex}");
            Assert.AreEqual(1, seats.Count(specification => specification.Name == "SettingsTestAgent"));
        }
    }

    [TestMethod]
    public async Task Analyze_ASweepRun_GroupsTheValuesAndWritesTheTables()
    {
        string csv = Path.Combine(_directory, "results.csv");
        int exit = await Commands.Run(CliArguments.Parse(new[]
        {
            "run", "--players", "SettingsTestAgent:Weight=0..1,SettingsTestAgent,RandomAgent", "--steps", "2",
            "--games", "3", "--seed", "5", "--rotate", "yes", "--output", csv, "--log-level", "warning",
        }), Array.Empty<string>());
        Assert.AreEqual(Commands.Ok, exit);

        exit = Commands.Analyze(
            CliArguments.Parse(new[]
            {
                "analyze", "--from", csv, "--output", "summary.csv", "--log-level", "warning",
            }));

        Assert.AreEqual(Commands.Ok, exit);
        List<Dictionary<string, string>> groups =
            ResultsCsvWriter.ReadRows(Path.Combine(_directory, "summary.csv")).ToList();
        // the swept agent at c=0 and c=1 (C defaults to 1, so c=1 equals the opponent's settings), the opponent,
        // RandomAgent
        CollectionAssert.AreEquivalent(
            new[] { "SettingsTestAgent Weight=0", "SettingsTestAgent Weight=1", "SettingsTestAgent", "RandomAgent" },
            groups.Select(group => group["label"]).ToArray());
        Assert.IsTrue(
            groups.All(group => group["games"] == ExpectedGamesOf(group["label"])),
            "each value: 3 deals x 3 seatings; the others play in both values' games");
        List<Dictionary<string, string>> pairs =
            ResultsCsvWriter.ReadRows(Path.Combine(_directory, "summary.pairs.csv")).ToList();
        Assert.AreEqual(6, pairs.Count);
        Assert.IsTrue(pairs.All(pair => pair["deals"] == "3"));
    }

    // a swept value plays 3 deals in 3 seatings; the other players play in both values' games
    private static string ExpectedGamesOf(string label)
    {
        if (label.StartsWith("SettingsTestAgent Weight="))
        {
            return "9";
        }

        return "18";
    }

    [TestMethod]
    public void Analyze_AFileWithSeveralRuns_NeedsRun()
    {
        string csv = Path.Combine(_directory, "two.csv");
        File.WriteAllLines(csv, new[]
        {
            ResultsCsvWriter.Header,
            "r1;0;0;1;0;A;{};GizahA;A;50;3;1;1;{}",
            "r1;0;0;1;1;B;{};RhodosA;A;40;3;2;0;{}",
            "r1;0;0;1;2;B;{};EphesosA;A;30;3;3;0;{}",
            "r2;0;0;1;0;A;{};GizahA;A;50;3;1;1;{}",
            "r2;0;0;1;1;B;{};RhodosA;A;40;3;2;0;{}",
            "r2;0;0;1;2;B;{};EphesosA;A;30;3;3;0;{}",
        });

        CliArguments noRun = CliArguments.Parse(new[] { "analyze", "--from", csv });
        CliArguments unknownRun = CliArguments.Parse(new[] { "analyze", "--from", csv, "--run", "r3" });
        CliArguments unknownGrouping =
            CliArguments.Parse(new[] { "analyze", "--from", csv, "--run", "r1", "--group-by", "x" });
        CliArguments oneRun = CliArguments.Parse(new[] { "analyze", "--from", csv, "--run", "r1" });
        CliArguments allRunsBySeat =
            CliArguments.Parse(new[] { "analyze", "--from", csv, "--run", "all", "--group-by", "seat" });

        Assert.ThrowsException<UsageException>(() => Commands.Analyze(noRun));
        Assert.ThrowsException<UsageException>(() => Commands.Analyze(unknownRun));
        Assert.ThrowsException<UsageException>(() => Commands.Analyze(unknownGrouping));
        Assert.AreEqual(Commands.Ok, Commands.Analyze(oneRun));
        Assert.AreEqual(Commands.Ok, Commands.Analyze(allRunsBySeat));
    }
}
