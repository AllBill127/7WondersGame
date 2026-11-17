using System.Globalization;

namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>One row of a results CSV, as 'analyze' needs it. Swept: the row is the agent with the range setting.</summary>
public sealed record ResultRow(string RunId, long GameIndex, string Seed, int Seat, string Agent, string ConfigJson,
                               string Wonder, int Vp, int Rank, double WinShare, int Players, bool Swept = false)
{
    /// <summary>The rows of a results CSV, with each game's player count worked out from its rows.</summary>
    public static List<ResultRow> Read(IEnumerable<Dictionary<string, string>> csvRows)
    {
        var inv = CultureInfo.InvariantCulture;
        var rows = csvRows.Select(r => new ResultRow(
            r["runId"], long.Parse(r["gameIndex"], inv), r["seed"], int.Parse(r["seat"], inv), r["agent"], r["configJson"],
            r["wonder"] + r["side"], int.Parse(r["vp"], inv), int.Parse(r["rank"], inv),
            double.Parse(r["winShare"], inv), 0)).ToList();
        var players = rows.GroupBy(r => (r.RunId, r.GameIndex)).ToDictionary(g => g.Key, g => g.Count());
        return rows.Select(r => r with { Players = players[(r.RunId, r.GameIndex)] }).ToList();
    }
}
