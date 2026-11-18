namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>
/// A paired comparison of groups A and B on the deals both played (the seed column: same seed, same deal).
/// Per deal, each group's VP and win share are averaged over its rows; the differences A - B are then averaged
/// over the deals. Se is the standard error of that mean, and the 95% interval is mean +- 1.96 Se.
/// </summary>
public sealed class PairStats
{
    /// <summary>A compared with B over <paramref name="deals"/> shared deals.</summary>
    public PairStats(GroupStats groupA, GroupStats groupB, int deals, double victoryPointDifference,
                     double victoryPointDifferenceStandardError, double winDifference,
                     double winDifferenceStandardError)
    {
        A = groupA;
        B = groupB;
        Deals = deals;
        VpDiff = victoryPointDifference;
        VpDiffSe = victoryPointDifferenceStandardError;
        WinDiff = winDifference;
        WinDiffSe = winDifferenceStandardError;
    }

    /// <summary>The 97.5% point of the standard normal distribution: a 95% interval is mean +- this many Se.</summary>
    public const double Z95 = 1.959964;

    /// <summary>The first group.</summary>
    public GroupStats A { get; }

    /// <summary>The group compared with it.</summary>
    public GroupStats B { get; }

    /// <summary>How many deals both groups played.</summary>
    public int Deals { get; }

    /// <summary>The mean of A's VP minus B's VP over the shared deals.</summary>
    public double VpDiff { get; }

    /// <summary>The standard error of <see cref="VpDiff"/>.</summary>
    public double VpDiffSe { get; }

    /// <summary>The mean of A's win share minus B's over the shared deals.</summary>
    public double WinDiff { get; }

    /// <summary>The standard error of <see cref="WinDiff"/>.</summary>
    public double WinDiffSe { get; }

    /// <summary>Whether the VP difference's 95% interval leaves out 0.</summary>
    public bool VpDiffers
    {
        get
        {
            return Deals > 1 && Math.Abs(VpDiff) > Z95 * VpDiffSe;
        }
    }

    /// <summary>Whether the win-share difference's 95% interval leaves out 0.</summary>
    public bool WinDiffers
    {
        get
        {
            return Deals > 1 && Math.Abs(WinDiff) > Z95 * WinDiffSe;
        }
    }
}
