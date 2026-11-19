namespace _7Wonders.Cli.ResultsAnalysis;

/// <summary>A group's average victory points and win share on one deal (over its seatings of that deal).</summary>
public sealed class DealAverages
{
    /// <summary>The averages <paramref name="victoryPoints"/> and <paramref name="winShare"/>.</summary>
    public DealAverages(double victoryPoints, double winShare)
    {
        VictoryPoints = victoryPoints;
        WinShare = winShare;
    }

    /// <summary>The average victory points.</summary>
    public double VictoryPoints { get; }

    /// <summary>The average win share.</summary>
    public double WinShare { get; }
}
