namespace _7Wonders.Cli.ResultsAnalysis;

/// <summary>The two ends of a confidence interval.</summary>
public sealed class ConfidenceInterval
{
    /// <summary>The interval from <paramref name="low"/> to <paramref name="high"/>.</summary>
    public ConfidenceInterval(double low, double high)
    {
        Low = low;
        High = high;
    }

    /// <summary>The low end.</summary>
    public double Low { get; }

    /// <summary>The high end.</summary>
    public double High { get; }
}
