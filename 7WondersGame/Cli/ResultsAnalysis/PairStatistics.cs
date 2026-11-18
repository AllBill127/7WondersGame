namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>
/// A paired comparison of groups A and B on the deals both played (the seed column: same seed, same deal).
/// Per deal, each group's VP and win share are averaged over its rows; the differences A - B are then averaged
/// over the deals. The standard error is that of the mean, and the 95% interval is mean +- 1.96 standard errors.
/// </summary>
public sealed class PairStatistics
{
    /// <summary>A compared with B over <paramref name="deals"/> shared deals.</summary>
    public PairStatistics(GroupStatistics groupA, GroupStatistics groupB, int deals, double victoryPointDifference,
                     double victoryPointDifferenceStandardError, double winRateDifference,
                     double winRateDifferenceStandardError)
    {
        GroupA = groupA;
        GroupB = groupB;
        Deals = deals;
        VictoryPointDifference = victoryPointDifference;
        VictoryPointDifferenceStandardError = victoryPointDifferenceStandardError;
        WinRateDifference = winRateDifference;
        WinRateDifferenceStandardError = winRateDifferenceStandardError;
    }

    /// <summary>
    /// The 97.5% point of the standard normal distribution: a 95% interval is mean +- this many standard errors.
    /// </summary>
    public const double ZScore95 = 1.959964;

    /// <summary>The first group.</summary>
    public GroupStatistics GroupA { get; }

    /// <summary>The group compared with it.</summary>
    public GroupStatistics GroupB { get; }

    /// <summary>How many deals both groups played.</summary>
    public int Deals { get; }

    /// <summary>The mean of A's VP minus B's VP over the shared deals.</summary>
    public double VictoryPointDifference { get; }

    /// <summary>The standard error of <see cref="VictoryPointDifference"/>.</summary>
    public double VictoryPointDifferenceStandardError { get; }

    /// <summary>The mean of A's win share minus B's over the shared deals.</summary>
    public double WinRateDifference { get; }

    /// <summary>The standard error of <see cref="WinRateDifference"/>.</summary>
    public double WinRateDifferenceStandardError { get; }

    /// <summary>Whether the VP difference's 95% interval leaves out 0.</summary>
    public bool VictoryPointDifferenceIsSignificant
    {
        get
        {
            return Deals > 1 && Math.Abs(VictoryPointDifference) > ZScore95 * VictoryPointDifferenceStandardError;
        }
    }

    /// <summary>Whether the win-share difference's 95% interval leaves out 0.</summary>
    public bool WinRateDifferenceIsSignificant
    {
        get
        {
            return Deals > 1 && Math.Abs(WinRateDifference) > ZScore95 * WinRateDifferenceStandardError;
        }
    }
}
