using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Payment.Dtos;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Payment.Services;

/// <summary>
/// Finds the cheapest way to pay a cost. It only reads the players and changes nothing.
/// <list type="bullet">
/// <item>Free, from the buyer's own city: fixed production (brown and grey cards, the board's resource), the
/// either/or brown cards, Caravansery, Forum and Alexandria's wildcards. Each either/or source gives one unit.</item>
/// <item>Bought from a neighbour: their fixed production and either/or brown cards (not their yellow cards or
/// wonder stages), 2 coins a unit, or 1 with the matching trading post, the Marketplace or Olympia B.</item>
/// </list>
/// Every way of using the either/or sources is tried, and a purchase may be split between the neighbours.
/// The plan with the lowest total wins. Among equally cheap plans the one paying least to the east wins
/// (west first), so agents never choose between them.
/// </summary>
public static class PaymentPlanner
{
    // One search per thread, reused: planning runs for every card a heuristic looks at (thousands of times a
    // second in a search's rollouts), and a new one each time was a large share of what a rollout allocates.
    // It is made on first use in each thread: a [ThreadStatic] field's initializer runs only on the first thread,
    // so on every other thread the field would start out null.
    [ThreadStatic] private static PaymentSearch? _search;

    /// <summary>
    /// The cheapest plan for <paramref name="buyer"/> to pay <paramref name="cost"/>, or null if they can't
    /// (the resources aren't available, or buying them plus the coin cost is more than their coins).
    /// </summary>
    public static PaymentPlan? Plan(PlayerState buyer, IReadOnlyDictionary<Resource, int> cost)
    {
        int budget = buyer.Resources[Resource.Coins] - CoinCostOf(cost);
        if (budget < 0)
        {
            return null;
        }

        // What the fixed production doesn't cover. Usually nothing, and then no search is needed.
        _search ??= new PaymentSearch();
        PaymentSearch search = _search;
        bool missing = FillMissing(buyer, cost, search.Need);
        if (!missing)
        {
            return PaymentPlan.Free;
        }

        PaymentPlan? best = search.Run(buyer);
        if (best == null)
        {
            return null;
        }
        if (best.Value.Total > budget)
        {
            return null;
        }
        return best;
    }

    /// <summary>The coins the cost itself asks for (for example Tree Farm's 1 coin), 0 if none.</summary>
    private static int CoinCostOf(IReadOnlyDictionary<Resource, int> cost)
    {
        if (cost.TryGetValue(Resource.Coins, out int coinCost))
        {
            return coinCost;
        }
        return 0;
    }

    /// <summary>
    /// Clears <paramref name="need"/> and writes into it how much of each good the buyer's fixed production
    /// doesn't cover. Returns whether anything is missing.
    /// </summary>
    private static bool FillMissing(PlayerState buyer, IReadOnlyDictionary<Resource, int> cost, int[] need)
    {
        Array.Clear(need);
        bool missing = false;
        if (cost is Dictionary<Resource, int> dictionary)
        {
            // (the costs are dictionaries; enumerating one through the interface would allocate)
            foreach ((Resource resource, int amount) in dictionary)
            {
                missing |= AddMissing(buyer, need, resource, amount);
            }
        }
        else
        {
            foreach ((Resource resource, int amount) in cost)
            {
                missing |= AddMissing(buyer, need, resource, amount);
            }
        }
        return missing;
    }

    /// <summary>
    /// Records in <paramref name="need"/> how much of one good the buyer lacks. Coins are not goods and are
    /// skipped. Returns whether any is missing.
    /// </summary>
    private static bool AddMissing(PlayerState buyer, int[] need, Resource resource, int amount)
    {
        if (resource > Resource.Papyrus)
        {
            return false;
        }
        int missing = amount - buyer.Resources[resource];
        if (missing <= 0)
        {
            return false;
        }
        need[(int)resource] = missing;
        return true;
    }
}
