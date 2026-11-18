using System.Text.RegularExpressions;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Results;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Randomness.Services;
using _7WondersGameTests.Agents.Mcts;
using Serilog;
using Serilog.Core;
using Serilog.Events;

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

    private static (ILogger Logger, ListSink Sink) Capture(LogEventLevel level)
    {
        var sink = new ListSink();
        ILogger logger = new LoggerConfiguration().MinimumLevel.Is(level).WriteTo.Sink(sink).CreateLogger();
        return (logger, sink);
    }

    private static Game NewGame(ulong seed, ILogger? trace = null)
    {
        var game = new Game(seed)
        {
            Agents = new List<IAgent>
            {
                new HeuristicAI(), new RandomChoiceAI(), new RandomChoiceAI(),
            },
        };
        if (trace != null)
        {
            game.Trace = trace;
        }

        game.InitGame();
        return game;
    }

    [TestMethod]
    public void Tracing_DoesNotChangeTheGame()
    {
        for (ulong i = 0; i < 20; i++)
        {
            ulong seed = Seeding.GameSeed(31, (long)i);
            Game silent = NewGame(seed);
            silent.Loop(record: true);

            var (logger, sink) = Capture(LogEventLevel.Verbose);
            Game traced = NewGame(seed, logger);
            traced.Loop(record: true);

            CollectionAssert.AreEqual(silent.Results, traced.Results, $"seed {seed}");
            Assert.IsTrue(sink.Events.Count > 0, "the traced game wrote a trace");
        }
    }

    [TestMethod]
    public void ASearchersTrace_SaysWhyItChose_AndChangesNothing()
    {
        var (logger, sink) = Capture(LogEventLevel.Debug);
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
    private static Game PlayWithSearcher(ILogger? trace)
    {
        var game = new Game(5)
        {
            Agents = new List<IAgent> { new HeuristicAI(), MctsAgentTests.Mcts(":n=40"), new HeuristicAI() },
        };
        if (trace != null)
        {
            game.Trace = trace;
        }

        game.InitGame();
        game.Loop(record: true);
        return game;
    }

    // the stats column holds search times, which differ between any two runs; the game itself doesn't
    private static string Outcome(Game game)
    {
        IEnumerable<string> seatOutcomes = game.Results!
            .Select(result => $"{result.VictoryPoints} {result.Coins} {result.Rank}");
        return string.Join(" | ", seatOutcomes);
    }

    [TestMethod]
    public void DeepCopy_IsSilent()
    {
        var (logger, _) = Capture(LogEventLevel.Verbose);
        Game game = NewGame(5, logger);

        Game copy = game.DeepCopy();

        Assert.AreSame(Logger.None, copy.Trace);
    }

    [TestMethod]
    public void DebugTrace_HasTheMainSteps()
    {
        var (logger, sink) = Capture(LogEventLevel.Debug);
        Game game = NewGame(Seeding.GameSeed(77, 0), logger);
        game.Loop(record: true);
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
        foreach (SeatResult seat in game.Results!)
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
        Game game = NewGame(3);
        Assert.AreSame(Logger.None, game.Trace);
        game.Loop();
    }
}
