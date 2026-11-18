using System.Globalization;
using System.Text;
using System.Text.Json;
using _7WondersGame.Cli.Agents;
using _7WondersGame.Cli.Results;

namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>
/// What 'analyze' computes from result rows: per group the win rate with a Wilson interval and the mean VP, and for
/// pairs of groups the paired VP and win-share differences over the same deals.
/// </summary>
public static class Analysis
{
    /// <summary>Without --vs, 'analyze' lists every pair only up to this many groups.</summary>
    public const int MaxAllPairsGroups = 8;

    /// <summary>The columns of the group table's CSV.</summary>
    public static readonly string[] GroupColumns =
    {
        "group", "label", "key", "games", "wins", "winRate", "winLow95", "winHigh95", "fairShare", "meanVp", "vpSe",
        "meanRank",
    };

    /// <summary>The columns of the pairs' CSV.</summary>
    public static readonly string[] PairColumns =
    {
        "groupA", "labelA", "groupB", "labelB", "deals", "vpDiff", "vpDiffSe", "vpDiffLow95", "vpDiffHigh95",
        "winDiff", "winDiffSe", "winDiffLow95", "winDiffHigh95",
    };

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>The 95% Wilson score interval for <paramref name="wins"/> out of <paramref name="n"/>.</summary>
    public static (double Low, double High) Wilson(double wins, int n, double z = PairStats.Z95)
    {
        if (n == 0)
        {
            return (0, 1);
        }
        double p = wins / n;
        double z2 = z * z;
        double centre = (p + z2 / (2 * n)) / (1 + z2 / n);
        double half = z / (1 + z2 / n) * Math.Sqrt(p * (1 - p) / n + z2 / (4.0 * n * n));
        return (Math.Max(0, centre - half), Math.Min(1, centre + half));
    }

