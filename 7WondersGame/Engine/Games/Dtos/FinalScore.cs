namespace _7WondersGame.Engine.Games.Dtos;

/// <summary>
/// What a player is ranked by at the end of a game: victory points, then coins. A struct, because MCTS ranks the
/// players at the end of every rollout.
/// </summary>
public readonly struct FinalScore
{
    /// <summary>The seat's victory points and coins.</summary>
    public FinalScore(int seat, int victoryPoints, int coins)
    {
        Seat = seat;
        VictoryPoints = victoryPoints;
        Coins = coins;
    }

    /// <summary>The player's seat.</summary>
    public int Seat { get; }

    /// <summary>The player's victory points.</summary>
    public int VictoryPoints { get; }

    /// <summary>The player's coins (the tie-break).</summary>
    public int Coins { get; }

    /// <summary>
    /// Whether this score ranks above <paramref name="other"/>: more points, or as many and more coins.
    /// </summary>
    public bool Beats(FinalScore other)
    {
        return VictoryPoints > other.VictoryPoints
            || (VictoryPoints == other.VictoryPoints && Coins > other.Coins);
    }
}
