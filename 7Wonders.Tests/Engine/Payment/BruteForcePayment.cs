using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Payment.Dtos;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;
using _7Wonders.Engine.Wonders.Enums;

namespace _7Wonders.Tests.Engine.Payment;

/// <summary>
/// The cheapest payment found by trying everything: every choice (including "unused") for every either/or source,
/// then every split of every purchase between the neighbours. Written from the rules, independently of the
/// planner, to check it.
/// </summary>
internal sealed class BruteForcePayment
{
    private const int Buyer = 0;
    private const int West = 1;
    private const int East = 2;

    private static readonly Resource[] Raw = { Resource.Wood, Resource.Ore, Resource.Clay, Resource.Stone };
    private static readonly Resource[] Manufactured = { Resource.Loom, Resource.Glass, Resource.Papyrus };

    private static readonly Resource[] Goods =
    {
        Resource.Wood, Resource.Ore, Resource.Clay, Resource.Stone, Resource.Loom, Resource.Glass, Resource.Papyrus
    };

    private readonly PlayerState _buyer;
    private readonly Dictionary<Resource, int> _cost;

    // every either/or source: its owner (Buyer, West or East) and the resources it may give
    private readonly List<EitherOrSource> _sources = new();

    // what each owner has of each good, with the sources' choices tried so far added. A two-dimensional array:
    // _supply[owner, good] is one cell of a 3 x 7 table (owner Buyer, West or East; good by its Resource value).
    private readonly int[,] _supply = new int[3, 7];

    private int _bestTotal = int.MaxValue;
    private int _bestEast = int.MaxValue;

    private BruteForcePayment(PlayerState buyer, Dictionary<Resource, int> cost)
    {
        _buyer = buyer;
        _cost = cost;
    }

    /// <summary>The cheapest payment for <paramref name="cost"/> (ties: least to the east), or null if none.</summary>
    public static PaymentPlan? Plan(PlayerState buyer, Dictionary<Resource, int> cost)
    {
        int budget = buyer.Resources[Resource.Coins] - cost.GetValueOrDefault(Resource.Coins);
        if (budget < 0)
        {
            return null;
        }

        BruteForcePayment search = new(buyer, cost);
        search.CollectSourcesAndSupply();
        search.Assign(0);

        if (search._bestTotal == int.MaxValue || search._bestTotal > budget)
        {
            return null;
        }

        int toWest = search._bestTotal - search._bestEast;
        return new PaymentPlan(toWest, search._bestEast);
    }

    private void CollectSourcesAndSupply()
    {
        PlayerState[] owners = { _buyer, _buyer.WestNeighbour, _buyer.EastNeighbour };
        for (int ownerIndex = 0; ownerIndex < 3; ownerIndex++)
        {
            foreach (Card card in owners[ownerIndex].BuiltCards)
            {
                Resource[]? options = OptionsOf(card.Id, ownerIndex == Buyer);
                if (options != null)
                {
                    AddSource(ownerIndex, options);
                }
            }
        }

        WonderId board = _buyer.Wonder.Id;
        int stage = _buyer.Wonder.BuiltStageCount;
        if (board == WonderId.AlexandriaA && stage >= 2 || board == WonderId.AlexandriaB && stage >= 1)
        {
            AddSource(Buyer, Raw);
        }

        if (board == WonderId.AlexandriaB && stage >= 2)
        {
            AddSource(Buyer, Manufactured);
        }

        for (int ownerIndex = 0; ownerIndex < 3; ownerIndex++)
        {
            foreach (Resource resource in Goods)
            {
                _supply[ownerIndex, (int)resource] = owners[ownerIndex].Resources[resource];
            }
        }
    }

    // giving a source a resource the cost doesn't need is the same as leaving it unused
    private void AddSource(int owner, Resource[] options)
    {
        Resource[] needed = options.Where(resource => _cost.GetValueOrDefault(resource) > 0).ToArray();
        if (needed.Length > 0)
        {
            _sources.Add(new EitherOrSource(owner, needed));
        }
    }

