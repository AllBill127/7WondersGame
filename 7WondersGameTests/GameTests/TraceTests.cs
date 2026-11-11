using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace _7WondersGameTests.GameTests
{
    /// <summary>
    /// Step 3b: the game trace (Debug/Verbose) only reads state, and only real games write it.
    /// </summary>
    [TestClass]
    public class TraceTests
    {
        private sealed class ListSink : ILogEventSink
        {
            public readonly List<LogEvent> Events = new();
            public void Emit(LogEvent logEvent) => Events.Add(logEvent);
            // rendered like the console ({Message:lj}): strings without quotes
            private static readonly Serilog.Formatting.Display.MessageTemplateTextFormatter Format = new("{Message:lj}");
            public IEnumerable<string> Messages => Events.Select(e =>
            {
                var w = new StringWriter();
                Format.Format(e, w);
                return w.ToString();
            });
        }

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
                    new DeterministicAI(), new RandomChoiceAI(), new PriorityRandomChoiceAI(ActionPriority.Wonder),
                },
            };
            if (trace != null)
                game.Trace = trace;
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
            Game Play(ILogger? trace)
            {
                var game = new Game(5)
                {
                    Agents = new List<IAgent> { new HeuristicAI(), MctsAgentTests.Mcts(":n=40"), new HeuristicAI() },
                };
                if (trace != null)
                    game.Trace = trace;
                game.InitGame();
                game.Loop(record: true);
                return game;
            }

            var (logger, sink) = Capture(LogEventLevel.Debug);
            // the stats column holds search times, which differ between any two runs; the game itself doesn't
            static string Outcome(Game g) => string.Join(" | ", g.Results!.Select(r => $"{r.VictoryPoints} {r.Coins} {r.Rank}"));
            Assert.AreEqual(Outcome(Play(null)), Outcome(Play(logger)));

            List<string> notes = sink.Messages.Where(m => m.StartsWith("Seat 1 MctsAgent: ")).ToList();
            Assert.IsTrue(notes.Count >= 15, string.Join(" / ", notes));
            StringAssert.Matches(notes[0], new System.Text.RegularExpressions.Regex(
                @"^Seat 1 MctsAgent: 40 iterations in \d+ ms; \w+ .+ \d+ visits, reward [\d.]+, wins \d+%"));
            Assert.IsTrue(sink.Messages.Where(m => m.Contains(" iterations in ")).All(m => m.StartsWith("Seat 1 ")),
                "agents without a note write none");
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

            Assert.AreEqual(1, lines.Count(l => l.StartsWith("Game seed")));
            Assert.AreEqual(3, lines.Count(l => l.Contains(" on ") && l.StartsWith("Seat")), "one setup line per seat");
            Assert.AreEqual(3 * 3, lines.Count(l => l.Contains("battles with")), "each seat's battles, each era");
            Assert.AreEqual(3, lines.Count(l => l.StartsWith("End of era")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Hands passed west")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Hands passed east")));
            Assert.IsTrue(lines.Count(l => l.Contains(" chooses ")) >= 18 * 3, "a choice per seat per turn");
            Assert.AreEqual(0, sink.Events.Count(e => e.Level == LogEventLevel.Verbose), "Debug hides Verbose");

            // the score breakdown adds up to the final VP
            foreach (SeatResult seat in game.Results!)
            {
                string line = lines.Single(l => l.StartsWith($"Seat {seat.Seat} ") && l.Contains(" VP = military"));
                int total = int.Parse(line.Split(':')[1].Trim().Split(' ')[0]);
                int sum = System.Text.RegularExpressions.Regex.Matches(line, @"(military|coins|wonder|civilian|commercial|guilds|science) (-?\d+)")
                    .Sum(m => int.Parse(m.Groups[2].Value));
                Assert.AreEqual(seat.VictoryPoints, total);
                Assert.AreEqual(total, sum, line);
            }
        }

        [TestMethod]
        public void UntracedGame_WritesNothing()
        {
            Game game = NewGame(3);
            Assert.AreSame(Logger.None, game.Trace);
            game.Loop();
        }
    }
}
