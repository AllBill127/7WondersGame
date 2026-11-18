using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Payment.Dtos;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Engine.Payment.Services;

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

        AddSeller(buyer.PlayerWest, westSupply);
        westSourceCount = sellerSources.Count;
        AddSeller(buyer.PlayerEast, eastSupply);

        for (int g = 0; g < Goods; g++)
        {
            bool isRaw = (Raw & (1 << g)) != 0;
            westPrice[g] = TradePrice(buyer, isRaw, Ability.RawCheapWest);
            eastPrice[g] = TradePrice(buyer, isRaw, Ability.RawCheapEast);
        }
    }

    /// <summary>The buyer's either/or cards and wildcards, one entry (a mask of goods) per unit they give.</summary>
    private void CollectOwnSources(PlayerState buyer)
    {
        foreach (Card card in buyer.PlayedCards)
        {
            int mask = card.ChoiceMask;
            if (mask != 0)
            {
                ownSources.Add(mask);
            }
        }
        if (buyer.Has(Ability.AnyRaw))
        {
            ownSources.Add(Raw);
        }
        if (buyer.Has(Ability.AnyManufactured))
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
            if (buyer.Has(rawTradingPost | Ability.WonderRawCheap))
            {
                return DiscountedTradePrice;
            }
            return StandardTradePrice;
        }
        if (buyer.Has(Ability.ManufCheap))
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
        for (int g = 0; g < Goods; g++)
        {
            supply[g] = seller.Resources[(Resource)g];
        }
        foreach (Card card in seller.PlayedCards)
        {
            int mask = card.SellableChoiceMask;
            if (mask != 0)
            {
                sellerSources.Add(mask);
            }
        }
    }

    /// <summary>A mask of the goods the buyer still needs at least one unit of.</summary>
    private int NeedMask()
    {
        int mask = 0;
        for (int g = 0; g < Goods; g++)
        {
            if (Need[g] > 0)
            {
                mask |= 1 << g;
            }
        }
        return mask;
    }

    /// <summary>
    /// Gives each of the buyer's own either/or sources to one still-needed resource, trying every choice.
    /// A free unit never makes a plan dearer, so a source is only left unused when it offers nothing needed.
    /// </summary>
    private void UseOwn(int i)
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
        if (i == ownSources.Count)
        {
            UseSellers(0);
            return;
        }

        int nextSource = i + 1;
        int options = ownSources[i] & needed;
        if (options == 0)
        {
            UseOwn(nextSource);
            return;
        }
        for (int g = 0; g < Goods; g++)
        {
            if ((options & (1 << g)) == 0)
            {
                continue;
            }
            Need[g]--;
            UseOwn(nextSource);
            Need[g]++;
        }
    }

    /// <summary>
    /// Gives each neighbour either/or source to one resource the buyer still needs and that neighbour
    /// doesn't already offer enough of, trying every choice; then prices the purchase.
    /// </summary>
    private void UseSellers(int i)
    {
        if (i == sellerSources.Count)
        {
            Price();
            return;
        }

        int nextSource = i + 1;
        int[] supply = SupplyOfSourceOwner(i);
        bool any = false;
        for (int g = 0; g < Goods; g++)
        {
            if ((sellerSources[i] & (1 << g)) == 0 || supply[g] >= Need[g])
            {
                continue;
            }
            any = true;
            supply[g]++;
            UseSellers(nextSource);
            supply[g]--;
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
        for (int g = 0; g < Goods; g++)
        {
            int n = Need[g];
            if (n == 0)
            {
                continue;
            }

            if (westPrice[g] <= eastPrice[g])
            {
                int fromWest = Math.Min(n, westSupply[g]);
                int fromEast = n - fromWest;
                if (fromEast > eastSupply[g])
                {
                    return;
                }
                toWest += fromWest * westPrice[g];
                toEast += fromEast * eastPrice[g];
            }
            else
            {
                int fromEast = Math.Min(n, eastSupply[g]);
                int fromWest = n - fromEast;
                if (fromWest > westSupply[g])
                {
                    return;
                }
                toEast += fromEast * eastPrice[g];
                toWest += fromWest * westPrice[g];
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
