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

    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    /// <summary>The 95% Wilson score interval for <paramref name="wins"/> out of <paramref name="games"/>.</summary>
    public static (double Low, double High) Wilson(double wins, int games, double zScore = PairStats.Z95)
    {
        if (games == 0)
        {
            return (0, 1);
        }
        double winRate = wins / games;
        double zScoreSquared = zScore * zScore;
        double centre = (winRate + zScoreSquared / (2 * games)) / (1 + zScoreSquared / games);
        double halfWidth = zScore / (1 + zScoreSquared / games)
            * Math.Sqrt(winRate * (1 - winRate) / games + zScoreSquared / (4.0 * games * games));
        return (Math.Max(0, centre - halfWidth), Math.Min(1, centre + halfWidth));
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

        var statistics = new List<GroupStats>(groups.Count);
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            int number = groupIndex + 1;
            statistics.Add(StatsOf(groups[groupIndex], number, labels[groups[groupIndex].Key]));
        }
        return statistics;
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
        int games = group.Count();
        double wins = group.Sum(row => row.WinShare);
        var (low, high) = Wilson(wins, games);
        List<double> victoryPoints = group.Select(row => (double)row.Vp).ToList();
        var (meanVictoryPoints, victoryPointsStandardError) = MeanSe(victoryPoints);
        double fairShare = group.Average(row => 1.0 / row.Players);
        double meanRank = group.Average(row => (double)row.Rank);
        return new GroupStats(number, label, group.Key, games, wins, wins / games, low, high, fairShare,
                              meanVictoryPoints, victoryPointsStandardError, meanRank);
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
            var settingsByGroup = sameAgent.Select(group => (group.Key, Settings: ParseConfig(group.ConfigJson)))
                                           .ToList();
            List<string> differing = DifferingSettings(settingsByGroup);
            foreach (var (key, settings) in settingsByGroup)
            {
                labels[key] = LabelOf(sameAgent.Key.Agent, differing, settings);
            }
        }
        return labels;
    }

    /// <summary>The settings, in name order, that don't have the same value in every configuration.</summary>
    private static List<string> DifferingSettings(
        List<(string Key, Dictionary<string, string> Settings)> settingsByGroup)
    {
        var differing = new List<string>();
        foreach (var groupSettings in settingsByGroup)
        {
            foreach (string name in groupSettings.Settings.Keys)
            {
                if (!differing.Contains(name) && HasDifferentValues(settingsByGroup, name))
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
    private static bool HasDifferentValues(List<(string Key, Dictionary<string, string> Settings)> settingsByGroup,
                                           string name)
    {
        string? firstValue = settingsByGroup[0].Settings.GetValueOrDefault(name);
        foreach (var groupSettings in settingsByGroup)
        {
            if (groupSettings.Settings.GetValueOrDefault(name) != firstValue)
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
                    foreach (JsonProperty property in doc.RootElement.EnumerateObject())
                    {
                        settings[property.Name] = AgentCatalog.SettingValueText(property.Value);
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
    public static PairStats? Compare(IReadOnlyList<ResultRow> rows, GroupBy by, GroupStats groupA, GroupStats groupB)
    {
        var dealsA = PerDeal(rows, by, groupA);
        var dealsB = PerDeal(rows, by, groupB);
        var shared = dealsA.Keys.Where(dealsB.ContainsKey).ToList();
        if (shared.Count == 0)
        {
            return null;
        }
        List<double> victoryPointDifferences = shared.Select(deal => dealsA[deal].Vp - dealsB[deal].Vp).ToList();
        List<double> winDifferences = shared.Select(deal => dealsA[deal].Win - dealsB[deal].Win).ToList();
        var (victoryPointDifference, victoryPointDifferenceStandardError) = MeanSe(victoryPointDifferences);
        var (winDifference, winDifferenceStandardError) = MeanSe(winDifferences);
        return new PairStats(groupA, groupB, shared.Count, victoryPointDifference, victoryPointDifferenceStandardError,
                             winDifference, winDifferenceStandardError);
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
            for (int firstIndex = 0; firstIndex < groups.Count; firstIndex++)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < groups.Count; secondIndex++)
                {
                    pairs.Add((groups[firstIndex], groups[secondIndex]));
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

    private static string Formatted(double value, string format)
    {
        return value.ToString(format, InvariantCulture);
    }

    /// <summary>The groups as a printed table.</summary>
    public static string GroupsTable(IReadOnlyList<GroupStats> groups)
    {
        int width = Math.Max(5, groups.Max(group => group.Label.Length));
        var table = new StringBuilder();
        table.AppendLine($"{"#",3}  {"Group".PadRight(width)}  {"Games",6}  {"Win rate",8}  {"95% interval",15}  " +
                         $"{"Fair",5}  {"Mean VP",13}  {"Rank",5}");
        foreach (GroupStats group in groups)
        {
            string interval = "[" + Formatted(group.WinLow, "0.000") + ", " + Formatted(group.WinHigh, "0.000") + "]";
            string meanVictoryPoints = Formatted(group.MeanVp, "0.00") + " +- " + Formatted(group.VpSe, "0.00");
            string winRate = Formatted(group.WinRate, "0.000");
            string fairShare = Formatted(group.FairShare, "0.00");
            string meanRank = Formatted(group.MeanRank, "0.00");
            table.AppendLine($"{group.Number,3}  {group.Label.PadRight(width)}  {group.Games,6}  {winRate,8}  " +
                             $"{interval,15}  {fairShare,5}  {meanVictoryPoints,13}  {meanRank,5}");
        }
        return table.ToString();
    }

    /// <summary>The paired differences as a printed table, with a star where the interval leaves out 0.</summary>
    public static string PairsTable(IReadOnlyList<PairStats> pairs)
    {
        var table = new StringBuilder();
        table.AppendLine($"{"A - B",9}  {"Deals",5}  {"VP diff",8}  {"95% interval",17}  {"Win diff",8}  " +
                         $"{"95% interval",17}");
        foreach (PairStats pair in pairs)
        {
            string name = $"{pair.A.Number} - {pair.B.Number}";
            string victoryPointDifference = Formatted(pair.VpDiff, "+0.00;-0.00;0.00");
            string victoryPointInterval = Interval(pair.VpDiff, pair.VpDiffSe, "0.00");
            string winDifference = Formatted(pair.WinDiff, "+0.000;-0.000;0.000");
            string winInterval = Interval(pair.WinDiff, pair.WinDiffSe, "0.000");
            table.AppendLine($"{name,9}  {pair.Deals,5}  {victoryPointDifference,8}  " +
                             $"{victoryPointInterval,17}{DiffersMark(pair.VpDiffers)}" +
                             $" {winDifference,8}  {winInterval,17}{DiffersMark(pair.WinDiffers)}");
        }
        table.AppendLine("* the 95% interval leaves out 0: the difference is unlikely to be chance.");
        return table.ToString();
    }

    private static string DiffersMark(bool differs)
    {
        if (differs)
        {
            return "*";
        }
        return " ";
    }

    private static string Interval(double mean, double standardError, string format)
    {
        string low = Formatted(mean - PairStats.Z95 * standardError, format);
        string high = Formatted(mean + PairStats.Z95 * standardError, format);
        return "[" + low + ", " + high + "]";
    }

    /// <summary>The group table as CSV lines, the header first.</summary>
    public static IEnumerable<string> GroupsCsv(IEnumerable<GroupStats> groups)
    {
        var lines = new List<string> { string.Join(",", GroupColumns) };
        foreach (GroupStats group in groups)
        {
            lines.Add(Row(group.Number.ToString(InvariantCulture), group.Label, group.Key,
                          group.Games.ToString(InvariantCulture),
                          RoundForOutput(group.Wins), RoundForOutput(group.WinRate),
                          RoundForOutput(group.WinLow), RoundForOutput(group.WinHigh),
                          RoundForOutput(group.FairShare),
                          RoundForOutput(group.MeanVp), RoundForOutput(group.VpSe),
                          RoundForOutput(group.MeanRank)));
        }
        return lines;
    }

    /// <summary>The paired differences as CSV lines, the header first.</summary>
    public static IEnumerable<string> PairsCsv(IEnumerable<PairStats> pairs)
    {
        var lines = new List<string> { string.Join(",", PairColumns) };
        foreach (PairStats pair in pairs)
        {
            double victoryPointMargin = PairStats.Z95 * pair.VpDiffSe;
            double winMargin = PairStats.Z95 * pair.WinDiffSe;
            lines.Add(Row(pair.A.Number.ToString(InvariantCulture), pair.A.Label,
                          pair.B.Number.ToString(InvariantCulture), pair.B.Label,
                          pair.Deals.ToString(InvariantCulture),
                          RoundForOutput(pair.VpDiff), RoundForOutput(pair.VpDiffSe),
                          RoundForOutput(pair.VpDiff - victoryPointMargin),
                          RoundForOutput(pair.VpDiff + victoryPointMargin),
                          RoundForOutput(pair.WinDiff), RoundForOutput(pair.WinDiffSe),
                          RoundForOutput(pair.WinDiff - winMargin),
                          RoundForOutput(pair.WinDiff + winMargin)));
        }
        return lines;
    }

    private static string RoundForOutput(double value)
    {
        return value.ToString("0.######", InvariantCulture);
    }

    private static string Row(params string[] fields)
    {
        return string.Join(",", fields.Select(ResultsCsvWriter.Escape));
    }
}
