namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>The mean of some values and the standard error of that mean.</summary>
public sealed class MeanWithStandardError
{
    /// <summary>The <paramref name="mean"/> and its <paramref name="standardError"/>.</summary>
    public MeanWithStandardError(double mean, double standardError)
    {
        Mean = mean;
        StandardError = standardError;
    }

    /// <summary>The mean (NaN for no values).</summary>
    public double Mean { get; }

    /// <summary>The standard error of the mean (0 for fewer than 2 values, NaN for none).</summary>
    public double StandardError { get; }
}
