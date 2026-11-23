using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Payment.Dtos;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;

namespace _7Wonders.Engine.Payment.Services;

/// <summary>
/// The search behind <see cref="PaymentPlanner"/>: tries every use of the either/or sources (the buyer's own,
/// then the neighbours') and prices each outcome. One instance per thread is reused, so it allocates nothing
/// per search.
/// </summary>
internal sealed class PaymentSearch
{
    // A set of goods is kept as one int, a "mask": bit i is 1 when the Resource with value i is in the set (Wood is
    // bit 0, Ore 1, Clay 2, Stone 3, Loom 4, Glass 5, Papyrus 6). The binary literals below read right to left:
    // 0b0001111 has bits 0-3 set (the raw materials), 0b1110000 bits 4-6 (the manufactured goods). A Clay/Ore card is
    // 0b0000110. `1 << i` is the mask of good i alone; `mask & other` keeps the goods in both sets; `mask | other`
    // joins two sets; `(mask & (1 << i)) != 0` asks whether good i is in the set.
    private const int GoodCount = 7;
    private const int RawMaterialsMask = 0b0001111;
    private const int ManufacturedGoodsMask = 0b1110000;

    private const int StandardTradePrice = 2;
    private const int DiscountedTradePrice = 1;

    private readonly List<int> _ownSources = new();
    private readonly List<int> _sellerSources = new();   // west neighbour's first, then east's
    private int _westSourceCount;
    private readonly int[] _westSupply = new int[GoodCount];
    private readonly int[] _eastSupply = new int[GoodCount];
    private readonly int[] _westPrice = new int[GoodCount];
    private readonly int[] _eastPrice = new int[GoodCount];
    private int _bestTotalCost;
    private int _bestCostToEast;
    private BoughtGoods _bestBoughtFromWest;
    private BoughtGoods _bestBoughtFromEast;

    /// <summary>
    /// How many units of each good (indexed by <see cref="Resource"/> value) the buyer still has to find.
    /// <see cref="PaymentPlanner"/> fills it before <see cref="FindCheapestPlan"/>; the search changes it while
    /// it runs and leaves it as it found it.
    /// </summary>
    public int[] NeededGoodCounts { get; } = new int[GoodCount];

    /// <summary>The cheapest plan for what <see cref="NeededGoodCounts"/> holds, or null.</summary>
    public PaymentPlan? FindCheapestPlan(PlayerState buyer)
    {
        ResetFor(buyer);
        UseOwnSources(0);
        if (_bestTotalCost == int.MaxValue)
        {
            return null;
        }
        int bestCostToWest = _bestTotalCost - _bestCostToEast;
        return new PaymentPlan(bestCostToWest, _bestCostToEast, _bestBoughtFromWest, _bestBoughtFromEast);
    }

    private void ResetFor(PlayerState buyer)
    {
        _ownSources.Clear();
        _sellerSources.Clear();
        _bestTotalCost = int.MaxValue;
        _bestCostToEast = int.MaxValue;

        CollectOwnSources(buyer);

        AddSeller(buyer.WestNeighbour, _westSupply);
        _westSourceCount = _sellerSources.Count;
        AddSeller(buyer.EastNeighbour, _eastSupply);

        // Whether good i is a raw material: its bit is set in the raw materials' mask.
        for (int goodIndex = 0; goodIndex < GoodCount; goodIndex++)
        {
            bool isRaw = (RawMaterialsMask & (1 << goodIndex)) != 0;
            _westPrice[goodIndex] = TradePrice(buyer, isRaw, Ability.RawMaterialsCheapFromWestNeighbour);
            _eastPrice[goodIndex] = TradePrice(buyer, isRaw, Ability.RawMaterialsCheapFromEastNeighbour);
        }
    }

    /// <summary>The buyer's either/or cards and wildcards, one entry (a mask of goods) per unit they give.</summary>
    private void CollectOwnSources(PlayerState buyer)
    {
        foreach (Card card in buyer.BuiltCards)
        {
            int choiceMask = card.ChoiceMask;
            if (choiceMask != 0)
            {
                _ownSources.Add(choiceMask);
            }
        }
        if (PlayerAbilities.Has(buyer, Ability.AnyRawMaterialEachTurn))
        {
            _ownSources.Add(RawMaterialsMask);
        }
        if (PlayerAbilities.Has(buyer, Ability.AnyManufacturedGoodEachTurn))
        {
            _ownSources.Add(ManufacturedGoodsMask);
        }
    }

