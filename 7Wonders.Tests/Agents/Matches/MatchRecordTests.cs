using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Agents.Heuristic;
using _7Wonders.Agents.Matches;
using _7Wonders.Agents.Matches.Records;
using _7Wonders.Agents.RandomChoice;
using _7Wonders.Cli.Logs;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Games.Enums;
using _7Wonders.Engine.Games.Events;
using _7Wonders.Engine.Moves.Dtos;
using _7Wonders.Engine.Randomness.Services;
using _7Wonders.Tests.Engine.Games;
using _7Wonders.Tests.TestHelpers;
using Serilog;
using Serilog.Events;
using Match = _7Wonders.Agents.Matches.Match;

namespace _7Wonders.Tests.Agents.Matches;

/// <summary>
/// A full match records its setup, every decision and what the rules did, in order; a results-only match records
/// nothing but the results.
/// </summary>
[TestClass]
public class MatchRecordTests
{
    private const int GameCount = 40;

    private static Game NewGame(ulong seed, int playerCount)
    {
        Game game = new(seed);
        game.SetUpRandomGame(playerCount);
        return game;
    }

    // 3 to 7 players, so that Babylon B and Halikarnassos come up
    private static int PlayerCountOf(int gameIndex)
    {
        return 3 + gameIndex % 5;
    }

    [TestMethod]
    public void AFullRecord_HasOneSeatChose_PerDecision_InTheOrderTheAgentsChose()
    {
        HashSet<Phase> phasesSeen = new();
        for (int gameIndex = 0; gameIndex < GameCount; gameIndex++)
        {
            int playerCount = PlayerCountOf(gameIndex);
            Game game = NewGame(Seeding.GameSeed(19, gameIndex), playerCount);
            List<CountingAgent> agents = new();
            for (int seat = 0; seat < playerCount; seat++)
            {
                agents.Add(new CountingAgent());
            }

            MatchRecord record = new Match(game, agents, MatchDetail.Full).Play();

            List<SeatChose> choices = record.Entries.OfType<SeatChose>().ToList();
            for (int seat = 0; seat < playerCount; seat++)
            {
                List<Move> recorded = choices
                    .Where(choice => choice.Seat == seat)
                    .Select(choice => choice.Move)
                    .ToList();
                CollectionAssert.AreEqual(agents[seat].Choices, recorded, $"game {gameIndex}, seat {seat}");
            }
            Assert.AreEqual(
                record.Entries.OfType<SeatShown>().Count(),
                choices.Count,
                "every seat shown a decision chose a move");
            phasesSeen.UnionWith(choices.Select(choice => choice.Phase));
        }

        Assert.IsTrue(phasesSeen.Contains(Phase.SeventhCard), "Babylon B played a last card");
        Assert.IsTrue(phasesSeen.Contains(Phase.DiscardPileBuild), "Halikarnassos built from the discard pile");
    }

    [TestMethod]
    public void AFullRecord_StartsWithTheSetUp_AndEndsWithTheGameOverAndTheScores()
    {
        Game game = NewGame(Seeding.GameSeed(23, 0), 4);
        List<IAgent> agents = new()
        {
            new HeuristicAgent(), new RandomAgent(), new HeuristicAgent(), new RandomAgent(),
        };

        MatchRecord record = new Match(game, agents, MatchDetail.Full).Play();

        Assert.IsInstanceOfType(record.Entries[0], typeof(MatchSetUp));
        int lastIndex = record.Entries.Count - 1;
        FinalScores? scores = record.Entries[lastIndex] as FinalScores;
        Assert.IsNotNull(scores, "the scores come last");
        CollectionAssert.AreEqual(
            record.Results.Select(result => result.VictoryPoints).ToList(),
            scores.Seats.Select(seat => seat.VictoryPoints).ToList());
        List<GameEvent> gameEvents = record.Entries.OfType<GameEventEntry>().Select(entry => entry.Event).ToList();
        int gameOverCount = gameEvents.Count(gameEvent => gameEvent is GameEnded);
        Assert.AreEqual(1, gameOverCount);
        int battleCount = gameEvents.Count(gameEvent => gameEvent is BattleFought);
        Assert.AreEqual(3 * 4, battleCount, "each seat's battles, each era");
    }

    [TestMethod]
    public void WritingTheRecordAfterTheGame_GivesTheSameTraceAsTheLiveWriter()
    {
        ListSink liveSink = new();
        Game game = NewGame(Seeding.GameSeed(29, 0), 5);
        List<IAgent> agents = new()
        {
            new HeuristicAgent(), new RandomAgent(), new HeuristicAgent(), new RandomAgent(), new RandomAgent(),
        };
        MatchRecord record = new Match(game, agents, new MatchTraceWriter(VerboseLoggerInto(liveSink))).Play();

        ListSink laterSink = new();
        MatchTraceWriter laterWriter = new(VerboseLoggerInto(laterSink));
        foreach (MatchEntry entry in record.Entries)
        {
            laterWriter.EntryAdded(entry);
        }

        Assert.IsTrue(liveSink.Events.Count > 100, "the live writer wrote the game");
        CollectionAssert.AreEqual(liveSink.Messages.ToList(), laterSink.Messages.ToList());
    }

    private static ILogger VerboseLoggerInto(ListSink sink)
    {
        return new LoggerConfiguration().MinimumLevel.Is(LogEventLevel.Verbose).WriteTo.Sink(sink).CreateLogger();
    }

    [TestMethod]
    public void AResultsOnlyRecord_KeepsNoEntries_AndTheSameResults()
    {
        for (int gameIndex = 0; gameIndex < 10; gameIndex++)
        {
            ulong seed = Seeding.GameSeed(37, gameIndex);
            List<IAgent> agents = new() { new HeuristicAgent(), new RandomAgent(), new RandomAgent() };
            List<IAgent> sameAgents = new() { new HeuristicAgent(), new RandomAgent(), new RandomAgent() };
            Match resultsOnly = new(NewGame(seed, 3), agents);
            Match full = new(NewGame(seed, 3), sameAgents, MatchDetail.Full);

            MatchRecord resultsOnlyRecord = resultsOnly.Play();
            MatchRecord fullRecord = full.Play();

            Assert.AreEqual(0, resultsOnlyRecord.Entries.Count);
            Assert.IsNull(resultsOnly.Game.EventListener, "no game events are built");
            Assert.IsTrue(fullRecord.Entries.Count > 0);
            CollectionAssert.AreEqual(fullRecord.Results, resultsOnlyRecord.Results, $"seed {seed}");
        }
    }

    [TestMethod]
    public void ASearchCopy_HasNoListener()
    {
        Game game = NewGame(Seeding.GameSeed(41, 0), 3);
        List<IAgent> agents = new() { new HeuristicAgent(), new RandomAgent(), new RandomAgent() };
        Match match = new(game, agents, MatchDetail.Full);
        match.Play();
        Assert.AreSame(match, game.EventListener);

        Game copy = game.DeepCopy();

        Assert.IsNull(copy.EventListener);
    }
}
