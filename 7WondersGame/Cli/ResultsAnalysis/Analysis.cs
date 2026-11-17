using System.Globalization;
using System.Text;
using System.Text.Json;
using _7WondersGame.Cli.Results;

namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>
/// What 'analyze' computes from result rows (step 9 tooling): per group the win rate with a Wilson interval and
/// the mean VP, and for pairs of groups the paired VP and win-share differences over the same deals.
/// </summary>
public static class Analysis
{
    /// <summary>The 95% Wilson score interval for <paramref name="wins"/> out of <paramref name="n"/>.</summary>
    public static (double Low, double High) Wilson(double wins, int n, double z = PairStats.Z95)
    {
        if (n == 0)
            return (0, 1);
        double p = wins / n, z2 = z * z;
        double centre = (p + z2 / (2 * n)) / (1 + z2 / n);
        double half = z / (1 + z2 / n) * Math.Sqrt(p * (1 - p) / n + z2 / (4.0 * n * n));
        return (Math.Max(0, centre - half), Math.Min(1, centre + half));
    }

    /// <summary>Mean and standard error of the mean (0 for fewer than 2 values).</summary>
    public static (double Mean, double Se) MeanSe(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return (double.NaN, double.NaN);
        double mean = values.Average();
        if (values.Count < 2)
            return (mean, 0);
        double variance = values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1);
        return (mean, Math.Sqrt(variance / values.Count));
    }

    /// <summary>Marks the rows of the swept agent in the runs that had a range setting.</summary>
    public static List<ResultRow> MarkSwept(IReadOnlyList<ResultRow> rows, IReadOnlyDictionary<string, SweptSeat> sweeps) =>
        rows.Select(r => sweeps.TryGetValue(r.RunId, out SweptSeat? s) && s.SeatIn(r.GameIndex, r.Players) == r.Seat
            ? r with { Swept = true }
            : r).ToList();

    public static string KeyOf(ResultRow row, GroupBy by) => by switch
    {
        GroupBy.Player => (row.Swept ? "swept " : "") + row.Agent + " " + row.ConfigJson,
        GroupBy.Agent => row.Agent,
        GroupBy.Seat => "seat " + row.Seat.ToString(CultureInfo.InvariantCulture),
        GroupBy.Wonder => row.Wonder,
        _ => throw new ArgumentOutOfRangeException(nameof(by)),
    };

    /// <summary>
    /// The groups, numbered from 1 in the order the run plays them (by first game, then seat; not by when games
    /// finished), so a sweep's values come in order.
    /// </summary>
    public static List<GroupStats> Groups(IReadOnlyList<ResultRow> rows, GroupBy by)
    {
        var groups = rows.GroupBy(r => KeyOf(r, by))
            .OrderBy(g => g.Min(r => (r.RunId, r.GameIndex, r.Seat)))
            .ToList();
        Dictionary<string, string> labels = by == GroupBy.Player
            ? PlayerLabels(groups.Select(g => (g.Key, g.First().Agent, g.First().ConfigJson, g.First().Swept)).ToList())
            : groups.ToDictionary(g => g.Key, g => g.Key);

        return groups.Select((g, i) =>
        {
            int n = g.Count();
            double wins = g.Sum(r => r.WinShare);
            var (low, high) = Wilson(wins, n);
            var (vp, vpSe) = MeanSe(g.Select(r => (double)r.Vp).ToList());
            return new GroupStats(i + 1, labels[g.Key], g.Key, n, wins, wins / n, low, high,
                g.Average(r => 1.0 / r.Players), vp, vpSe, g.Average(r => (double)r.Rank));
        }).ToList();
    }

    /// <summary>
    /// Short names for agent configurations: the agent, plus the settings whose values differ between the groups
    /// of that agent (e.g. "MctsAgent C=0.3"). An agent with one configuration is just its name. The swept agent's
    /// values are named apart from the other players' (an opponent is "HeuristicAI" even if a swept value equals it).
    /// </summary>
    public static Dictionary<string, string> PlayerLabels(IReadOnlyList<(string Key, string Agent, string ConfigJson, bool Swept)> groups)
    {
        var labels = new Dictionary<string, string>();
        foreach (var sameAgent in groups.GroupBy(g => (g.Agent, g.Swept)))
        {
            var configs = sameAgent.Select(g => (g.Key, Settings: ParseConfig(g.ConfigJson))).ToList();
            var differing = configs.SelectMany(c => c.Settings.Keys).Distinct()
                .Where(name => configs.Select(c => c.Settings.GetValueOrDefault(name)).Distinct().Count() > 1)
                .OrderBy(name => name, StringComparer.Ordinal).ToList();
            foreach (var (key, settings) in configs)
                labels[key] = differing.Count == 0
                    ? sameAgent.Key.Agent
                    : sameAgent.Key.Agent + " " + string.Join(" ", differing.Select(d => $"{d}={settings.GetValueOrDefault(d) ?? "-"}"));
        }
        return labels;
    }

    private static Dictionary<string, string> ParseConfig(string json)
    {
        var settings = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                foreach (JsonProperty p in doc.RootElement.EnumerateObject())
                    settings[p.Name] = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString()! : p.Value.GetRawText();
        }
        catch (JsonException)
        {
            settings["config"] = json;
        }
        return settings;
    }

    /// <summary>The paired comparison of two groups over the deals both played (null if they share none).</summary>
    public static PairStats? Compare(IReadOnlyList<ResultRow> rows, GroupBy by, GroupStats a, GroupStats b)
    {
        Dictionary<string, (double Vp, double Win)> PerDeal(GroupStats g) => rows.Where(r => KeyOf(r, by) == g.Key)
            .GroupBy(r => r.Seed)
            .ToDictionary(d => d.Key, d => (d.Average(r => (double)r.Vp), d.Average(r => r.WinShare)));

        var dealsA = PerDeal(a);
        var dealsB = PerDeal(b);
        var shared = dealsA.Keys.Where(dealsB.ContainsKey).ToList();
        if (shared.Count == 0)
            return null;
        var (vp, vpSe) = MeanSe(shared.Select(d => dealsA[d].Vp - dealsB[d].Vp).ToList());
        var (win, winSe) = MeanSe(shared.Select(d => dealsA[d].Win - dealsB[d].Win).ToList());
        return new PairStats(a, b, shared.Count, vp, vpSe, win, winSe);
    }

    /// <summary>Without --vs, 'analyze' lists every pair only up to this many groups.</summary>
    public const int MaxAllPairsGroups = 8;

    /// <summary>Every pair of groups, or every group against group <paramref name="vs"/>, that played shared deals.</summary>
    public static List<PairStats> Pairs(IReadOnlyList<ResultRow> rows, GroupBy by, IReadOnlyList<GroupStats> groups, int? vs)
    {
        var pairs = new List<(GroupStats, GroupStats)>();
        if (vs is int v)
        {
            GroupStats baseline = groups.Single(g => g.Number == v);
            pairs.AddRange(groups.Where(g => g != baseline).Select(g => (g, baseline)));
        }
        else
        {
            for (int i = 0; i < groups.Count; i++)
                for (int j = i + 1; j < groups.Count; j++)
                    pairs.Add((groups[i], groups[j]));
        }
        return pairs.Select(p => Compare(rows, by, p.Item1, p.Item2)).OfType<PairStats>().ToList();
    }

    // ============ output ============

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static string F(double v, string format) => v.ToString(format, Inv);

    public static string GroupsTable(IReadOnlyList<GroupStats> groups)
    {
        int width = Math.Max(5, groups.Max(g => g.Label.Length));
        var sb = new StringBuilder();
        sb.AppendLine($"{"#",3}  {"Group".PadRight(width)}  {"Games",6}  {"Win rate",8}  {"95% interval",15}  {"Fair",5}  {"Mean VP",13}  {"Rank",5}");
        foreach (GroupStats g in groups)
            sb.AppendLine($"{g.Number,3}  {g.Label.PadRight(width)}  {g.Games,6}  {F(g.WinRate, "0.000"),8}  " +
                          $"{"[" + F(g.WinLow, "0.000") + ", " + F(g.WinHigh, "0.000") + "]",15}  {F(g.FairShare, "0.00"),5}  " +
                          $"{F(g.MeanVp, "0.00") + " +- " + F(g.VpSe, "0.00"),13}  {F(g.MeanRank, "0.00"),5}");
        return sb.ToString();
    }

    public static string PairsTable(IReadOnlyList<PairStats> pairs)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{"A - B",9}  {"Deals",5}  {"VP diff",8}  {"95% interval",17}  {"Win diff",8}  {"95% interval",17}");
        foreach (PairStats p in pairs)
        {
            string name = $"{p.A.Number} - {p.B.Number}";
            sb.AppendLine($"{name,9}  {p.Deals,5}  {F(p.VpDiff, "+0.00;-0.00;0.00"),8}  {Interval(p.VpDiff, p.VpDiffSe, "0.00"),17}{(p.VpDiffers ? "*" : " ")}" +
                          $" {F(p.WinDiff, "+0.000;-0.000;0.000"),8}  {Interval(p.WinDiff, p.WinDiffSe, "0.000"),17}{(p.WinDiffers ? "*" : " ")}");
        }
        sb.AppendLine("* the 95% interval leaves out 0: the difference is unlikely to be chance.");
        return sb.ToString();
    }

    private static string Interval(double mean, double se, string format) =>
        "[" + F(mean - PairStats.Z95 * se, format) + ", " + F(mean + PairStats.Z95 * se, format) + "]";

    public static readonly string[] GroupColumns =
        { "group", "label", "key", "games", "wins", "winRate", "winLow95", "winHigh95", "fairShare", "meanVp", "vpSe", "meanRank" };

    public static readonly string[] PairColumns =
        { "groupA", "labelA", "groupB", "labelB", "deals", "vpDiff", "vpDiffSe", "vpDiffLow95", "vpDiffHigh95",
          "winDiff", "winDiffSe", "winDiffLow95", "winDiffHigh95" };

    public static IEnumerable<string> GroupsCsv(IEnumerable<GroupStats> groups) =>
        groups.Select(g => Row(g.Number.ToString(Inv), g.Label, g.Key, g.Games.ToString(Inv), R(g.Wins), R(g.WinRate),
            R(g.WinLow), R(g.WinHigh), R(g.FairShare), R(g.MeanVp), R(g.VpSe), R(g.MeanRank)))
            .Prepend(string.Join(",", GroupColumns));

    public static IEnumerable<string> PairsCsv(IEnumerable<PairStats> pairs) =>
        pairs.Select(p => Row(p.A.Number.ToString(Inv), p.A.Label, p.B.Number.ToString(Inv), p.B.Label, p.Deals.ToString(Inv),
            R(p.VpDiff), R(p.VpDiffSe), R(p.VpDiff - PairStats.Z95 * p.VpDiffSe), R(p.VpDiff + PairStats.Z95 * p.VpDiffSe),
            R(p.WinDiff), R(p.WinDiffSe), R(p.WinDiff - PairStats.Z95 * p.WinDiffSe), R(p.WinDiff + PairStats.Z95 * p.WinDiffSe)))
            .Prepend(string.Join(",", PairColumns));

    private static string R(double v) => v.ToString("0.######", Inv);
    private static string Row(params string[] fields) => string.Join(",", fields.Select(ResultsCsvWriter.Escape));
}
