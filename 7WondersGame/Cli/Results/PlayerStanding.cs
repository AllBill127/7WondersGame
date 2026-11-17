namespace _7WondersGame.Cli.Results;

/// <summary>
/// One player's final standing.
/// Rank 1 is the best; tied players share a rank.
/// WinShare is 1/k for each of the k players tied for first place, otherwise 0.
/// </summary>
public record PlayerStanding(int Id, int VictoryPoints, int Coins, int Rank, double WinShare);
