using System.Globalization;

namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>
/// One row of a results CSV, as 'analyze' needs it. Swept: the row is the agent with the range setting.
/// </summary>
public sealed class ResultRow
{
    /// <summary>A row with these column values.</summary>
    public ResultRow(string runId, long gameIndex, string seed, int seat, string agent, string configurationJson,
                     string wonder, int victoryPoints, int rank, double winShare, int players, bool swept = false)
    {
        RunId = runId;
        GameIndex = gameIndex;
        Seed = seed;
        Seat = seat;
        Agent = agent;
        ConfigurationJson = configurationJson;
        Wonder = wonder;
        VictoryPoints = victoryPoints;
        Rank = rank;
        WinShare = winShare;
        Players = players;
        Swept = swept;
    }

    /// <summary>The run the game belongs to.</summary>
    public string RunId { get; }

    /// <summary>The game's number in its run.</summary>
    public long GameIndex { get; }

    /// <summary>The game's seed, as written in the file (the same seed means the same deal).</summary>
    public string Seed { get; }

    /// <summary>The seat the row is about.</summary>
    public int Seat { get; }

    /// <summary>The agent's name.</summary>
    public string Agent { get; }

    /// <summary>The agent's settings as JSON.</summary>
    public string ConfigurationJson { get; }

    /// <summary>The wonder board and side, e.g. <c>GizahA</c>.</summary>
    public string Wonder { get; }

    /// <summary>The seat's final victory points.</summary>
    public int VictoryPoints { get; }

    /// <summary>The seat's place, 1 for the winner.</summary>
    public int Rank { get; }

    /// <summary>The seat's share of the win: 1, 1/k in a k-way tie for first, otherwise 0.</summary>
    public double WinShare { get; }

    /// <summary>How many players the game had.</summary>
    public int Players { get; }

    /// <summary>Whether this is the agent with the range setting.</summary>
    public bool Swept { get; }

    /// <summary>A copy of this row with the player count set.</summary>
    public ResultRow WithPlayers(int players)
    {
        return new ResultRow(RunId, GameIndex, Seed, Seat, Agent, ConfigurationJson, Wonder, VictoryPoints, Rank,
                             WinShare, players, Swept);
    }

    /// <summary>A copy of this row marked as the swept agent's.</summary>
    public ResultRow AsSwept()
    {
        return new ResultRow(RunId, GameIndex, Seed, Seat, Agent, ConfigurationJson, Wonder, VictoryPoints, Rank,
                             WinShare, Players, swept: true);
    }

    /// <summary>The rows of a results CSV, with each game's player count worked out from its rows.</summary>
    public static List<ResultRow> Read(IEnumerable<Dictionary<string, string>> csvRows)
    {
        List<ResultRow> rows = csvRows.Select(FromCsv).ToList();
        var players = rows
            .GroupBy(row => (row.RunId, row.GameIndex))
            .ToDictionary(game => game.Key, game => game.Count());

        var withPlayers = new List<ResultRow>(rows.Count);
        foreach (ResultRow row in rows)
        {
            var game = (row.RunId, row.GameIndex);
            withPlayers.Add(row.WithPlayers(players[game]));
        }
        return withPlayers;
    }

    /// <summary>One CSV row (column → value), the player count not yet known (0).</summary>
    private static ResultRow FromCsv(Dictionary<string, string> csvRow)
    {
        var invariantCulture = CultureInfo.InvariantCulture;
        return new ResultRow(
            csvRow["runId"],
            long.Parse(csvRow["gameIndex"], invariantCulture),
            csvRow["seed"],
            int.Parse(csvRow["seat"], invariantCulture),
            csvRow["agent"],
            csvRow["configurationJson"],
            csvRow["wonder"] + csvRow["side"],
            int.Parse(csvRow["victoryPoints"], invariantCulture),
            int.Parse(csvRow["rank"], invariantCulture),
            double.Parse(csvRow["winShare"], invariantCulture),
            0);
    }
}
