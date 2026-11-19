using System.Globalization;
using System.Text;
using System.Text.Json;
using _7Wonders.Cli.Agents;
using _7Wonders.Cli.Results;

namespace _7Wonders.Cli.ResultsAnalysis;

/// <summary>
/// What 'analyze' computes from result rows: per group the win rate with a Wilson interval and the mean VP, and for
/// pairs of groups the paired VP and win-share differences over the same deals.
/// </summary>
public static class Analysis
{
    /// <summary>Without --compare-with, 'analyze' lists every pair only up to this many groups.</summary>
    public const int MostGroupsForAllPairs = 8;

    /// <summary>The columns of the group table's CSV.</summary>
    public static readonly string[] GroupColumns =
    {
        "group", "label", "key", "games", "wins", "winRate", "winRateLow95", "winRateHigh95", "fairShare",
        "meanVictoryPoints", "victoryPointsStandardError", "meanRank",
    };

    /// <summary>The columns of the pairs' CSV.</summary>
    public static readonly string[] PairColumns =
    {
        "groupA", "labelA", "groupB", "labelB", "deals",
        "victoryPointDifference", "victoryPointDifferenceStandardError",
        "victoryPointDifferenceLow95", "victoryPointDifferenceHigh95",
        "winRateDifference", "winRateDifferenceStandardError", "winRateDifferenceLow95", "winRateDifferenceHigh95",
    };

    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    /// <summary>The 95% Wilson score interval for <paramref name="wins"/> out of <paramref name="games"/>.</summary>
    public static ConfidenceInterval Wilson(double wins, int games, double zScore = PairStatistics.ZScore95)
    {
        if (games == 0)
        {
            return new ConfidenceInterval(0, 1);
        }
        double winRate = wins / games;
        double zScoreSquared = zScore * zScore;
        double centre = (winRate + zScoreSquared / (2 * games)) / (1 + zScoreSquared / games);
        double halfWidth = zScore / (1 + zScoreSquared / games)
            * Math.Sqrt(winRate * (1 - winRate) / games + zScoreSquared / (4.0 * games * games));
        return new ConfidenceInterval(Math.Max(0, centre - halfWidth), Math.Min(1, centre + halfWidth));
    }

