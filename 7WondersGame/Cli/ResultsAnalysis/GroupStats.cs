namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>
/// A group's results. Wins counts a k-way tie for first as 1/k (the winShare column). The win rate's interval is
/// the 95% Wilson score interval; FairShare is what a player no better than the others would get (1 / players).
/// </summary>
public sealed record GroupStats(int Number, string Label, string Key, int Games, double Wins, double WinRate,
                                double WinLow, double WinHigh, double FairShare, double MeanVp, double VpSe, double MeanRank);
