namespace _7Wonders.Agents.Mcts.Search;

/// <summary>
/// What a rollout gives back up the tree: every seat's reward and the searching player's win share. A struct,
/// because every iteration makes one.
/// </summary>
internal readonly struct RolloutResult
{
    /// <summary>The rewards by seat and the searching player's win share.</summary>
    public RolloutResult(double[] rewardBySeat, double searchingSeatWinShare)
    {
        RewardBySeat = rewardBySeat;
        SearchingSeatWinShare = searchingSeatWinShare;
    }

    /// <summary>Each seat's reward, of the kind the configuration says.</summary>
    public double[] RewardBySeat { get; }

    /// <summary>The searching player's share of the win: 1, a split share for a tie, or 0.</summary>
    public double SearchingSeatWinShare { get; }
}