    /// <summary>
    /// What one unit of a good costs from one neighbour. <paramref name="rawTradingPost"/> is the trading post that
    /// makes raw goods cheaper from that side (Olympia B makes them cheaper from both).
    /// </summary>
    private static int TradePrice(PlayerState buyer, bool isRaw, Ability rawTradingPost)
    {
        if (isRaw)
        {
            // `|` joins the two abilities into one set; Has is true when the buyer has either of them.
            if (PlayerAbilities.Has(buyer, rawTradingPost | Ability.RawMaterialsCheapFromBothNeighbours))
            {
                return DiscountedTradePrice;
            }
            return StandardTradePrice;
        }
        if (PlayerAbilities.Has(buyer, Ability.ManufacturedGoodsCheapFromBothNeighbours))
        {
            return DiscountedTradePrice;
        }
        return StandardTradePrice;
    }

    /// <summary>
    /// Records what <paramref name="seller"/> can sell: their fixed production in <paramref name="supply"/>, and
    /// their sellable either/or cards in <see cref="_sellerSources"/>.
    /// </summary>
    private void AddSeller(PlayerState seller, int[] supply)
    {
        for (int goodIndex = 0; goodIndex < GoodCount; goodIndex++)
        {
            supply[goodIndex] = seller.Resources[(Resource)goodIndex];
        }
        foreach (Card card in seller.BuiltCards)
        {
            int choiceMask = card.SellableChoiceMask;
            if (choiceMask != 0)
            {
                _sellerSources.Add(choiceMask);
            }
        }
    }

    /// <summary>A mask of the goods the buyer still needs at least one unit of.</summary>
    private int NeededGoodsMask()
    {
        int neededMask = 0;
        for (int goodIndex = 0; goodIndex < GoodCount; goodIndex++)
        {
            if (NeededGoodCounts[goodIndex] > 0)
            {
                // Adds good i to the set: `|=` sets its bit and leaves the others as they were.
                neededMask |= 1 << goodIndex;
            }
        }
        return neededMask;
    }

    /// <summary>
    /// Gives each of the buyer's own either/or sources to one still-needed resource, trying every choice.
    /// A free unit never makes a plan dearer, so a source is only left unused when it offers nothing needed.
    /// </summary>
    /// <remarks>
    /// A depth-first search by recursion: for source number <paramref name="sourceIndex"/> it tries each good the
    /// source can give, lowers that good's need by one, lets the call for the next source try all of its choices,
    /// and then raises the need again (backtracking), so the next choice starts from the same state. With two
    /// Clay/Ore sources and a need of 1 Clay and 1 Ore it tries Clay+Clay, Clay+Ore, Ore+Clay and Ore+Ore. After the
    /// last own source it goes on to the neighbours' sources the same way, and each complete set of choices is
    /// priced. Counting down and back up in one array, instead of copying it for each try, allocates nothing.
    /// </remarks>
    private void UseOwnSources(int sourceIndex)
    {
        if (_bestTotalCost == 0)
        {
            return;
        }
        int neededMask = NeededGoodsMask();
        if (neededMask == 0)
        {
            KeepIfCheapest(0, 0, BoughtGoods.None, BoughtGoods.None);
            return;
        }
        if (sourceIndex == _ownSources.Count)
        {
            UseSellerSources(0);
            return;
        }

        int nextSource = sourceIndex + 1;
        // The goods this source offers that are still needed (bits set in both masks).
        int usableMask = _ownSources[sourceIndex] & neededMask;
        if (usableMask == 0)
        {
            UseOwnSources(nextSource);
            return;
        }
        for (int goodIndex = 0; goodIndex < GoodCount; goodIndex++)
        {
            if ((usableMask & (1 << goodIndex)) == 0)
            {
                continue;
            }
            NeededGoodCounts[goodIndex]--;
            UseOwnSources(nextSource);
            NeededGoodCounts[goodIndex]++;
        }
    }

