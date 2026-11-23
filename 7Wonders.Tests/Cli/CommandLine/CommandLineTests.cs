using _7Wonders.Agents.Catalog;
using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Agents.Common.Settings;
using _7Wonders.Agents.Matches;
using _7Wonders.Cli.Arguments;
using _7Wonders.Cli.CommandLine;
using _7Wonders.Cli.Experiments;
using _7Wonders.Cli.Logs;
using _7Wonders.Cli.Results;
using _7Wonders.Cli.Sweeps;
using _7Wonders.Tests.TestHelpers;
using Serilog.Events;

namespace _7Wonders.Tests.Cli.CommandLine;

/// <summary>
/// The command line (options, agent text, ranges, log levels).
/// </summary>
[TestClass]
public class CommandLineTests
{
    /// <summary>Adds the test agent with settings to the catalog.</summary>
    [ClassInitialize]
    public static void RegisterTestAgent(TestContext context)
    {
        SettingsTestAgent.Register();
    }

    [TestMethod]
    public void NoArguments_MeansHelp()
    {
        Assert.AreEqual("help", CliArguments.Parse(Array.Empty<string>()).Command);
    }

    [TestMethod]
    public void Options_InBothForms()
    {
        CliArguments cli = CliArguments.Parse(new[] { "RUN", "--games", "5", "--Seed=0x10" });

        Assert.AreEqual("run", cli.Command);
        Assert.AreEqual(5, cli.RequireWholeNumber("games", 1));
        Assert.AreEqual(16UL, cli.GetSeed("seed"));
    }

    [TestMethod]
    public void BadOptions_AreUsageErrors()
    {
        string[] noValue = { "run", "--games" };
        string[] twice = { "run", "--games", "1", "--games", "2" };
        string[] noDashes = { "run", "games" };
        CliArguments unknownOption = CliArguments.Parse(new[] { "run", "--gmaes", "1" });
        CliArguments noGames = CliArguments.Parse(new[] { "run", "--games", "0" });
        CliArguments noPlayers = CliArguments.Parse(new[] { "run" });

        Assert.ThrowsException<UsageException>(() => CliArguments.Parse(noValue));
        Assert.ThrowsException<UsageException>(() => CliArguments.Parse(twice));
        Assert.ThrowsException<UsageException>(() => CliArguments.Parse(noDashes));
        Assert.ThrowsException<UsageException>(() => unknownOption.AllowOnly("games"));
        Assert.ThrowsException<UsageException>(() => noGames.RequireWholeNumber("games", 1));
        Assert.ThrowsException<UsageException>(() => noPlayers.Require("players"));
    }

    [TestMethod]
    public void HelpTopic()
    {
        CliArguments cli = CliArguments.Parse(new[] { "help", "run" });
        Assert.AreEqual("run", cli.Get("topic"));
        StringAssert.Contains(HelpText.For("run"), "--players");
        Assert.IsTrue(CliArguments.Parse(new[] { "replay", "--help" }).HelpRequested);
    }

    [TestMethod]
    public void AgentSpec_Settings()
    {
        List<AgentSpecification> specifications =
            RunPlan.ParsePlayers("HeuristicAgent, RandomAgent ,SettingsTestAgent:Weight=1.5:ITERATIONS=500", "run");

        Assert.AreEqual(3, specifications.Count);
        IAgent agent = AgentCatalog.Create(specifications[2]);
        Assert.IsInstanceOfType(agent, typeof(SettingsTestAgent));
        Assert.AreEqual("{\"Iterations\":500,\"Weight\":1.5}", agent.ConfigurationJson());
    }

    [TestMethod]
    public void AgentSpec_Defaults_AreTheClassDefaults()
    {
        IAgent agent = AgentCatalog.Create(AgentSpecification.Parse("settingstestagent"));
        Assert.AreEqual("{\"Iterations\":100,\"Weight\":1}", agent.ConfigurationJson());
    }

    [TestMethod]
    public void AgentSpec_Mistakes_AreSpecificationErrors()
    {
        AgentSpecification unknownAgent = AgentSpecification.Parse("Nobody");
        AgentSpecification unknownSetting = AgentSpecification.Parse("SettingsTestAgent:x=1");
        AgentSpecification notANumber = AgentSpecification.Parse("SettingsTestAgent:Weight=abc");
        AgentSpecification agentWithoutSettings = AgentSpecification.Parse("RandomAgent:Weight=1");

        Assert.ThrowsException<AgentSpecificationException>(() => AgentCatalog.Create(unknownAgent));
        Assert.ThrowsException<AgentSpecificationException>(() => AgentCatalog.Create(unknownSetting));
        Assert.ThrowsException<AgentSpecificationException>(() => AgentCatalog.Create(notANumber));
        Assert.ThrowsException<AgentSpecificationException>(() => AgentCatalog.Create(agentWithoutSettings));
        Assert.ThrowsException<AgentSpecificationException>(() => AgentSpecification.Parse("SettingsTestAgent:c"));
    }

