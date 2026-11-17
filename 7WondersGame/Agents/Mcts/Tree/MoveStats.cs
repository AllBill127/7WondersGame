namespace _7WondersGame.Agents.Mcts.Tree;

/// <summary>One decision of one player at a node: its statistics.</summary>
internal sealed class MoveStats
{
    public MoveStats(int players) => Total = new double[players];

    public int Visits;
    /// <summary>How many times the move was legal when its node was visited (ISMCTS: it may not be in every guess).</summary>
    public int Available;
    /// <summary>The rewards summed, per seat.</summary>
    public readonly double[] Total;
    /// <summary>The searching player's win shares summed.</summary>
    public double Wins;
}