    /// <summary>Mean and standard error of the mean (0 for fewer than 2 values).</summary>
    public static (double Mean, double Se) MeanSe(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return (double.NaN, double.NaN);
        }
        double mean = values.Average();
        if (values.Count < 2)
        {
            return (mean, 0);
        }
        double variance = values.Sum(value => (value - mean) * (value - mean)) / (values.Count - 1);
        return (mean, Math.Sqrt(variance / values.Count));
    }

    /// <summary>Marks the rows of the swept agent in the runs that had a range setting.</summary>
    public static List<ResultRow> MarkSwept(IReadOnlyList<ResultRow> rows,
                                            IReadOnlyDictionary<string, SweptSeat> sweeps)
    {
        var marked = new List<ResultRow>(rows.Count);
        foreach (ResultRow row in rows)
        {
            marked.Add(MarkedIfSwept(row, sweeps));
        }
        return marked;
    }

    private static ResultRow MarkedIfSwept(ResultRow row, IReadOnlyDictionary<string, SweptSeat> sweeps)
    {
        if (sweeps.TryGetValue(row.RunId, out SweptSeat? sweptSeat)
            && sweptSeat.SeatIn(row.GameIndex, row.Players) == row.Seat)
        {
            return row.AsSwept();
        }
        return row;
    }

    /// <summary>What the rows of one group have in common, for a grouping.</summary>
    public static string KeyOf(ResultRow row, GroupBy by)
    {
        switch (by)
        {
            case GroupBy.Player:
                return PlayerKeyOf(row);
            case GroupBy.Agent:
                return row.Agent;
            case GroupBy.Seat:
                return "seat " + row.Seat.ToString(CultureInfo.InvariantCulture);
            case GroupBy.Wonder:
                return row.Wonder;
            default:
                throw new ArgumentOutOfRangeException(nameof(by));
        }
    }

    /// <summary>The agent and its settings; the swept agent apart from the others.</summary>
    private static string PlayerKeyOf(ResultRow row)
    {
        string key = row.Agent + " " + row.ConfigJson;
        if (row.Swept)
        {
            return "swept " + key;
        }
        return key;
    }

    /// <summary>
    /// The groups, numbered from 1 in the order the run plays them (by first game, then seat; not by when games
    /// finished), so a sweep's values come in order.
    /// </summary>
    public static List<GroupStats> Groups(IReadOnlyList<ResultRow> rows, GroupBy by)
    {
        var groups = rows.GroupBy(row => KeyOf(row, by))
            .OrderBy(FirstPlayed)
            .ToList();
        Dictionary<string, string> labels = LabelsOf(groups, by);

        var stats = new List<GroupStats>(groups.Count);
        for (int i = 0; i < groups.Count; i++)
        {
            int number = i + 1;
            stats.Add(StatsOf(groups[i], number, labels[groups[i].Key]));
        }
        return stats;
    }

    /// <summary>Where the group first comes in the run: its first game, then seat.</summary>
    private static (string RunId, long GameIndex, int Seat) FirstPlayed(IGrouping<string, ResultRow> group)
    {
        return group.Min(PlayOrderOf);
    }

    private static (string RunId, long GameIndex, int Seat) PlayOrderOf(ResultRow row)
    {
        return (row.RunId, row.GameIndex, row.Seat);
    }

    /// <summary>The printed name of each group, by key: short names for players, otherwise the key.</summary>
    private static Dictionary<string, string> LabelsOf(List<IGrouping<string, ResultRow>> groups, GroupBy by)
    {
        if (by == GroupBy.Player)
        {
            return PlayerLabels(groups.Select(PlayerGroupOf).ToList());
        }
        return groups.ToDictionary(group => group.Key, group => group.Key);
    }

    private static (string Key, string Agent, string ConfigJson, bool Swept) PlayerGroupOf(
        IGrouping<string, ResultRow> group)
    {
        ResultRow first = group.First();
        return (group.Key, first.Agent, first.ConfigJson, first.Swept);
    }

    private static GroupStats StatsOf(IGrouping<string, ResultRow> group, int number, string label)
    {
        int n = group.Count();
        double wins = group.Sum(row => row.WinShare);
        var (low, high) = Wilson(wins, n);
        var (vp, vpSe) = MeanSe(group.Select(row => (double)row.Vp).ToList());
        double fairShare = group.Average(row => 1.0 / row.Players);
        double meanRank = group.Average(row => (double)row.Rank);
        return new GroupStats(number, label, group.Key, n, wins, wins / n, low, high, fairShare, vp, vpSe, meanRank);
    }

    /// <summary>
    /// Short names for agent configurations: the agent, plus the settings whose values differ between the groups
    /// of that agent (e.g. "MctsAgent C=0.3"). An agent with one configuration is just its name. The swept agent's
    /// values are named apart from the other players' (an opponent is "HeuristicAI" even if a swept value equals it).
    /// </summary>
    public static Dictionary<string, string> PlayerLabels(
        IReadOnlyList<(string Key, string Agent, string ConfigJson, bool Swept)> groups)
    {
        var labels = new Dictionary<string, string>();
        foreach (var sameAgent in groups.GroupBy(group => (group.Agent, group.Swept)))
        {
            var configs = sameAgent.Select(group => (group.Key, Settings: ParseConfig(group.ConfigJson))).ToList();
            List<string> differing = DifferingSettings(configs);
            foreach (var (key, settings) in configs)
            {
                labels[key] = LabelOf(sameAgent.Key.Agent, differing, settings);
            }
        }
        return labels;
    }

    /// <summary>The settings, in name order, that don't have the same value in every configuration.</summary>
    private static List<string> DifferingSettings(List<(string Key, Dictionary<string, string> Settings)> configs)
    {
        var differing = new List<string>();
        foreach (var config in configs)
        {
            foreach (string name in config.Settings.Keys)
            {
                if (!differing.Contains(name) && HasDifferentValues(configs, name))
                {
                    differing.Add(name);
                }
            }
        }
        differing.Sort(StringComparer.Ordinal);
        return differing;
    }

    /// <summary>
    /// Whether two configurations give the setting different values (a missing setting is a value too).
    /// </summary>
    private static bool HasDifferentValues(List<(string Key, Dictionary<string, string> Settings)> configs,
                                           string name)
    {
        string? firstValue = configs[0].Settings.GetValueOrDefault(name);
        foreach (var config in configs)
        {
            if (config.Settings.GetValueOrDefault(name) != firstValue)
            {
                return true;
            }
        }
        return false;
    }

    private static string LabelOf(string agent, List<string> differing, Dictionary<string, string> settings)
    {
        if (differing.Count == 0)
        {
            return agent;
        }
        var shownSettings = new List<string>(differing.Count);
        foreach (string name in differing)
        {
            shownSettings.Add($"{name}={ValueOrDash(settings, name)}");
        }
        return agent + " " + string.Join(" ", shownSettings);
    }

    private static string ValueOrDash(Dictionary<string, string> settings, string name)
    {
        if (settings.TryGetValue(name, out string? value))
        {
            return value;
        }
        return "-";
    }

    private static Dictionary<string, string> ParseConfig(string json)
    {
        var settings = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty p in doc.RootElement.EnumerateObject())
                    {
                        settings[p.Name] = AgentCatalog.SettingValueText(p.Value);
                    }
                }
            }
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
        var dealsA = PerDeal(rows, by, a);
        var dealsB = PerDeal(rows, by, b);
        var shared = dealsA.Keys.Where(dealsB.ContainsKey).ToList();
        if (shared.Count == 0)
        {
            return null;
        }
        var (vp, vpSe) = MeanSe(shared.Select(deal => dealsA[deal].Vp - dealsB[deal].Vp).ToList());
        var (win, winSe) = MeanSe(shared.Select(deal => dealsA[deal].Win - dealsB[deal].Win).ToList());
        return new PairStats(a, b, shared.Count, vp, vpSe, win, winSe);
    }

    /// <summary>The group's mean VP and win share on each deal it played, by the deal's seed.</summary>
    private static Dictionary<string, (double Vp, double Win)> PerDeal(IReadOnlyList<ResultRow> rows, GroupBy by,
                                                                       GroupStats group)
    {
        return rows.Where(row => KeyOf(row, by) == group.Key)
            .GroupBy(row => row.Seed)
            .ToDictionary(deal => deal.Key, AveragesOf);
    }

    private static (double Vp, double Win) AveragesOf(IGrouping<string, ResultRow> deal)
    {
        return (deal.Average(row => (double)row.Vp), deal.Average(row => row.WinShare));
    }

    /// <summary>
    /// Every pair of groups, or every group against group <paramref name="vs"/>, that played shared deals.
    /// </summary>
    public static List<PairStats> Pairs(IReadOnlyList<ResultRow> rows, GroupBy by, IReadOnlyList<GroupStats> groups,
                                        int? vs)
    {
        var pairs = new List<(GroupStats, GroupStats)>();
        if (vs != null)
        {
            GroupStats baseline = groups.Single(group => group.Number == vs.Value);
            foreach (GroupStats group in groups)
            {
                if (group != baseline)
                {
                    pairs.Add((group, baseline));
                }
            }
        }
        else
        {
            for (int i = 0; i < groups.Count; i++)
            {
                for (int j = i + 1; j < groups.Count; j++)
                {
                    pairs.Add((groups[i], groups[j]));
                }
            }
        }

        var compared = new List<PairStats>();
        foreach (var (first, second) in pairs)
        {
            PairStats? pair = Compare(rows, by, first, second);
            if (pair != null)
            {
                compared.Add(pair);
            }
        }
        return compared;
    }

    // ============ output ============

    private static string F(double v, string format)
    {
        return v.ToString(format, Inv);
    }

    /// <summary>The groups as a printed table.</summary>
    public static string GroupsTable(IReadOnlyList<GroupStats> groups)
    {
        int width = Math.Max(5, groups.Max(group => group.Label.Length));
        var sb = new StringBuilder();
        sb.AppendLine($"{"#",3}  {"Group".PadRight(width)}  {"Games",6}  {"Win rate",8}  {"95% interval",15}  " +
                      $"{"Fair",5}  {"Mean VP",13}  {"Rank",5}");
        foreach (GroupStats g in groups)
        {
            string interval = "[" + F(g.WinLow, "0.000") + ", " + F(g.WinHigh, "0.000") + "]";
            string meanVp = F(g.MeanVp, "0.00") + " +- " + F(g.VpSe, "0.00");
            sb.AppendLine($"{g.Number,3}  {g.Label.PadRight(width)}  {g.Games,6}  {F(g.WinRate, "0.000"),8}  " +
                          $"{interval,15}  {F(g.FairShare, "0.00"),5}  {meanVp,13}  {F(g.MeanRank, "0.00"),5}");
        }
        return sb.ToString();
    }

    /// <summary>The paired differences as a printed table, with a star where the interval leaves out 0.</summary>
    public static string PairsTable(IReadOnlyList<PairStats> pairs)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{"A - B",9}  {"Deals",5}  {"VP diff",8}  {"95% interval",17}  {"Win diff",8}  " +
                      $"{"95% interval",17}");
        foreach (PairStats p in pairs)
        {
            string name = $"{p.A.Number} - {p.B.Number}";
            string vpDiff = F(p.VpDiff, "+0.00;-0.00;0.00");
            string vpInterval = Interval(p.VpDiff, p.VpDiffSe, "0.00");
            string winDiff = F(p.WinDiff, "+0.000;-0.000;0.000");
            string winInterval = Interval(p.WinDiff, p.WinDiffSe, "0.000");
            sb.AppendLine($"{name,9}  {p.Deals,5}  {vpDiff,8}  {vpInterval,17}{DiffersMark(p.VpDiffers)}" +
                          $" {winDiff,8}  {winInterval,17}{DiffersMark(p.WinDiffers)}");
        }
        sb.AppendLine("* the 95% interval leaves out 0: the difference is unlikely to be chance.");
        return sb.ToString();
    }

    private static string DiffersMark(bool differs)
    {
        if (differs)
        {
            return "*";
        }
        return " ";
    }

    private static string Interval(double mean, double se, string format)
    {
        return "[" + F(mean - PairStats.Z95 * se, format) + ", " + F(mean + PairStats.Z95 * se, format) + "]";
    }

    /// <summary>The group table as CSV lines, the header first.</summary>
    public static IEnumerable<string> GroupsCsv(IEnumerable<GroupStats> groups)
    {
        var lines = new List<string> { string.Join(",", GroupColumns) };
        foreach (GroupStats g in groups)
        {
            lines.Add(Row(g.Number.ToString(Inv), g.Label, g.Key, g.Games.ToString(Inv), R(g.Wins), R(g.WinRate),
                          R(g.WinLow), R(g.WinHigh), R(g.FairShare), R(g.MeanVp), R(g.VpSe), R(g.MeanRank)));
        }
        return lines;
    }

    /// <summary>The paired differences as CSV lines, the header first.</summary>
    public static IEnumerable<string> PairsCsv(IEnumerable<PairStats> pairs)
    {
        var lines = new List<string> { string.Join(",", PairColumns) };
        foreach (PairStats p in pairs)
        {
            double vpMargin = PairStats.Z95 * p.VpDiffSe;
            double winMargin = PairStats.Z95 * p.WinDiffSe;
            lines.Add(Row(p.A.Number.ToString(Inv), p.A.Label, p.B.Number.ToString(Inv), p.B.Label,
                          p.Deals.ToString(Inv),
                          R(p.VpDiff), R(p.VpDiffSe), R(p.VpDiff - vpMargin), R(p.VpDiff + vpMargin),
                          R(p.WinDiff), R(p.WinDiffSe), R(p.WinDiff - winMargin), R(p.WinDiff + winMargin)));
        }
        return lines;
    }

    private static string R(double v)
    {
        return v.ToString("0.######", Inv);
    }

    private static string Row(params string[] fields)
    {
        return string.Join(",", fields.Select(ResultsCsvWriter.Escape));
    }
}
