namespace _7Wonders.Engine.Games.Dtos;

/// <summary>
/// One player's final standing.
/// Rank 1 is the best; tied players share a rank.
/// WinShare is 1/k for each of the k players tied for first place, otherwise 0.
/// </summary>
public sealed class PlayerStanding
{
    /// <summary>Player <paramref name="id"/>'s standing.</summary>
    public PlayerStanding(int id, int victoryPoints, int coins, int rank, double winShare)
    {
        Id = id;
        VictoryPoints = victoryPoints;
        Coins = coins;
        Rank = rank;
        WinShare = winShare;
    }

    /// <summary>The player's seat.</summary>
    public int Id { get; }

    /// <summary>The player's final victory points.</summary>
    public int VictoryPoints { get; }

    /// <summary>The player's coins at the end (the tie-break).</summary>
    public int Coins { get; }

    /// <summary>The player's place, 1 for the best.</summary>
    public int Rank { get; }

    /// <summary>1/k for each of the k players tied for first place, otherwise 0.</summary>
    public double WinShare { get; }

    /// <summary>A copy of this standing with the win share set.</summary>
    public PlayerStanding WithWinShare(double winShare)
    {
        return new PlayerStanding(Id, VictoryPoints, Coins, Rank, winShare);
    }
}
