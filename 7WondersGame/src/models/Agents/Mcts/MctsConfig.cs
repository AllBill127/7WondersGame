namespace _7WondersGame.src.models.Agents.Mcts
{
    /// <summary>What the search may know (M1).</summary>
    public enum MctsInformation
    {
        /// <summary>SO-ISMCTS: each iteration searches a new guess at the hidden cards (<see cref="Determinization"/>).</summary>
        Determinized,
        /// <summary>Searches the real game, hidden cards included: a cheating baseline.</summary>
        PerfectInformation,
    }

    /// <summary>How the tree handles moves made at the same time (Q16, M7).</summary>
    public enum MctsTree
    {
        /// <summary>One player per node: the searching player first, then the others in seat order (they see its move).</summary>
        Sequential,
        /// <summary>Decoupled UCT: one node per turn, each player choosing from its own statistics.</summary>
        Duct,
    }

    /// <summary>Whose reward a player maximises when choosing in the tree (M3).</summary>
    public enum MctsBackup
    {
        /// <summary>max^n: every player maximises its own reward.</summary>
        MaxN,
        /// <summary>The opponents minimise the searching player's reward.</summary>
        Paranoid,
        /// <summary>Every player maximises the searching player's reward (the old v3-v12, "cooperative").</summary>
        OwnScore,
    }

    /// <summary>The reward of a finished rollout, per player (M4).</summary>
    public enum MctsReward
    {
        /// <summary>1 for a win, 1/k for a k-way tie for first (VP, then coins), else 0.</summary>
        Win,
        /// <summary>The player's VP minus the best opponent's VP (not scaled, so C must be about 20 times larger).</summary>
        VpMargin,
        /// <summary>The player's VP (not scaled, as the old versions).</summary>
        RawVp,
    }

    /// <summary>The tree policy (M4). Moves not tried yet are always tried first.</summary>
    public enum MctsExploration
    {
        /// <summary>UCB1: mean + C √(ln n / nᵢ), n = how often the move was available (ISMCTS).</summary>
        Ucb1,
        /// <summary>The old +1 smoothing: total / (nᵢ + 1) + C √(ln(n + 1) / (nᵢ + 1)).</summary>
        Smoothed,
        /// <summary>The old diminishing exploration: UCB1 with C / (1 + ln(1 + nᵢ)) in place of C.</summary>
        Diminishing,
    }

    /// <summary>Which of the searching player's moves is played after the search (M4).</summary>
    public enum MctsFinalMove
    {
        /// <summary>The most visited (robust child).</summary>
        MostVisits,
        /// <summary>The highest total reward (a sum, as the old versions).</summary>
        HighestReward,
        /// <summary>The most wins (win shares summed), whatever the reward.</summary>
        MostWins,
    }

    /// <summary>How the players move in a rollout (M8).</summary>
    public enum MctsRollout
    {
        /// <summary><see cref="HeuristicAI"/>'s move, or with probability ε a random move.</summary>
        EpsilonHeuristic,
        /// <summary>Always <see cref="HeuristicAI"/>'s move (deterministic, like the old DDK rollouts).</summary>
        Heuristic,
        /// <summary>A random action, then a random card for it (as <see cref="RandomChoiceAI"/>).</summary>
        Random,
    }

    /// <summary>Where a rollout stops (M8).</summary>
    public enum MctsRolloutEnd
    {
        /// <summary>At the end of the game.</summary>
        GameEnd,
        /// <summary>At the end of the era it started in; the reward uses the scores at that point.</summary>
        EraEnd,
    }

    /// <summary>Which moves the tree considers (M10).</summary>
    public enum MctsPruning
    {
        /// <summary>Every legal move.</summary>
        None,
        /// <summary>One wonder-stage move and one discard, each with a random hand card (<see cref="Game.LegalMovesPruned"/>).</summary>
        RandomCard,
    }

    /// <summary>When the search stops (M9).</summary>
    public enum MctsBudget
    {
        /// <summary>After <see cref="MctsConfig.Iterations"/> iterations; every iteration counts. Reproducible.</summary>
        Iterations,
        /// <summary>After <see cref="MctsConfig.TimeMs"/> milliseconds. Not reproducible: it depends on the machine.</summary>
        Time,
    }

    /// <summary>
    /// Every setting of <see cref="MctsAgent"/> (fix plan step 6, E + D1). Each is a setting on the command line,
    /// e.g. <c>MctsAgent:C=1.2:Backup=Paranoid</c>, and is logged in the results (configJson). The defaults are the
    /// fix plan's; C is still to be tuned (in [0.3, 2]).
    /// </summary>
    [SettingsGroup]
    public sealed record MctsConfig
    {
        public MctsInformation Information { get; set; } = MctsInformation.Determinized;
        public MctsTree Tree { get; set; } = MctsTree.Sequential;
        public MctsBackup Backup { get; set; } = MctsBackup.MaxN;
        public MctsReward Reward { get; set; } = MctsReward.Win;
        public MctsExploration Exploration { get; set; } = MctsExploration.Ucb1;

        /// <summary>The exploration constant.</summary>
        public double C { get; set; } = 0.7;

        public MctsFinalMove FinalMove { get; set; } = MctsFinalMove.MostVisits;
        public MctsRollout Rollout { get; set; } = MctsRollout.EpsilonHeuristic;

        /// <summary>For <see cref="MctsRollout.EpsilonHeuristic"/>: the chance of a random move.</summary>
        public double Epsilon { get; set; } = 0.1;

        public MctsRolloutEnd RolloutEnd { get; set; } = MctsRolloutEnd.GameEnd;
        public MctsPruning Pruning { get; set; } = MctsPruning.None;
        public MctsBudget Budget { get; set; } = MctsBudget.Iterations;

        /// <summary>Iterations per decision (selection, expansion, one rollout, backup), for <see cref="MctsBudget.Iterations"/>.</summary>
        public int Iterations { get; set; } = 1000;

        /// <summary>Milliseconds per decision, for <see cref="MctsBudget.Time"/>.</summary>
        public int TimeMs { get; set; } = 1000;
    }
}