    /// <summary>Mean and standard error of the mean (0 for fewer than 2 values).</summary>
    public static MeanWithStandardError MeanAndStandardError(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return new MeanWithStandardError(double.NaN, double.NaN);
        }
        double mean = values.Average();
        if (values.Count < 2)
        {
            return new MeanWithStandardError(mean, 0);
        }
        double variance = values.Sum(value => (value - mean) * (value - mean)) / (values.Count - 1);
        return new MeanWithStandardError(mean, Math.Sqrt(variance / values.Count));
    }

    /// <summary>Marks the rows of the swept agent in the runs that had a range setting.</summary>
    public static List<ResultRow> MarkSwept(IReadOnlyList<ResultRow> rows,
                                            IReadOnlyDictionary<string, SweptSeat> sweeps)
    {
        List<ResultRow> marked = new(rows.Count);
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
    public static string KeyOf(ResultRow row, GroupBy grouping)
    {
        switch (grouping)
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
                throw new ArgumentOutOfRangeException(nameof(grouping));
        }
    }

    /// <summary>The agent and its settings; the swept agent apart from the others.</summary>
    private static string PlayerKeyOf(ResultRow row)
    {
        string key = row.Agent + " " + row.ConfigurationJson;
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
    public static List<GroupStatistics> Groups(IReadOnlyList<ResultRow> rows, GroupBy grouping)
    {
        List<IGrouping<string, ResultRow>> groups = rows.GroupBy(row => KeyOf(row, grouping))
            .OrderBy(FirstPlayed)
            .ToList();
        Dictionary<string, string> labels = LabelsOf(groups, grouping);

        List<GroupStatistics> statistics = new(groups.Count);
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            int number = groupIndex + 1;
            statistics.Add(StatisticsOf(groups[groupIndex], number, labels[groups[groupIndex].Key]));
        }
        return statistics;
    }

    /// <summary>Where the group first comes in the run: its first game, then seat.</summary>
    private static PlayOrder? FirstPlayed(IGrouping<string, ResultRow> group)
    {
        return group.Min(row => new PlayOrder(row));
    }

    /// <summary>The printed name of each group, by key: short names for players, otherwise the key.</summary>
    private static Dictionary<string, string> LabelsOf(List<IGrouping<string, ResultRow>> groups, GroupBy grouping)
    {
        if (grouping == GroupBy.Player)
        {
            return PlayerLabels(groups.Select(PlayerGroupOf).ToList());
        }
        return groups.ToDictionary(group => group.Key, group => group.Key);
    }

    private static PlayerGroup PlayerGroupOf(IGrouping<string, ResultRow> group)
    {
        ResultRow first = group.First();
        return new PlayerGroup(group.Key, first.Agent, first.ConfigurationJson, first.Swept);
    }

    private static GroupStatistics StatisticsOf(IGrouping<string, ResultRow> group, int number, string label)
    {
        int games = group.Count();
        double wins = group.Sum(row => row.WinShare);
        ConfidenceInterval winRateInterval = Wilson(wins, games);
        List<double> victoryPoints = group.Select(row => (double)row.VictoryPoints).ToList();
        MeanWithStandardError meanVictoryPoints = MeanAndStandardError(victoryPoints);
        double fairShare = group.Average(row => 1.0 / row.Players);
        double meanRank = group.Average(row => (double)row.Rank);
        return new GroupStatistics(number, label, group.Key, games, wins, wins / games, winRateInterval.Low,
                                   winRateInterval.High, fairShare, meanVictoryPoints.Mean,
                                   meanVictoryPoints.StandardError, meanRank);
    }

    /// <summary>
    /// Short names for agent configurations: the agent, plus the settings whose values differ between the groups
    /// of that agent (e.g. "MctsAgent ExplorationConstant=0.3"). An agent with one configuration is just its name. The
    /// swept agent's values are named apart from the other players' (an opponent is "HeuristicAgent" even if a swept
    /// value equals it).
    /// </summary>
    public static Dictionary<string, string> PlayerLabels(IReadOnlyList<PlayerGroup> groups)
    {
        Dictionary<string, string> labels = new();
        foreach (IGrouping<string, PlayerGroup> sameAgent in groups.GroupBy(group => group.Agent))
        {
            foreach (IGrouping<bool, PlayerGroup> sameAgentAndSweep in sameAgent.GroupBy(group => group.Swept))
            {
                AddLabels(labels, sameAgent.Key, sameAgentAndSweep);
            }
        }
        return labels;
    }

    /// <summary>Adds the labels of the groups of one agent (all swept, or none).</summary>
    private static void AddLabels(Dictionary<string, string> labels, string agent, IEnumerable<PlayerGroup> groups)
    {
        Dictionary<string, Dictionary<string, string>> settingsByGroupKey = new();
        foreach (PlayerGroup group in groups)
        {
            settingsByGroupKey[group.Key] = ParseConfiguration(group.ConfigurationJson);
        }
        List<string> differing = DifferingSettings(settingsByGroupKey);
        foreach (KeyValuePair<string, Dictionary<string, string>> groupSettings in settingsByGroupKey)
        {
            labels[groupSettings.Key] = LabelOf(agent, differing, groupSettings.Value);
        }
    }

    /// <summary>The settings, in name order, that don't have the same value in every configuration.</summary>
    private static List<string> DifferingSettings(
        Dictionary<string, Dictionary<string, string>> settingsByGroupKey)
    {
        List<string> differing = new();
        foreach (Dictionary<string, string> settings in settingsByGroupKey.Values)
        {
            foreach (string name in settings.Keys)
            {
                if (!differing.Contains(name) && HasDifferentValues(settingsByGroupKey, name))
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
    private static bool HasDifferentValues(Dictionary<string, Dictionary<string, string>> settingsByGroupKey,
                                           string name)
    {
        string? firstValue = settingsByGroupKey.Values.First().GetValueOrDefault(name);
        foreach (Dictionary<string, string> settings in settingsByGroupKey.Values)
        {
            if (settings.GetValueOrDefault(name) != firstValue)
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
        List<string> shownSettings = new(differing.Count);
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

    private static Dictionary<string, string> ParseConfiguration(string json)
    {
        Dictionary<string, string> settings = new(StringComparer.Ordinal);
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
    public static PairStatistics? Compare(IReadOnlyList<ResultRow> rows, GroupBy grouping, GroupStatistics groupA,
                                     GroupStatistics groupB)
    {
        Dictionary<string, DealAverages> dealsA = PerDeal(rows, grouping, groupA);
        Dictionary<string, DealAverages> dealsB = PerDeal(rows, grouping, groupB);
        List<string> shared = dealsA.Keys.Where(dealsB.ContainsKey).ToList();
        if (shared.Count == 0)
        {
            return null;
        }
        List<double> victoryPointDifferences = shared
            .Select(deal => dealsA[deal].VictoryPoints - dealsB[deal].VictoryPoints)
            .ToList();
        List<double> winDifferences = shared.Select(deal => dealsA[deal].WinShare - dealsB[deal].WinShare).ToList();
        MeanWithStandardError victoryPointDifference = MeanAndStandardError(victoryPointDifferences);
        MeanWithStandardError winDifference = MeanAndStandardError(winDifferences);
        return new PairStatistics(groupA, groupB, shared.Count, victoryPointDifference.Mean,
                                  victoryPointDifference.StandardError, winDifference.Mean,
                                  winDifference.StandardError);
    }

    /// <summary>The group's mean VP and win share on each deal it played, by the deal's seed.</summary>
    private static Dictionary<string, DealAverages> PerDeal(
        IReadOnlyList<ResultRow> rows, GroupBy grouping, GroupStatistics group)
    {
        return rows.Where(row => KeyOf(row, grouping) == group.Key)
            .GroupBy(row => row.Seed)
            .ToDictionary(deal => deal.Key, AveragesOf);
    }

    private static DealAverages AveragesOf(IGrouping<string, ResultRow> deal)
    {
        double victoryPoints = deal.Average(row => (double)row.VictoryPoints);
        double winShare = deal.Average(row => row.WinShare);
        return new DealAverages(victoryPoints, winShare);
    }

    /// <summary>
    /// Every pair of groups, or every group against group <paramref name="compare-with"/>, that played shared deals.
    /// </summary>
    public static List<PairStatistics> Pairs(IReadOnlyList<ResultRow> rows, GroupBy grouping,
                                        IReadOnlyList<GroupStatistics> groups,
                                        int? comparedGroup)
    {
        List<PairStatistics> compared = new();
        if (comparedGroup != null)
        {
            GroupStatistics baseline = groups.Single(group => group.Number == comparedGroup.Value);
            foreach (GroupStatistics group in groups)
            {
                if (group != baseline)
                {
                    AddComparison(compared, rows, grouping, group, baseline);
                }
            }
            return compared;
        }
        for (int firstIndex = 0; firstIndex < groups.Count; firstIndex++)
        {
            for (int secondIndex = firstIndex + 1; secondIndex < groups.Count; secondIndex++)
            {
                AddComparison(compared, rows, grouping, groups[firstIndex], groups[secondIndex]);
            }
        }
        return compared;
    }

    /// <summary>
    /// Adds the comparison of two groups to <paramref name="compared"/>, if they played shared deals.
    /// </summary>
    private static void AddComparison(List<PairStatistics> compared, IReadOnlyList<ResultRow> rows, GroupBy grouping,
                                      GroupStatistics first, GroupStatistics second)
    {
        PairStatistics? pair = Compare(rows, grouping, first, second);
        if (pair != null)
        {
            compared.Add(pair);
        }
    }

    // ============ output ============

    private static string Formatted(double value, string format)
    {
        return value.ToString(format, InvariantCulture);
    }

    /// <summary>The groups as a printed table.</summary>
    public static string GroupsTable(IReadOnlyList<GroupStatistics> groups)
    {
        int width = Math.Max(5, groups.Max(group => group.Label.Length));
        StringBuilder table = new();
        table.AppendLine($"{"#",3}  {"Group".PadRight(width)}  {"Games",6}  {"Win rate",8}  {"95% interval",15}  " +
                         $"{"Fair",5}  {"Mean VP",13}  {"Rank",5}");
        foreach (GroupStatistics group in groups)
        {
            string winRateLow = Formatted(group.WinRateLow, "0.000");
            string winRateHigh = Formatted(group.WinRateHigh, "0.000");
            string interval = "[" + winRateLow + ", " + winRateHigh + "]";
            string meanVictoryPoints = Formatted(group.MeanVictoryPoints, "0.00") + " +- " +
                                       Formatted(group.VictoryPointsStandardError, "0.00");
            string winRate = Formatted(group.WinRate, "0.000");
            string fairShare = Formatted(group.FairShare, "0.00");
            string meanRank = Formatted(group.MeanRank, "0.00");
            table.AppendLine($"{group.Number,3}  {group.Label.PadRight(width)}  {group.Games,6}  {winRate,8}  " +
                             $"{interval,15}  {fairShare,5}  {meanVictoryPoints,13}  {meanRank,5}");
        }
        return table.ToString();
    }

    /// <summary>The paired differences as a printed table, with a star where the interval leaves out 0.</summary>
    public static string PairsTable(IReadOnlyList<PairStatistics> pairs)
    {
        StringBuilder table = new();
        table.AppendLine($"{"A - B",9}  {"Deals",5}  {"VP diff",8}  {"95% interval",17}  {"Win diff",8}  " +
                         $"{"95% interval",17}");
        foreach (PairStatistics pair in pairs)
        {
            string name = $"{pair.GroupA.Number} - {pair.GroupB.Number}";
            string victoryPointDifference = Formatted(pair.VictoryPointDifference, "+0.00;-0.00;0.00");
            string victoryPointInterval = Interval(pair.VictoryPointDifference,
                                                   pair.VictoryPointDifferenceStandardError, "0.00");
            string winDifference = Formatted(pair.WinRateDifference, "+0.000;-0.000;0.000");
            string winInterval = Interval(pair.WinRateDifference, pair.WinRateDifferenceStandardError, "0.000");
            table.AppendLine($"{name,9}  {pair.Deals,5}  {victoryPointDifference,8}  " +
                             $"{victoryPointInterval,17}{DiffersMark(pair.VictoryPointDifferenceIsSignificant)}" +
                             $" {winDifference,8}  {winInterval,17}{DiffersMark(pair.WinRateDifferenceIsSignificant)}");
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
        string low = Formatted(mean - PairStatistics.ZScore95 * standardError, format);
        string high = Formatted(mean + PairStatistics.ZScore95 * standardError, format);
        return "[" + low + ", " + high + "]";
    }

    /// <summary>The group table as CSV lines, the header first.</summary>
    public static IEnumerable<string> GroupsCsv(IEnumerable<GroupStatistics> groups)
    {
        List<string> lines = new() { string.Join(ResultsCsvWriter.Separator, GroupColumns) };
        foreach (GroupStatistics group in groups)
        {
            lines.Add(Row(group.Number.ToString(InvariantCulture), group.Label, group.Key,
                          group.Games.ToString(InvariantCulture),
                          RoundForOutput(group.Wins), RoundForOutput(group.WinRate),
                          RoundForOutput(group.WinRateLow), RoundForOutput(group.WinRateHigh),
                          RoundForOutput(group.FairShare),
                          RoundForOutput(group.MeanVictoryPoints), RoundForOutput(group.VictoryPointsStandardError),
                          RoundForOutput(group.MeanRank)));
        }
        return lines;
    }

    /// <summary>The paired differences as CSV lines, the header first.</summary>
    public static IEnumerable<string> PairsCsv(IEnumerable<PairStatistics> pairs)
    {
        List<string> lines = new() { string.Join(ResultsCsvWriter.Separator, PairColumns) };
        foreach (PairStatistics pair in pairs)
        {
            double victoryPointMargin = PairStatistics.ZScore95 * pair.VictoryPointDifferenceStandardError;
            double winMargin = PairStatistics.ZScore95 * pair.WinRateDifferenceStandardError;
            lines.Add(Row(pair.GroupA.Number.ToString(InvariantCulture), pair.GroupA.Label,
                          pair.GroupB.Number.ToString(InvariantCulture), pair.GroupB.Label,
                          pair.Deals.ToString(InvariantCulture),
                          RoundForOutput(pair.VictoryPointDifference),
                          RoundForOutput(pair.VictoryPointDifferenceStandardError),
                          RoundForOutput(pair.VictoryPointDifference - victoryPointMargin),
                          RoundForOutput(pair.VictoryPointDifference + victoryPointMargin),
                          RoundForOutput(pair.WinRateDifference), RoundForOutput(pair.WinRateDifferenceStandardError),
                          RoundForOutput(pair.WinRateDifference - winMargin),
                          RoundForOutput(pair.WinRateDifference + winMargin)));
        }
        return lines;
    }

    private static string RoundForOutput(double value)
    {
        return value.ToString("0.######", InvariantCulture);
    }

    private static string Row(params string[] fields)
    {
        return string.Join(ResultsCsvWriter.Separator, fields.Select(ResultsCsvWriter.Escape));
    }
}
