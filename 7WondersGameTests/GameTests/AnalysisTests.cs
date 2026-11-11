using _7WondersGame.src.cli;
using _7WondersGame.src.models;

namespace _7WondersGameTests.GameTests
{
    /// <summary>The 'analyze' command (step 9 tooling): Wilson intervals, paired differences, groups.</summary>
    [TestClass]
    public class AnalysisTests
    {
        [ClassInitialize]
        public static void RegisterTestAgent(TestContext _) => SettingsTestAgent.Register();

        private string _dir = "";

        [TestInitialize]
        public void MakeFolder() => Directory.CreateDirectory(_dir = Path.Combine(Path.GetTempPath(), $"analysis-{Guid.NewGuid():N}"));

        [TestCleanup]
        public void RemoveFolder() => Directory.Delete(_dir, recursive: true);

        private static ResultRow Row(string seed, long game, int seat, string agent, int vp, double winShare, string config = "{}") =>
            new("run", game, seed, seat, agent, config, "GizahA", vp, winShare > 0 ? 1 : 2, winShare, 3);

        [TestMethod]
        public void Wilson_MatchesTheTextbookValues()
        {
            var (low, high) = Analysis.Wilson(50, 100);
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
            var (mean, se) = Analysis.MeanSe(new[] { 1.0, 2, 3, 4 });
            Assert.AreEqual(2.5, mean, 1e-12);
            Assert.AreEqual(Math.Sqrt(5.0 / 3 / 4), se, 1e-12);
            Assert.AreEqual((7.0, 0.0), Analysis.MeanSe(new[] { 7.0 }));
        }

        [TestMethod]
        public void Groups_CountTiesAsShares_AndCompareThePairsDealByDeal()
        {
            // three deals; A beats B by 2, 4 and 6 VP; one tie for first between A and C
            var rows = new List<ResultRow>
            {
                Row("s1", 0, 0, "A", 50, 1), Row("s1", 0, 1, "B", 48, 0), Row("s1", 0, 2, "C", 40, 0),
                Row("s2", 1, 0, "A", 50, 0.5), Row("s2", 1, 1, "B", 46, 0), Row("s2", 1, 2, "C", 50, 0.5),
                Row("s3", 2, 0, "A", 40, 0), Row("s3", 2, 1, "B", 34, 0), Row("s3", 2, 2, "C", 45, 1),
            };

            List<GroupStats> groups = Analysis.Groups(rows, GroupBy.Agent);

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, groups.Select(g => g.Label).ToArray());
            Assert.AreEqual(1.5, groups[0].Wins, 1e-12);
            Assert.AreEqual(0.5, groups[0].WinRate, 1e-12);
            Assert.AreEqual(1.0 / 3, groups[0].FairShare, 1e-12);

            PairStats ab = Analysis.Compare(rows, GroupBy.Agent, groups[0], groups[1])!;
            Assert.AreEqual(3, ab.Deals);
            Assert.AreEqual(4, ab.VpDiff, 1e-12);
            Assert.AreEqual(Analysis.MeanSe(new[] { 2.0, 4, 6 }).Se, ab.VpDiffSe, 1e-12);
            Assert.AreEqual(0.5, ab.WinDiff, 1e-12);
            Assert.IsTrue(ab.VpDiffers);

            Assert.AreEqual(3, Analysis.Pairs(rows, GroupBy.Agent, groups, null).Count);
            List<PairStats> vsB = Analysis.Pairs(rows, GroupBy.Agent, groups, 2);
            CollectionAssert.AreEqual(new[] { "A-B", "C-B" }, vsB.Select(p => $"{p.A.Label}-{p.B.Label}").ToArray());
        }

        [TestMethod]
        public void Compare_UsesOnlyTheDealsBothPlayed_AveragingRepeats()
        {
            // a sweep: value x plays deals s1, s2; value y plays s2 (twice, e.g. two seatings) and s3
            var rows = new List<ResultRow>
            {
                Row("s1", 0, 0, "x", 50, 1), Row("s2", 1, 0, "x", 40, 0),
                Row("s2", 2, 0, "y", 30, 0), Row("s2", 3, 1, "y", 36, 0), Row("s3", 4, 0, "y", 60, 1),
            };
            List<GroupStats> groups = Analysis.Groups(rows, GroupBy.Agent);

            PairStats xy = Analysis.Compare(rows, GroupBy.Agent, groups[0], groups[1])!;

            Assert.AreEqual(1, xy.Deals);
            Assert.AreEqual(40 - 33, xy.VpDiff, 1e-12);
            Assert.IsFalse(xy.VpDiffers, "one deal tells nothing about chance");
        }