    // the resources a card offers one of; the Caravansery and Forum only to their owner
    private static Resource[]? OptionsOf(CardId id, bool own)
    {
        switch (id)
        {
            case CardId.TreeFarm:
                return new[] { Resource.Wood, Resource.Clay };
            case CardId.ForestCave:
                return new[] { Resource.Wood, Resource.Ore };
            case CardId.TimberYard:
                return new[] { Resource.Wood, Resource.Stone };
            case CardId.Excavation:
                return new[] { Resource.Clay, Resource.Stone };
            case CardId.ClayPit:
                return new[] { Resource.Clay, Resource.Ore };
            case CardId.Mine:
                return new[] { Resource.Ore, Resource.Stone };
            case CardId.Caravansery:
                if (own)
                {
                    return Raw;
                }

                return null;
            case CardId.Forum:
                if (own)
                {
                    return Manufactured;
                }

                return null;
            default:
                return null;
        }
    }

    // tries source i unused and with each of its options, then the sources after it. Recursion with backtracking:
    // each choice adds one unit to the owner's supply, the call for the next source tries all of its choices on top,
    // and the unit is taken back before the next choice, so every combination is tried once from the same table.
    private void Assign(int sourceIndex)
    {
        if (sourceIndex == _sources.Count)
        {
            SplitEveryPurchase();
            return;
        }

        int nextSource = sourceIndex + 1;
        Assign(nextSource);   // unused
        int owner = _sources[sourceIndex].Owner;
        foreach (Resource resource in _sources[sourceIndex].Options)
        {
            _supply[owner, (int)resource]++;
            Assign(nextSource);
            _supply[owner, (int)resource]--;
        }
    }

    // every split of every missing resource between west and east
    private void SplitEveryPurchase()
    {
        List<Resource> missing = Goods.Where(IsMissing).ToList();
        Split(missing, 0, 0, 0);
    }

    // the buyer has fewer than the cost asks for, even with the sources' choices
    private bool IsMissing(Resource resource)
    {
        return _cost.GetValueOrDefault(resource) > _supply[Buyer, (int)resource];
    }

    private void Split(List<Resource> missing, int goodIndex, int total, int toEast)
    {
        if (goodIndex == missing.Count)
        {
            KeepIfCheaper(total, toEast);
            return;
        }

        Resource resource = missing[goodIndex];
        int missingCount = _cost[resource] - _supply[Buyer, (int)resource];
        int nextGood = goodIndex + 1;
        for (int fromWest = 0; fromWest <= missingCount; fromWest++)
        {
            int fromEast = missingCount - fromWest;
            if (fromWest > _supply[West, (int)resource] || fromEast > _supply[East, (int)resource])
            {
                continue;
            }

            int paidToEast = fromEast * Price(East, resource);
            Split(missing, nextGood, total + fromWest * Price(West, resource) + paidToEast, toEast + paidToEast);
        }
    }

    // cheaper wins; at the same total, less paid to the east
    private void KeepIfCheaper(int total, int toEast)
    {
        if (total < _bestTotal || total == _bestTotal && toEast < _bestEast)
        {
            _bestTotal = total;
            _bestEast = toEast;
        }
    }

    private int Price(int seller, Resource resource)
    {
        bool cheap;
        if (Raw.Contains(resource))
        {
            cheap = PlayerAbilities.Has(_buyer, Ability.RawMaterialsCheapFromBothNeighbours)
                    || HasRawDiscountWith(seller);
        }
        else
        {
            cheap = PlayerAbilities.Has(_buyer, Ability.ManufacturedGoodsCheapFromBothNeighbours);
        }

        if (cheap)
        {
            return 1;
        }

        return 2;
    }

    private bool HasRawDiscountWith(int seller)
    {
        if (seller == West)
        {
            return PlayerAbilities.Has(_buyer, Ability.RawMaterialsCheapFromWestNeighbour);
        }

        return PlayerAbilities.Has(_buyer, Ability.RawMaterialsCheapFromEastNeighbour);
    }
}
