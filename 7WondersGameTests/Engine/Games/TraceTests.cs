using System.Text.RegularExpressions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Match = _7WondersGame.Cli.Matches.Match;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Results;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Randomness.Services;
using _7WondersGameTests.Agents.Mcts;

namespace _7WondersGameTests.Engine.Games;

/// <summary>
/// The game trace (Debug/Verbose) only reads state, and only real games write it.
/// </summary>
[TestClass]
public class TraceTests
{
    private const string ScoreBreakdownPattern =
        @"(military|coins|wonder|civilian|commercial|guilds|science) (-?\d+)";

    private const string SearchNotePattern =
        @"^Seat 1 MctsAgent: 40 iterations in \d+ ms; \w+ .+ \d+ visits, reward [\d.]+, wins \d+%";

    /// <summary>A logger that writes events of <paramref name="level"/> and up to <paramref name="sink"/>.</summary>
    private static ILogger LoggerInto(ListSink sink, LogEventLevel level)
    {
        return new LoggerConfiguration().MinimumLevel.Is(level).WriteTo.Sink(sink).CreateLogger();
    }

    private static Match NewMatch(ulong seed, ILogger? trace = null)
    {
        Game game = new(seed);
        if (trace != null)
        {
            game.Trace = trace;
        }

        game.SetUpRandomGame(3);
        List<IAgent> agents = new() { new HeuristicAgent(), new RandomAgent(), new RandomAgent() };
        return new Match(game, agents);
    }

    [TestMethod]
    public void Tracing_DoesNotChangeTheGame()
    {
        for (ulong gameIndex = 0; gameIndex < 20; gameIndex++)
        {
            ulong seed = Seeding.GameSeed(31, (long)gameIndex);
            List<SeatResult> silent = NewMatch(seed).Play();

            ListSink sink = new();
            ILogger logger = LoggerInto(sink, LogEventLevel.Verbose);
            List<SeatResult> traced = NewMatch(seed, logger).Play();

            CollectionAssert.AreEqual(silent, traced, $"seed {seed}");
            Assert.IsTrue(sink.Events.Count > 0, "the traced game wrote a trace");
        }
    }

    [TestMethod]
    public void ASearchersTrace_SaysWhyItChose_AndChangesNothing()
    {
        ListSink sink = new();
        ILogger logger = LoggerInto(sink, LogEventLevel.Debug);
        Assert.AreEqual(Outcome(PlayWithSearcher(null)), Outcome(PlayWithSearcher(logger)));

        List<string> notes = sink.Messages.Where(message => message.StartsWith("Seat 1 MctsAgent: ")).ToList();
        Assert.IsTrue(notes.Count >= 15, string.Join(" / ", notes));
        StringAssert.Matches(notes[0], new Regex(SearchNotePattern));
        Assert.IsTrue(
            sink.Messages
                .Where(message => message.Contains(" iterations in "))
                .All(message => message.StartsWith("Seat 1 ")),
            "agents without a note write none");
    }

    // a game with an MctsAgent at seat 1, traced if a logger is given
    private static List<SeatResult> PlayWithSearcher(ILogger? trace)
    {
        Game game = new(5);
        if (trace != null)
        {
            game.Trace = trace;
        }

        game.SetUpRandomGame(3);
        List<IAgent> agents = new()
        {
            new HeuristicAgent(),
            MctsAgentTests.Mcts(":IterationBudget=40"),
            new HeuristicAgent(),
        };
        return new Match(game, agents).Play();
    }

    // the stats column holds search times, which differ between any two runs; the game itself doesn't
    private static string Outcome(List<SeatResult> results)
    {
        IEnumerable<string> seatOutcomes = results
            .Select(result => $"{result.VictoryPoints} {result.Coins} {result.Rank}");
        return string.Join(" | ", seatOutcomes);
    }

    [TestMethod]
    public void DeepCopy_IsSilent()
    {
        ILogger logger = LoggerInto(new ListSink(), LogEventLevel.Verbose);
        Game game = NewMatch(5, logger).Game;

        Game copy = game.DeepCopy();

        Assert.AreSame(Logger.None, copy.Trace);
    }

    [TestMethod]
    public void DebugTrace_HasTheMainSteps()
    {
        ListSink sink = new();
        ILogger logger = LoggerInto(sink, LogEventLevel.Debug);
        List<SeatResult> results = NewMatch(Seeding.GameSeed(77, 0), logger).Play();
        List<string> lines = sink.Messages.ToList();

        Assert.AreEqual(1, lines.Count(line => line.StartsWith("Game seed")));
        Assert.AreEqual(
            3,
            lines.Count(line => line.Contains(" on ") && line.StartsWith("Seat")),
            "one setup line per seat");
        Assert.AreEqual(3 * 3, lines.Count(line => line.Contains("battles with")), "each seat's battles, each era");
        Assert.AreEqual(3, lines.Count(line => line.StartsWith("End of era")));
        Assert.IsTrue(lines.Any(line => line.StartsWith("Hands passed west")));
        Assert.IsTrue(lines.Any(line => line.StartsWith("Hands passed east")));
        Assert.IsTrue(lines.Count(line => line.Contains(" chooses ")) >= 18 * 3, "a choice per seat per turn");
        Assert.AreEqual(
            0,
            sink.Events.Count(logEvent => logEvent.Level == LogEventLevel.Verbose),
            "Debug hides Verbose");

        // the score breakdown adds up to the final VP
        foreach (SeatResult seat in results)
        {
            string line = ScoreLineOf(seat, lines);
            int total = int.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
            int sum = Regex.Matches(line, ScoreBreakdownPattern).Sum(match => int.Parse(match.Groups[2].Value));
            Assert.AreEqual(seat.VictoryPoints, total);
            Assert.AreEqual(total, sum, line);
        }
    }

    private static string ScoreLineOf(SeatResult seat, List<string> lines)
    {
        string seatPrefix = $"Seat {seat.Seat} ";
        return lines.Single(traceLine => traceLine.StartsWith(seatPrefix) && traceLine.Contains(" VP = military"));
    }

    [TestMethod]
    public void UntracedGame_WritesNothing()
    {
        Match match = NewMatch(3);
        Assert.AreSame(Logger.None, match.Game.Trace);
        match.Play();
    }
}