    /// <summary>
    /// Gives each neighbour either/or source to one resource the buyer still needs and that neighbour
    /// doesn't already offer enough of, trying every choice; then prices the purchase.
    /// </summary>
    private void UseSellerSources(int sourceIndex)
    {
        if (sourceIndex == _sellerSources.Count)
        {
            PriceRemainingPurchases();
            return;
        }

        int nextSource = sourceIndex + 1;
        int[] supply = SupplyOfSourceOwner(sourceIndex);
        bool gaveAnyGood = false;
        for (int goodIndex = 0; goodIndex < GoodCount; goodIndex++)
        {
            // Whether good i is one this source can give: its bit is set in the source's mask.
            bool sourceGivesGood = (_sellerSources[sourceIndex] & (1 << goodIndex)) != 0;
            bool supplyAlreadyEnough = supply[goodIndex] >= NeededGoodCounts[goodIndex];
            if (!sourceGivesGood || supplyAlreadyEnough)
            {
                continue;
            }
            // Adds the unit to the neighbour's supply, searches on, and takes it back (as in UseOwnSources).
            gaveAnyGood = true;
            supply[goodIndex]++;
            UseSellerSources(nextSource);
            supply[goodIndex]--;
        }
        if (!gaveAnyGood)
        {
            UseSellerSources(nextSource);
        }
    }

    /// <summary>The supply of the neighbour who owns seller source <paramref name="sourceIndex"/>.</summary>
    private int[] SupplyOfSourceOwner(int sourceIndex)
    {
        if (sourceIndex < _westSourceCount)
        {
            return _westSupply;
        }
        return _eastSupply;
    }

    /// <summary>Buys each missing resource from the cheaper neighbour first (west when equal).</summary>
    private void PriceRemainingPurchases()
    {
        int toWest = 0;
        int toEast = 0;
        BoughtGoods boughtFromWest = BoughtGoods.None;
        BoughtGoods boughtFromEast = BoughtGoods.None;
        for (int goodIndex = 0; goodIndex < GoodCount; goodIndex++)
        {
            int missingCount = NeededGoodCounts[goodIndex];
            if (missingCount == 0)
            {
                continue;
            }

            if (_westPrice[goodIndex] <= _eastPrice[goodIndex])
            {
                int fromWest = Math.Min(missingCount, _westSupply[goodIndex]);
                int fromEast = missingCount - fromWest;
                if (fromEast > _eastSupply[goodIndex])
                {
                    return;
                }
                toWest += fromWest * _westPrice[goodIndex];
                toEast += fromEast * _eastPrice[goodIndex];
                boughtFromWest = boughtFromWest.With((Resource)goodIndex, fromWest);
                boughtFromEast = boughtFromEast.With((Resource)goodIndex, fromEast);
            }
            else
            {
                int fromEast = Math.Min(missingCount, _eastSupply[goodIndex]);
                int fromWest = missingCount - fromEast;
                if (fromWest > _westSupply[goodIndex])
                {
                    return;
                }
                toEast += fromEast * _eastPrice[goodIndex];
                toWest += fromWest * _westPrice[goodIndex];
                boughtFromEast = boughtFromEast.With((Resource)goodIndex, fromEast);
                boughtFromWest = boughtFromWest.With((Resource)goodIndex, fromWest);
            }
        }
        KeepIfCheapest(toWest + toEast, toEast, boughtFromWest, boughtFromEast);
    }

    /// <summary>Keeps the plan if it is cheaper than the best so far, or as cheap and pays less to the east.</summary>
    private void KeepIfCheapest(int total, int toEast, BoughtGoods boughtFromWest, BoughtGoods boughtFromEast)
    {
        bool isCheaper = total < _bestTotalCost;
        bool isAsCheapButPaysLessEast = total == _bestTotalCost && toEast < _bestCostToEast;
        if (isCheaper || isAsCheapButPaysLessEast)
        {
            _bestTotalCost = total;
            _bestCostToEast = toEast;
            _bestBoughtFromWest = boughtFromWest;
            _bestBoughtFromEast = boughtFromEast;
        }
    }
}
