namespace _7WondersGame.Agents.Mcts.Tree;

/// <summary>One decision of one player at a node: its statistics.</summary>
internal sealed class MoveStatistics
{
    /// <summary>New statistics, with a reward total for each of the <paramref name="players"/> seats.</summary>
    public MoveStatistics(int players)
    {
        Total = new double[players];
    }

    /// <summary>How many times the move was chosen (rollouts backed up through it, plus any virtual loss).</summary>
    public int Visits { get; set; }

    /// <summary>
    /// How many times the move was legal when its node was visited (ISMCTS: it may not be in every guess).
    /// </summary>
    public int Available { get; set; }

    /// <summary>The rewards summed, per seat.</summary>
    public double[] Total { get; }

    /// <summary>The searching player's win shares summed.</summary>
    public double Wins { get; set; }
}
