namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>
/// A group's results. Wins counts a k-way tie for first as 1/k (the winShare column). The win rate's interval is
/// the 95% Wilson score interval; FairShare is what a player no better than the others would get (1 / players).
/// </summary>
public sealed class GroupStats
{
    /// <summary>A group's results with these values.</summary>
    public GroupStats(int number, string label, string key, int games, double wins, double winRate,
                      double winLow, double winHigh, double fairShare, double meanVictoryPoints,
                      double victoryPointsStandardError, double meanRank)
    {
        Number = number;
        Label = label;
        Key = key;
        Games = games;
        Wins = wins;
        WinRate = winRate;
        WinLow = winLow;
        WinHigh = winHigh;
        FairShare = fairShare;
        MeanVp = meanVictoryPoints;
        VpSe = victoryPointsStandardError;
        MeanRank = meanRank;
    }

    /// <summary>The group's number in the printed table, from 1.</summary>
    public int Number { get; }

    /// <summary>The group's name as printed.</summary>
    public string Label { get; }

    /// <summary>What the rows of the group have in common (see <see cref="Analysis.KeyOf"/>).</summary>
    public string Key { get; }

    /// <summary>How many rows (seat games) the group has.</summary>
    public int Games { get; }

    /// <summary>The wins, a k-way tie for first counting 1/k.</summary>
    public double Wins { get; }

    /// <summary><see cref="Wins"/> per game.</summary>
    public double WinRate { get; }

    /// <summary>The low end of the win rate's 95% Wilson interval.</summary>
    public double WinLow { get; }

    /// <summary>The high end of the win rate's 95% Wilson interval.</summary>
    public double WinHigh { get; }

    /// <summary>The win rate of a player no better than the others (1 / players).</summary>
    public double FairShare { get; }

    /// <summary>The mean victory points.</summary>
    public double MeanVp { get; }

    /// <summary>The standard error of <see cref="MeanVp"/>.</summary>
    public double VpSe { get; }

    /// <summary>The mean place (1 = first).</summary>
    public double MeanRank { get; }
}
