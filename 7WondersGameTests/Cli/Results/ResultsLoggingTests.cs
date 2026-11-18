using _7WondersGame.Agents.Common.Settings;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Results;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Cli.Results;

/// <summary>
/// One CSV row per (game, seat), written by a single appending writer.
/// </summary>
[TestClass]
public class ResultsLoggingTests
{
    private string _directory = "";

    /// <summary>Makes an empty folder for the test's files.</summary>
    [TestInitialize]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "7w-results-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Removes the test's folder.</summary>
    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public void Loop_WithRecord_GivesOneResultPerSeat()
    {
        Game game = NewGame(3);
        game.SetUpRandomGame();
        var wonders = game.Players.Select(player => player.Wonder.Id.ToString()).ToList();

        Dictionary<int, int> scores = game.Loop(record: true);

        List<SeatResult> results = game.Results!;
        Assert.AreEqual(3, results.Count);
        for (int seat = 0; seat < 3; seat++)
        {
            SeatResult seatResult = results[seat];
            Assert.AreEqual(seat, seatResult.Seat);
            Assert.AreEqual(scores[seat], seatResult.VictoryPoints);
            Assert.AreEqual(wonders[seat], seatResult.Wonder + seatResult.Side);
            Assert.IsTrue(seatResult.Side == "A" || seatResult.Side == "B");
        }

        Assert.AreEqual("RandomChoiceAI", results[0].Agent);
        Assert.AreEqual("HeuristicAI", results[1].Agent);
        Assert.AreEqual(1.0, results.Sum(result => result.WinShare), 1e-9, "win shares add up to 1");
        Assert.IsTrue(results.Any(result => result.Rank == 1));
    }

    [TestMethod]
    public void Loop_WithoutRecord_KeepsNoResults()
    {
        Game game = NewGame(3);
        game.SetUpRandomGame();
        game.Loop();
        Assert.IsNull(game.Results);
    }

    [TestMethod]
    public void ConfigJson_ListsTheAgentsOwnSettings()
    {
        Assert.AreEqual("{}", new RandomChoiceAI().ConfigJson());
        Assert.AreEqual("{\"C\":2.5,\"Iterations\":100}", new SettingsTestAgent { C = 2.5 }.ConfigJson());
    }

    [TestMethod]
    public void FormatRow_QuotesJsonAndUsesInvariantNumbers()
    {
        var seat = new SeatResult(
            2,
            "SomeAgent",
            "{\"c\":1.5,\"n\":2}",
            "Gizah",
            "B",
            51,
            7,
            1,
            0.5,
            "{\"searches\":3,\"iterations\":30}");

        string row = ResultsCsvWriter.FormatRow("run1", 4, 1, 18446744073709551615UL, seat);

        string expected = "run1,4,1,18446744073709551615,2,SomeAgent,\"{\"\"c\"\":1.5,\"\"n\"\":2}\"," +
                          "Gizah,B,51,7,1,0.5,\"{\"\"searches\"\":3,\"\"iterations\"\":30}\"";
        Assert.AreEqual(expected, row);
    }

    [TestMethod]
    public void Writer_WritesHeaderOnceAndAppendsAcrossRuns()
    {
        string path = Path.Combine(_directory, "sub", "results.csv");
        SeatResult[] game = Seats(3);

        using (var writer = new ResultsCsvWriter(path))
        {
            writer.WriteGame("runA", 0, 0, 11, game);
        }

        using (var writer = new ResultsCsvWriter(path))
        {
            writer.WriteGame("runB", 0, 0, 12, game);
        }

        string[] lines = File.ReadAllLines(path);
        Assert.AreEqual(ResultsCsvWriter.Header, lines[0]);
        Assert.AreEqual(1 + 6, lines.Length);
        Assert.AreEqual(3, lines.Count(line => line.StartsWith("runA,")));
        Assert.AreEqual(3, lines.Count(line => line.StartsWith("runB,")));
    }

    [TestMethod]
    public void Writer_RefusesAFileWithADifferentHeader()
    {
        string path = Path.Combine(_directory, "old.csv");
        File.WriteAllText(path, "Player_0,Player_1\n1,2\n");

        Assert.ThrowsException<InvalidOperationException>(() => new ResultsCsvWriter(path));
    }

    [TestMethod]
    public void Writer_KeepsEachGamesRowsWholeUnderParallelWrites()
    {
        string path = Path.Combine(_directory, "parallel.csv");
        using (var writer = new ResultsCsvWriter(path))
        {
            ParallelGameWriter gameWriter = new ParallelGameWriter(writer);
            Parallel.For(0, 200, gameWriter.WriteGame);
        }

        var rows = File.ReadAllLines(path).Skip(1).Select(line => line.Split(',')).ToList();
        Assert.AreEqual(600, rows.Count);
        Assert.IsTrue(rows.All(row => row.Length == ResultsCsvWriter.Columns.Length));
        // a game's 3 rows are written together
        for (int firstRowIndex = 0; firstRowIndex < rows.Count; firstRowIndex += 3)
        {
            int secondRowIndex = firstRowIndex + 1;
            int thirdRowIndex = firstRowIndex + 2;
            Assert.AreEqual(rows[firstRowIndex][1], rows[secondRowIndex][1]);
            Assert.AreEqual(rows[firstRowIndex][1], rows[thirdRowIndex][1]);
            string[] seatsOfTheGame = rows.Skip(firstRowIndex).Take(3).Select(row => row[4]).ToArray();
            CollectionAssert.AreEqual(new[] { "0", "1", "2" }, seatsOfTheGame);
        }
    }

    // ===== helpers =====

    private static Game NewGame(ulong seed)
    {
        Game game = new Game(seed);
        game.Agents.Add(new RandomChoiceAI());
        game.Agents.Add(new HeuristicAI());
        game.Agents.Add(new RandomChoiceAI());
        return game;
    }

    /// <summary>A made-up game's results for <paramref name="seatCount"/> seats; the last seat wins.</summary>
    internal static SeatResult[] Seats(int seatCount)
    {
        SeatResult[] seats = new SeatResult[seatCount];
        int lastSeat = seatCount - 1;
        for (int seat = 0; seat < seatCount; seat++)
        {
            int victoryPoints = 40 + seat;
            int rank = seatCount - seat;
            double winShare = 0;
            if (seat == lastSeat)
            {
                winShare = 1;
            }

            seats[seat] = new SeatResult(seat, "RandomChoiceAI", "{}", "Rhodos", "A", victoryPoints, 3, rank, winShare);
        }

        return seats;
    }
}