        [TestMethod]
        public void PlayerLabels_NameTheSettingsThatDiffer_AndKeepTheSweptAgentApart()
        {
            var labels = Analysis.PlayerLabels(new[]
            {
                ("k1", "MctsAgent", "{\"C\":0.3,\"Iterations\":1000}", true),
                ("k2", "MctsAgent", "{\"C\":1.2,\"Iterations\":1000}", true),
                ("k3", "HeuristicAI", "{\"CoinValue\":0.75}", false),
                ("k4", "HeuristicAI", "{\"CoinValue\":0.75}", true),
                ("k5", "HeuristicAI", "{\"CoinValue\":0.25}", true),
            });

            Assert.AreEqual("MctsAgent C=0.3", labels["k1"]);
            Assert.AreEqual("MctsAgent C=1.2", labels["k2"]);
            Assert.AreEqual("HeuristicAI", labels["k3"], "the opponents, named apart from the swept values");
            Assert.AreEqual("HeuristicAI CoinValue=0.75", labels["k4"]);
            Assert.AreEqual("HeuristicAI CoinValue=0.25", labels["k5"]);
        }

        [TestMethod]
        public void SweptSeat_FollowsTheRotation_AsTheRunPlansIt()
        {
            RunPlan plan = RunPlan.FromCli(CliArgs.Parse(new[]
            {
                "run", "--players", "RandomChoiceAI,SettingsTestAgent:c=0..1,RandomChoiceAI,RandomChoiceAI",
                "--steps", "3", "--games", "2", "--seed", "1", "--rotate", "yes",
            }));
            var swept = new SweptSeat(plan.Sweep!.Seat, plan.Games, plan.Rotations);

            for (int i = 0; i < plan.Total; i++)
            {
                List<AgentSpec> seats = plan.SeatsFor(plan.At(i).Rotation);
                int seat = swept.SeatIn(i, seats.Count);
                Assert.AreEqual("SettingsTestAgent", seats[seat].Name, $"game {i}");
                Assert.AreEqual(1, seats.Count(s => s.Name == "SettingsTestAgent"));
            }
        }

        [TestMethod]
        public async Task Analyze_ASweepRun_GroupsTheValuesAndWritesTheTables()
        {
            string csv = Path.Combine(_dir, "results.csv");
            int exit = await Commands.Run(CliArgs.Parse(new[]
            {
                "run", "--players", "SettingsTestAgent:c=0..1,SettingsTestAgent,RandomChoiceAI", "--steps", "2",
                "--games", "3", "--seed", "5", "--rotate", "yes", "--out", csv, "--log", "warning",
            }), Array.Empty<string>());
            Assert.AreEqual(Commands.Ok, exit);

            exit = Commands.Analyze(CliArgs.Parse(new[] { "analyze", "--from", csv, "--out", "summary.csv", "--log", "warning" }));

            Assert.AreEqual(Commands.Ok, exit);
            var groups = ResultsCsvWriter.ReadRows(Path.Combine(_dir, "summary.csv")).ToList();
            // the swept agent at c=0 and c=1 (C defaults to 1, so c=1 equals the opponent's settings), the opponent, RandomChoiceAI
            CollectionAssert.AreEquivalent(new[] { "SettingsTestAgent C=0", "SettingsTestAgent C=1", "SettingsTestAgent", "RandomChoiceAI" },
                groups.Select(g => g["label"]).ToArray());
            Assert.IsTrue(groups.All(g => g["games"] == (g["label"].StartsWith("SettingsTestAgent C=") ? "9" : "18")),
                "each value: 3 deals x 3 seatings; the others play in both values' games");
            var pairs = ResultsCsvWriter.ReadRows(Path.Combine(_dir, "summary.pairs.csv")).ToList();
            Assert.AreEqual(6, pairs.Count);
            Assert.IsTrue(pairs.All(p => p["deals"] == "3"));
        }

        [TestMethod]
        public void Analyze_AFileWithSeveralRuns_NeedsRun()
        {
            string csv = Path.Combine(_dir, "two.csv");
            File.WriteAllLines(csv, new[]
            {
                ResultsCsvWriter.Header,
                "r1,0,0,1,0,A,{},GizahA,A,50,3,1,1,{}", "r1,0,0,1,1,B,{},RhodosA,A,40,3,2,0,{}", "r1,0,0,1,2,B,{},EphesosA,A,30,3,3,0,{}",
                "r2,0,0,1,0,A,{},GizahA,A,50,3,1,1,{}", "r2,0,0,1,1,B,{},RhodosA,A,40,3,2,0,{}", "r2,0,0,1,2,B,{},EphesosA,A,30,3,3,0,{}",
            });

            Assert.ThrowsException<UsageException>(() => Commands.Analyze(CliArgs.Parse(new[] { "analyze", "--from", csv })));
            Assert.ThrowsException<UsageException>(() => Commands.Analyze(CliArgs.Parse(new[] { "analyze", "--from", csv, "--run", "r3" })));
            Assert.ThrowsException<UsageException>(() => Commands.Analyze(CliArgs.Parse(new[] { "analyze", "--from", csv, "--run", "r1", "--by", "x" })));
            Assert.AreEqual(Commands.Ok, Commands.Analyze(CliArgs.Parse(new[] { "analyze", "--from", csv, "--run", "r1" })));
            Assert.AreEqual(Commands.Ok, Commands.Analyze(CliArgs.Parse(new[] { "analyze", "--from", csv, "--run", "all", "--by", "seat" })));
        }
    }
}
