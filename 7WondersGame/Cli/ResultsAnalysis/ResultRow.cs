using System.Globalization;

namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>One row of a results CSV, as 'analyze' needs it. Swept: the row is the agent with the range setting.</summary>
public sealed class ResultRow
{
    /// <summary>A row with these column values.</summary>
    public ResultRow(string runId, long gameIndex, string seed, int seat, string agent, string configJson,
                     string wonder, int vp, int rank, double winShare, int players, bool swept = false)
    {
        RunId = runId;
        GameIndex = gameIndex;
        Seed = seed;
        Seat = seat;
        Agent = agent;
        ConfigJson = configJson;
        Wonder = wonder;
        Vp = vp;
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
    public string ConfigJson { get; }

    /// <summary>The wonder board and side, e.g. <c>GizahA</c>.</summary>
    public string Wonder { get; }

    /// <summary>The seat's final victory points.</summary>
    public int Vp { get; }

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
        return new ResultRow(RunId, GameIndex, Seed, Seat, Agent, ConfigJson, Wonder, Vp, Rank, WinShare, players,
                             Swept);
    }

    /// <summary>A copy of this row marked as the swept agent's.</summary>
    public ResultRow AsSwept()
    {
        return new ResultRow(RunId, GameIndex, Seed, Seat, Agent, ConfigJson, Wonder, Vp, Rank, WinShare, Players,
                             swept: true);
    }

    /// <summary>The rows of a results CSV, with each game's player count worked out from its rows.</summary>
    public static List<ResultRow> Read(IEnumerable<Dictionary<string, string>> csvRows)
    {
        var inv = CultureInfo.InvariantCulture;
        var rows = csvRows.Select(r => new ResultRow(
            r["runId"], long.Parse(r["gameIndex"], inv), r["seed"], int.Parse(r["seat"], inv), r["agent"], r["configJson"],
            r["wonder"] + r["side"], int.Parse(r["vp"], inv), int.Parse(r["rank"], inv),
            double.Parse(r["winShare"], inv), 0)).ToList();
        var players = rows.GroupBy(r => (r.RunId, r.GameIndex)).ToDictionary(g => g.Key, g => g.Count());
        return rows.Select(r => r.WithPlayers(players[(r.RunId, r.GameIndex)])).ToList();
    }
}
