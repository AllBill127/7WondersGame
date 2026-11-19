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
    // Bit i stands for the Resource with value i: Wood, Ore, Clay, Stone, Loom, Glass, Papyrus.
    private const int Goods = 7;
    private const int Raw = 0b0001111;
    private const int Manufactured = 0b1110000;

    private const int StandardTradePrice = 2;
    private const int DiscountedTradePrice = 1;

    private readonly List<int> ownSources = new();
    private readonly List<int> sellerSources = new();   // west neighbour's first, then east's
    private int westSourceCount;
    private readonly int[] westSupply = new int[Goods];
    private readonly int[] eastSupply = new int[Goods];
    private readonly int[] westPrice = new int[Goods];
    private readonly int[] eastPrice = new int[Goods];
    private int bestTotal;
    private int bestEast;

    /// <summary>
    /// How many units of each good (indexed by <see cref="Resource"/> value) the buyer still has to find.
    /// <see cref="PaymentPlanner"/> fills it before <see cref="Run"/>; the search changes it while it runs and
    /// leaves it as it found it.
    /// </summary>
    public int[] Need { get; } = new int[Goods];

    /// <summary>The cheapest plan for what <see cref="Need"/> holds, or null.</summary>
    public PaymentPlan? Run(PlayerState buyer)
    {
        Reset(buyer);
        UseOwn(0);
        if (bestTotal == int.MaxValue)
        {
            return null;
        }
        int bestWest = bestTotal - bestEast;
        return new PaymentPlan(bestWest, bestEast);
    }

    private void Reset(PlayerState buyer)
    {
        ownSources.Clear();
        sellerSources.Clear();
        bestTotal = int.MaxValue;
        bestEast = int.MaxValue;

        CollectOwnSources(buyer);

        AddSeller(buyer.WestNeighbour, westSupply);
        westSourceCount = sellerSources.Count;
        AddSeller(buyer.EastNeighbour, eastSupply);

        for (int goodIndex = 0; goodIndex < Goods; goodIndex++)
        {
            bool isRaw = (Raw & (1 << goodIndex)) != 0;
            westPrice[goodIndex] = TradePrice(buyer, isRaw, Ability.RawMaterialsCheapFromWestNeighbour);
            eastPrice[goodIndex] = TradePrice(buyer, isRaw, Ability.RawMaterialsCheapFromEastNeighbour);
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
                ownSources.Add(choiceMask);
            }
        }
        if (PlayerAbilities.Has(buyer, Ability.AnyRawMaterialEachTurn))
        {
            ownSources.Add(Raw);
        }
        if (PlayerAbilities.Has(buyer, Ability.AnyManufacturedGoodEachTurn))
        {
            ownSources.Add(Manufactured);
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
    /// their sellable either/or cards in <see cref="sellerSources"/>.
    /// </summary>
    private void AddSeller(PlayerState seller, int[] supply)
    {
        for (int goodIndex = 0; goodIndex < Goods; goodIndex++)
        {
            supply[goodIndex] = seller.Resources[(Resource)goodIndex];
        }
        foreach (Card card in seller.BuiltCards)
        {
            int choiceMask = card.SellableChoiceMask;
            if (choiceMask != 0)
            {
                sellerSources.Add(choiceMask);
            }
        }
    }

    /// <summary>A mask of the goods the buyer still needs at least one unit of.</summary>
    private int NeedMask()
    {
        int neededMask = 0;
        for (int goodIndex = 0; goodIndex < Goods; goodIndex++)
        {
            if (Need[goodIndex] > 0)
            {
                neededMask |= 1 << goodIndex;
            }
        }
        return neededMask;
    }

    /// <summary>
    /// Gives each of the buyer's own either/or sources to one still-needed resource, trying every choice.
    /// A free unit never makes a plan dearer, so a source is only left unused when it offers nothing needed.
    /// </summary>
    private void UseOwn(int sourceIndex)
    {
        if (bestTotal == 0)
        {
            return;
        }
        int needed = NeedMask();
        if (needed == 0)
        {
            Consider(0, 0);
            return;
        }
        if (sourceIndex == ownSources.Count)
        {
            UseSellers(0);
            return;
        }

        int nextSource = sourceIndex + 1;
        int options = ownSources[sourceIndex] & needed;
        if (options == 0)
        {
            UseOwn(nextSource);
            return;
        }
        for (int goodIndex = 0; goodIndex < Goods; goodIndex++)
        {
            if ((options & (1 << goodIndex)) == 0)
            {
                continue;
            }
            Need[goodIndex]--;
            UseOwn(nextSource);
            Need[goodIndex]++;
        }
    }

    /// <summary>
    /// Gives each neighbour either/or source to one resource the buyer still needs and that neighbour
    /// doesn't already offer enough of, trying every choice; then prices the purchase.
    /// </summary>
    private void UseSellers(int sourceIndex)
    {
        if (sourceIndex == sellerSources.Count)
        {
            Price();
            return;
        }

        int nextSource = sourceIndex + 1;
        int[] supply = SupplyOfSourceOwner(sourceIndex);
        bool any = false;
        for (int goodIndex = 0; goodIndex < Goods; goodIndex++)
        {
            if ((sellerSources[sourceIndex] & (1 << goodIndex)) == 0 || supply[goodIndex] >= Need[goodIndex])
            {
                continue;
            }
            any = true;
            supply[goodIndex]++;
            UseSellers(nextSource);
            supply[goodIndex]--;
        }
        if (!any)
        {
            UseSellers(nextSource);
        }
    }

    /// <summary>The supply of the neighbour who owns seller source <paramref name="sourceIndex"/>.</summary>
    private int[] SupplyOfSourceOwner(int sourceIndex)
    {
        if (sourceIndex < westSourceCount)
        {
            return westSupply;
        }
        return eastSupply;
    }

    /// <summary>Buys each missing resource from the cheaper neighbour first (west when equal).</summary>
    private void Price()
    {
        int toWest = 0;
        int toEast = 0;
        for (int goodIndex = 0; goodIndex < Goods; goodIndex++)
        {
            int missingCount = Need[goodIndex];
            if (missingCount == 0)
            {
                continue;
            }

            if (westPrice[goodIndex] <= eastPrice[goodIndex])
            {
                int fromWest = Math.Min(missingCount, westSupply[goodIndex]);
                int fromEast = missingCount - fromWest;
                if (fromEast > eastSupply[goodIndex])
                {
                    return;
                }
                toWest += fromWest * westPrice[goodIndex];
                toEast += fromEast * eastPrice[goodIndex];
            }
            else
            {
                int fromEast = Math.Min(missingCount, eastSupply[goodIndex]);
                int fromWest = missingCount - fromEast;
                if (fromWest > westSupply[goodIndex])
                {
                    return;
                }
                toEast += fromEast * eastPrice[goodIndex];
                toWest += fromWest * westPrice[goodIndex];
            }
        }
        Consider(toWest + toEast, toEast);
    }

    /// <summary>Keeps the plan if it is cheaper than the best so far, or as cheap and pays less to the east.</summary>
    private void Consider(int total, int toEast)
    {
        bool isCheaper = total < bestTotal;
        bool isAsCheapButPaysLessEast = total == bestTotal && toEast < bestEast;
        if (isCheaper || isAsCheapButPaysLessEast)
        {
            bestTotal = total;
            bestEast = toEast;
        }
    }
}
