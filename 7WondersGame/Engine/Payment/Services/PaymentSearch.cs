using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Payment.Dtos;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Engine.Payment.Services;

internal sealed class PaymentSearch
{
    // Bit i stands for the Resource with value i: Wood, Ore, Clay, Stone, Loom, Glass, Papyrus.
    private const int Goods = 7;
    private const int Raw = 0b0001111;
    private const int Manufactured = 0b1110000;

    public readonly int[] Need = new int[Goods];
    private readonly int[] need;
    private readonly List<int> ownSources = new();
    private readonly List<int> sellerSources = new();   // west neighbour's first, then east's
    private int westSourceCount;
    private readonly int[] westSupply = new int[Goods], eastSupply = new int[Goods];
    private readonly int[] westPrice = new int[Goods], eastPrice = new int[Goods];
    private int bestTotal, bestEast;

    public PaymentSearch() => need = Need;

    /// <summary>The cheapest plan for what <see cref="Need"/> holds, or null.</summary>
    public PaymentPlan? Run(PlayerState buyer)
    {
        Reset(buyer);
        UseOwn(0);
        return bestTotal == int.MaxValue ? null : new PaymentPlan(bestTotal - bestEast, bestEast);
    }

    private void Reset(PlayerState buyer)
    {
        ownSources.Clear();
        sellerSources.Clear();
        bestTotal = int.MaxValue;
        bestEast = int.MaxValue;

        foreach (Card card in buyer.PlayedCards)
        {
            int mask = card.ChoiceMask;
            if (mask != 0)
                ownSources.Add(mask);
        }
        if (buyer.Has(Ability.AnyRaw))
            ownSources.Add(Raw);
        if (buyer.Has(Ability.AnyManufactured))
            ownSources.Add(Manufactured);

        AddSeller(buyer.PlayerWest, westSupply);
        westSourceCount = sellerSources.Count;
        AddSeller(buyer.PlayerEast, eastSupply);

        for (int g = 0; g < Goods; g++)
        {
            bool raw = (Raw & (1 << g)) != 0;
            westPrice[g] = raw ? (buyer.Has(Ability.RawCheapWest | Ability.WonderRawCheap) ? 1 : 2) : (buyer.Has(Ability.ManufCheap) ? 1 : 2);
            eastPrice[g] = raw ? (buyer.Has(Ability.RawCheapEast | Ability.WonderRawCheap) ? 1 : 2) : (buyer.Has(Ability.ManufCheap) ? 1 : 2);
        }
    }

    private void AddSeller(PlayerState seller, int[] supply)
    {
        for (int g = 0; g < Goods; g++)
            supply[g] = seller.Resources[(Resource)g];
        foreach (Card card in seller.PlayedCards)
        {
            int mask = card.SellableChoiceMask;
            if (mask != 0)
                sellerSources.Add(mask);
        }
    }

    private int NeedMask()
    {
        int mask = 0;
        for (int g = 0; g < Goods; g++)
            if (need[g] > 0)
                mask |= 1 << g;
        return mask;
    }

    /// <summary>
    /// Gives each of the buyer's own either/or sources to one still-needed resource, trying every choice.
    /// A free unit never makes a plan dearer, so a source is only left unused when it offers nothing needed.
    /// </summary>
    private void UseOwn(int i)
    {
        if (bestTotal == 0)
            return;
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
        int options = ownSources[i] & needed;
        if (options == 0)
        {
            UseOwn(i + 1);
            return;
        }
        for (int g = 0; g < Goods; g++)
        {
            if ((options & (1 << g)) == 0)
                continue;
            need[g]--;
            UseOwn(i + 1);
            need[g]++;
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
        int[] supply = i < westSourceCount ? westSupply : eastSupply;
        bool any = false;
        for (int g = 0; g < Goods; g++)
        {
            if ((sellerSources[i] & (1 << g)) == 0 || supply[g] >= need[g])
                continue;
            any = true;
            supply[g]++;
            UseSellers(i + 1);
            supply[g]--;
        }
        if (!any)
            UseSellers(i + 1);
    }

    /// <summary>Buys each missing resource from the cheaper neighbour first (west when equal).</summary>
    private void Price()
    {
        int toWest = 0, toEast = 0;
        for (int g = 0; g < Goods; g++)
        {
            int n = need[g];
            if (n == 0)
                continue;
            if (westPrice[g] <= eastPrice[g])
            {
                int fromWest = Math.Min(n, westSupply[g]);
                if (n - fromWest > eastSupply[g])
                    return;
                toWest += fromWest * westPrice[g];
                toEast += (n - fromWest) * eastPrice[g];
            }
            else
            {
                int fromEast = Math.Min(n, eastSupply[g]);
                if (n - fromEast > westSupply[g])
                    return;
                toEast += fromEast * eastPrice[g];
                toWest += (n - fromEast) * westPrice[g];
            }
        }
        Consider(toWest + toEast, toEast);
    }

    private void Consider(int total, int toEast)
    {
        if (total < bestTotal || total == bestTotal && toEast < bestEast)
        {
            bestTotal = total;
            bestEast = toEast;
        }
    }
}
