namespace _7WondersGame.src.models.Agents
{
    /// <summary>
    /// Every number <see cref="HeuristicAI"/> uses (G; R10 and Q5 in the critique). Values are in victory points
    /// unless a comment says otherwise. "Old" is the value in DeterministicAI's heuristic where it had the same idea.
    /// The defaults were tuned by sweeping one weight at a time in 3-player games against two DeterministicAI
    /// (1000 to 2000 deals per value; docs/10-ai-players.md has the numbers).
    /// Each one is an agent setting, e.g. <c>--players HeuristicAI:CoinValue=0.5,...</c>, and is logged in the results.
    /// </summary>
    [SettingsGroup]
    public sealed record HeuristicWeights
    {
        // ---------- money ----------

        /// <summary>What a coin spent is worth: a card or wonder stage costs this per coin it costs.</summary>
        public double CoinValue { get; set; } = 0.75;

        /// <summary>
        /// From era 2, extra per coin a card gives (Tavern, Vineyard, Bazaar...) while the player has
        /// <see cref="PoorCoins"/> coins or fewer (old: 2, Vineyard and Bazaar only; 0, off, tuned best).
        /// </summary>
        public double PoorCoinBonus { get; set; } = 0;

        /// <summary>The most coins a player can have and still count as poor (old: 3).</summary>
        public int PoorCoins { get; set; } = 3;

        // ---------- resources ----------

        /// <summary>
        /// A card giving a resource that a wonder stage still to build needs more of than the player and its neighbours
        /// can produce, by era (old: about 2.2 in era 1, 8 to 11 in era 2). Era 3 has no resource cards.
        /// </summary>
        public double WonderResourceEra1 { get; set; } = 6;
        public double WonderResourceEra2 { get; set; } = 9;

        /// <summary>On top of the above, for an either/or card: per choice that the wonder uses (old: 0.5).</summary>
        public double PerUsefulChoice { get; set; } = 1;

        /// <summary>
        /// Less than the above for a raw material when the player already buys raw materials for 1 coin from a neighbour
        /// with <see cref="RawNeighbourPlenty"/> or more of them (old: 1 to 1.5).
        /// </summary>
        public double CheapRawTradePenalty { get; set; } = 1;

        /// <summary>How many raw material cards (the board counted) make a neighbour worth trading with (old: 2).</summary>
        public int RawNeighbourPlenty { get; set; } = 2;

        /// <summary>Era 2: a manufactured good when neither the player nor its neighbours produce any (old: 8).</summary>
        public double NoGoodsNearbyEra2 { get; set; } = 8;

        // ---------- trading discounts (trading posts, Marketplace) ----------

        /// <summary>
        /// A discount card, when the player has few of those resources itself (<see cref="FewOwnRaw"/> or
        /// <see cref="FewOwnGoods"/>, the board counted) and the neighbours have some (old: 2, or 2.3 for the west post).
        /// </summary>
        public double TradeDiscount { get; set; } = 2;

        /// <summary>On top, per card of that kind the neighbours have (old: 1).</summary>
        public double TradeDiscountPerCard { get; set; } = 1;

        public int FewOwnRaw { get; set; } = 2;
        public int FewOwnGoods { get; set; } = 1;

        // ---------- card types ----------

        /// <summary>Eras 1 and 2: a science card (old: 2.3).</summary>
        public double ScienceEarly { get; set; } = 2.3;

        /// <summary>
        /// Instead, the player's first science card when it has 2 or more raw material cards (old: 4 in era 1, 10 in
        /// era 2; in era 2 the same as any science card tuned best).
        /// </summary>
        public double FirstScienceEra1 { get; set; } = 4;
        public double FirstScienceEra2 { get; set; } = 2.3;

        /// <summary>Era 1: a civilian (blue) card (old: -0.7).</summary>
        public double CivilianEra1 { get; set; } = -1.5;

        // ---------- choosing the move ----------

        /// <summary>
        /// A wonder stage, on top of what it adds to the score now: its abilities and progress pay off later (old: none;
        /// DeterministicAI built a stage whenever it could).
        /// </summary>
        public double WonderStage { get; set; } = 2;

        /// <summary>
        /// Taken off what a discard's 3 coins add to the score: a card built is usually worth more later than its
        /// score now shows (old: discarded only when no card could be built).
        /// </summary>
        public double DiscardPenalty { get; set; } = 3;

        /// <summary>
        /// What keeping Olympia A's free card for a later turn is worth, so it goes on an expensive card. It is 0 on the
        /// era's last choice, when the charge would be lost (old: used on the first turn of each era).
        /// </summary>
        public double FreeBuildKeep { get; set; } = 1.5;

        /// <summary>
        /// Per victory point the card would give the player who gets this hand next: taking it away from them is worth
        /// this much (old: only as a tie-break, choosing the neighbour's favourite even when it scored less).
        /// </summary>
        public double Denial { get; set; } = 0.3;
    }
}
