using _7Wonders.Agents.Common.Settings;

namespace _7Wonders.Agents.Heuristic;

/// <summary>
/// Every number <see cref="HeuristicAgent"/> uses. Values are in victory points unless a comment says otherwise.
/// The defaults were tuned by sweeping one weight at a time in 3-player games (1000 to 2000 deals per value;
/// docs/10-ai-players.md has the numbers).
/// Each one is an agent setting (<see cref="HeuristicAgent.Weights"/> is a setting group), e.g.
/// <c>--players HeuristicAgent:CoinValueInVictoryPoints=0.5,...</c>, and is logged in the results.
/// </summary>
internal sealed class HeuristicWeights
{
    // ---------- money ----------

    /// <summary>What a coin spent is worth: a card or wonder stage costs this per coin it costs.</summary>
    [Setting]
    public double CoinValueInVictoryPoints { get; set; } = 0.75;

    /// <summary>
    /// From era 2, extra per coin a card gives (Tavern, Vineyard, Bazaar...) while the player has
    /// <see cref="PoorCoinThreshold"/> coins or fewer (0, off, tuned best).
    /// </summary>
    [Setting]
    public double PoorPlayerCoinGainBonus { get; set; } = 0;

    /// <summary>The most coins a player can have and still count as poor.</summary>
    [Setting]
    public int PoorCoinThreshold { get; set; } = 3;

    // ---------- resources ----------

    /// <summary>
    /// A card giving a resource that a wonder stage still to build needs more of than the player and its neighbours
    /// can produce, by era. Era 3 has no resource cards.
    /// </summary>
    [Setting]
    public double MissingWonderResourceBonusEra1 { get; set; } = 6;

    /// <summary>The same in era 2.</summary>
    [Setting]
    public double MissingWonderResourceBonusEra2 { get; set; } = 9;

    /// <summary>On top of the above, for an either/or card: per choice that the wonder uses.</summary>
    [Setting]
    public double BonusPerUsefulResourceChoice { get; set; } = 1;

    /// <summary>
    /// Less than the above for a raw material when the player already buys raw materials for 1 coin from a neighbour
    /// with <see cref="NeighbourRawMaterialCardsForCheapTrade"/> or more of them.
    /// </summary>
    [Setting]
    public double CheapRawMaterialTradePenalty { get; set; } = 1;

    /// <summary>How many raw material cards (the board counted) make a neighbour worth trading with.</summary>
    [Setting]
    public int NeighbourRawMaterialCardsForCheapTrade { get; set; } = 2;

    /// <summary>Era 2: a manufactured good when neither the player nor its neighbours produce any.</summary>
    [Setting]
    public double NoManufacturedGoodsNearbyBonusEra2 { get; set; } = 8;

    // ---------- trading discounts (trading posts, Marketplace) ----------

    /// <summary>
    /// A discount card, when the player has few of those resources itself (<see
    /// cref="FewOwnRawMaterialCardsThreshold"/> or <see cref="FewOwnManufacturedGoodsCardsThreshold"/>, the board
    /// counted) and the neighbours have some.
    /// </summary>
    [Setting]
    public double TradingDiscountBonus { get; set; } = 2;

    /// <summary>On top, per card of that kind the neighbours have.</summary>
    [Setting]
    public double TradingDiscountBonusPerNeighbourCard { get; set; } = 1;

    /// <summary>
    /// The most raw material cards (the board counted) a player can have and still gain from a trading post.
    /// </summary>
    [Setting]
    public int FewOwnRawMaterialCardsThreshold { get; set; } = 2;

    /// <summary>
    /// The most good cards (the board counted) a player can have and still gain from the Marketplace.
    /// </summary>
    [Setting]
    public int FewOwnManufacturedGoodsCardsThreshold { get; set; } = 1;

    // ---------- card types ----------

    /// <summary>Eras 1 and 2: a science card.</summary>
    [Setting]
    public double EarlyScienceCardBonus { get; set; } = 2.3;

    /// <summary>
    /// Instead, the player's first science card when it has 2 or more raw material cards (in era 2 the same as any
    /// science card tuned best).
    /// </summary>
    [Setting]
    public double FirstScienceCardBonusEra1 { get; set; } = 4;

    /// <summary>The same in era 2.</summary>
    [Setting]
    public double FirstScienceCardBonusEra2 { get; set; } = 2.3;

    /// <summary>Era 1: a civilian (blue) card.</summary>
    [Setting]
    public double CivilianCardBonusEra1 { get; set; } = -1.5;

    // ---------- choosing the move ----------

    /// <summary>
    /// A wonder stage, on top of what it adds to the score now: its abilities and progress pay off later.
    /// </summary>
    [Setting]
    public double WonderStageBonus { get; set; } = 2;

    /// <summary>
    /// Taken off what a discard's 3 coins add to the score: a card built is usually worth more later than its
    /// score now shows.
    /// </summary>
    [Setting]
    public double DiscardPenalty { get; set; } = 3;

    /// <summary>
    /// What keeping Olympia A's free card for a later turn is worth, so it goes on an expensive card. It is 0 on the
    /// era's last choice, when the charge would be lost.
    /// </summary>
    [Setting]
    public double KeepFreeBuildValue { get; set; } = 1.5;

    /// <summary>
    /// Per victory point the card would give the player who gets this hand next: taking it away from them is worth
    /// this much.
    /// </summary>
    [Setting]
    public double NextPlayerDenialWeight { get; set; } = 0.3;
}