    [TestMethod]
    public void EveryCatalogAgent_CanBeCreated()
    {
        foreach (AgentCatalogEntry entry in AgentCatalog.Entries)
        {
            IAgent agent = AgentCatalog.Create(AgentSpecification.Parse(entry.Name));
            Assert.AreEqual(entry.Name, agent.Name);
        }
    }

    [TestMethod]
    public void ResultRow_RebuildsTheSameAgent()
    {
        IAgent original = AgentCatalog.Create(AgentSpecification.Parse("SettingsTestAgent:Weight=0.1:iterations=1234"));
        AgentSpecification fromRow = AgentCatalog.FromResultRow(original.Name, original.ConfigurationJson());

        Assert.AreEqual(original.ConfigurationJson(), AgentCatalog.Create(fromRow).ConfigurationJson());
    }

    [TestMethod]
    public void Range_GivesASweep()
    {
        List<AgentSpecification> specifications =
            RunPlan.ParsePlayers("HeuristicAgent,RandomAgent,SettingsTestAgent:Weight=0..2:iterations=7", "run");
        Sweep? sweep = LineUp.Validate(specifications, 5);

        Assert.IsNotNull(sweep);
        Assert.AreEqual(2, sweep.Seat);
        double[] values = Enumerable.Range(0, 5).Select(sweep.ValueAt).ToArray();
        CollectionAssert.AreEqual(new[] { 0, 0.5, 1, 1.5, 2 }, values);
        IAgent sweptAgent = LineUp.CreateAgent(specifications[2], sweep.ValueAt(3));
        StringAssert.Contains(sweptAgent.ConfigurationJson(), "\"Weight\":1.5");
    }

    [TestMethod]
    public void Range_MatchesTheOldCSweepValues()
    {
        // old SearchOptimalCParallel: c = minC + k * ((maxC - minC) / iterations), iterations + 1 values
        double minC = 0.3;
        double maxC = 2.1;
        int iterations = 7;
        Sweep sweep = new(2, "c", minC, maxC, iterations + 1);
        for (int step = 0; step <= iterations; step++)
        {
            Assert.AreEqual(minC + step * ((maxC - minC) / iterations), sweep.ValueAt(step));
        }
    }

    [TestMethod]
    public void Range_Mistakes_AreUsageErrors()
    {
        List<AgentSpecification> oneRange =
            RunPlan.ParsePlayers("HeuristicAgent,RandomAgent,SettingsTestAgent:Weight=0..2", "run");
        List<AgentSpecification> noRange = RunPlan.ParsePlayers("HeuristicAgent,RandomAgent,RandomAgent", "run");
        List<AgentSpecification> twoRanges =
            RunPlan.ParsePlayers("SettingsTestAgent:Weight=0..1,RandomAgent,SettingsTestAgent:Weight=0..2", "run");

        Assert.ThrowsException<UsageException>(() => LineUp.Validate(oneRange, null), "range without --steps");
        Assert.ThrowsException<UsageException>(() => LineUp.Validate(noRange, 3), "--steps without a range");
        Assert.ThrowsException<UsageException>(() => LineUp.Validate(twoRanges, 3), "two ranges");
    }

    [TestMethod]
    public void LogLevel_Precedence()
    {
        string? saved = Environment.GetEnvironmentVariable(Logging.EnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(Logging.EnvironmentVariable, "verbose");
            Assert.AreEqual(LogEventLevel.Verbose, Logging.Resolve(null).Level, "environment variable");
            Assert.AreEqual(LogEventLevel.Information, Logging.Resolve("info").Level, "--log-level wins");
            Assert.AreEqual(LogEventLevel.Debug, Logging.Resolve(null, LogEventLevel.Debug).Level, "replay's default");

            Environment.SetEnvironmentVariable(Logging.EnvironmentVariable, null);
            Assert.AreNotEqual(Logging.EnvironmentVariable, Logging.Resolve(null).Source, "falls back to the build");
            Assert.ThrowsException<UsageException>(() => Logging.Resolve("loud"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(Logging.EnvironmentVariable, saved);
        }
    }

    [TestMethod]
    public void ResultsCsv_RowsReadBack()
    {
        SeatResult seat = new(1, "SomeAgent", "{\"a\":1,\"b\":\"x;y\"}", "Gizah", "B", 50, 3, 1, 0.5);
        string row = ResultsCsvWriter.FormatRow("run1", 4, 2, 99, seat);

        List<string> fields = ResultsCsvWriter.SplitRow(row);

        Assert.AreEqual(ResultsCsvWriter.Columns.Length, fields.Count);
        Assert.AreEqual(seat.ConfigurationJson, fields[Array.IndexOf(ResultsCsvWriter.Columns, "configurationJson")]);
        Assert.AreEqual("50", fields[Array.IndexOf(ResultsCsvWriter.Columns, "victoryPoints")]);
    }
}
