namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>
/// A paired comparison of groups A and B on the deals both played (the seed column: same seed, same deal).
/// Per deal, each group's VP and win share are averaged over its rows; the differences A - B are then averaged
/// over the deals. Se is the standard error of that mean, and the 95% interval is mean +- 1.96 Se.
/// </summary>
public sealed record PairStats(GroupStats A, GroupStats B, int Deals, double VpDiff, double VpDiffSe,
                               double WinDiff, double WinDiffSe)
{
    public const double Z95 = 1.959964;
    public bool VpDiffers => Deals > 1 && Math.Abs(VpDiff) > Z95 * VpDiffSe;
    public bool WinDiffers => Deals > 1 && Math.Abs(WinDiff) > Z95 * WinDiffSe;
}
