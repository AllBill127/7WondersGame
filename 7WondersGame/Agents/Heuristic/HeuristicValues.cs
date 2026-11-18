using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Effects;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;
using static _7WondersGame.Engine.Rules.Services.Rules;

namespace _7WondersGame.Agents.Heuristic;

/// <summary>
/// What cards and wonder stages are worth to one player, for <see cref="HeuristicAI"/>, in victory points: how
/// much the player's score would go up if the move were made now and the era then ended (a battle; Olympia B
/// copies a guild), plus the bonuses in <see cref="HeuristicWeights"/> for what that score can't see yet
/// (resources a wonder stage lacks, trading discounts, a first science card...).
/// One object is one decision: it caches card values. It works on a copy (one, reset before each try), so the
/// player doesn't change.
/// </summary>
/// <remarks>
/// It runs in every step of a search's rollouts: no LINQ, no delegates, and no allocations beyond the first copy
/// and the cache.
/// </remarks>
public sealed class HeuristicValues
{
    private readonly PlayerState _p;
    private readonly int _era;
    private readonly HeuristicWeights _w;
    private readonly int _baseline;
    // the gains worked out so far (a decision looks at a handful of cards, so a list beats a dictionary)
    private CardId[] _gainIds = new CardId[8];
    private double[] _gains = new double[8];
    private int _gainCount;
    // the next player's values, made the first time a card's gain for them is asked for: most hands never ask,
    // and a decision is made in every rollout step, so making it up front would allocate for nothing
    private HeuristicValues? _next;
    // the copy each try is made on, reset to the player first
    private PlayerState? _scratch;

    /// <summary>Values for one decision of <paramref name="player"/> in <paramref name="era"/>.</summary>
    public HeuristicValues(PlayerState player, int era, HeuristicWeights weights)
    {
        _p = player;
        _era = era;
        _w = weights;
        _baseline = ScoreIfEraEnded(Copy());
    }

    /// <summary>The player as it is now, on the scratch copy.</summary>
    private PlayerState Copy()
    {
        if (_scratch == null)
        {
            _scratch = _p.Clone();
        }
        else
        {
            _scratch.CopyFrom(_p);
        }
        return _scratch;
    }

    /// <summary>What building the card would add, before its price.</summary>
    public double Gain(Card card)
    {
        for (int i = 0; i < _gainCount; i++)
        {
            if (_gainIds[i] == card.Id)
            {
                return _gains[i];
            }
        }
        double gain = WorkOutGain(card);
        RememberGain(card, gain);
        return gain;
    }

    /// <summary>Builds the card on the scratch copy and scores it.</summary>
    private double WorkOutGain(Card card)
    {
        PlayerState s = Copy();
        int coinsBefore = s.Resources[Resource.Coins];

        // in the city before its effect applies, like a real build (the Lighthouse counts itself)
        s.PlayedCards.Add(card);
        s.ApplyCardEffect(card);

        int score = ScoreIfEraEnded(s);
        int coinsGained = s.Resources[Resource.Coins] - coinsBefore;
        return score - _baseline + Bonus(card, coinsGained);
    }

    /// <summary>Adds the card's gain to the cache, growing it when full.</summary>
    private void RememberGain(Card card, double gain)
    {
        if (_gainCount == _gains.Length)
        {
            int biggerLength = _gainCount * 2;
            Array.Resize(ref _gainIds, biggerLength);
            Array.Resize(ref _gains, biggerLength);
        }
        _gainIds[_gainCount] = card.Id;
        _gains[_gainCount] = gain;
        _gainCount++;
    }

    /// <summary>What building the next wonder stage would add to the score.</summary>
    public double WonderGain()
    {
        PlayerState s = Copy();
        s.Board.AddStage(s);
        return ScoreIfEraEnded(s) - _baseline;
    }

    /// <summary>What a discard's 3 coins would add to the score.</summary>
    public double DiscardGain()
    {
        PlayerState s = Copy();
        s.Resources[Resource.Coins] += CoinsForDiscard;
        return ScoreIfEraEnded(s) - _baseline;
    }

    /// <summary>
    /// What the card would be worth to the player who gets this hand next (west in eras 1 and 3, east in era 2),
    /// before its price, or 0 if they couldn't build it.
    /// </summary>
    public double NextPlayerGain(Card card)
    {
        PlayerState next = NextPlayer();
        if (next == null || next == _p || next.BuildPrice(card) == null)
        {
            return 0;
        }
        _next ??= new HeuristicValues(next, _era, _w);
        return _next.Gain(card);
    }

    /// <summary>The player who gets this hand next: east in era 2, west in eras 1 and 3.</summary>
    private PlayerState NextPlayer()
    {
        if (_era == 2)
        {
            return _p.PlayerEast;
        }
        return _p.PlayerWest;
    }

    /// <summary>The player's score if the era ended now: a battle, then Olympia B's guild copy.</summary>
    private int ScoreIfEraEnded(PlayerState s)
    {
        s.Battle(_era);
        s.CopyGuild();
        return s.CalculateScore();
    }

    // =========== bonuses ===========

    /// <summary>What the card is worth beyond the score it adds now.</summary>
    private double Bonus(Card card, int coinsGained)
    {
        double bonus = ProductionBonus(card);

        Ability grants = AbilitiesGrantedBy(card);
        if ((grants & Ability.RawCheapEast) != 0)
        {
            bonus += TradingPostBonus(_p.PlayerEast);
        }
        if ((grants & Ability.RawCheapWest) != 0)
        {
            bonus += TradingPostBonus(_p.PlayerWest);
        }
        if ((grants & Ability.ManufCheap) != 0)
        {
            bonus += MarketplaceBonus();
        }

        if (card.Type == CardType.Scientific && _era <= 2)
        {
            bonus += ScienceBonus();
        }
        if (card.Type == CardType.Civilian && _era == 1)
        {
            bonus += _w.CivilianEra1;
        }
        // in era 1 everyone starts poor, so this starts in era 2
        if (_era >= 2 && coinsGained > 0 && _p.Resources[Resource.Coins] <= _w.PoorCoins)
        {
            bonus += _w.PoorCoinBonus * coinsGained;
        }
        return bonus;
    }

    /// <summary>Every lasting ability the card's effects give.</summary>
    private static Ability AbilitiesGrantedBy(Card card)
    {
        Ability grants = Ability.None;
        for (int i = 0; i < card.Effects.Count; i++)
        {
            grants |= card.Effects[i].Grants;
        }
        return grants;
    }

    /// <summary>
    /// A science card in era 1 or 2: more for the player's first one when it already has 2 raw material cards.
    /// </summary>
    private double ScienceBonus()
    {
        bool isFirstScience = _p.AmountOfType(CardType.Scientific) == 0;
        bool hasRawMaterials = _p.AmountOfType(CardType.Materials) >= 2;
        if (!isFirstScience || !hasRawMaterials)
        {
            return _w.ScienceEarly;
        }
        if (_era == 1)
        {
            return _w.FirstScienceEra1;
        }
        return _w.FirstScienceEra2;
    }

    /// <summary>A resource card: is it a resource the wonder stages still to build lack?</summary>
    private double ProductionBonus(Card card)
    {
        // the goods the card makes, listed once per effect (on the stack: this runs for every card a rollout
        // looks at)
        int goodsCount = CountGoodsMade(card);
        if (goodsCount == 0)
        {
            return 0;
        }
        Span<Resource> goods = stackalloc Resource[goodsCount];
        ListGoodsMade(card, goods);

        double wonderResource = WonderResourceValueThisEra();
        int lacking = 0;
        bool lackingAllRaw = true;
        bool anyGood = false;
        foreach (Resource r in goods)
        {
            if (Need(r) > Available(r))
            {
                lacking++;
                lackingAllRaw &= IsRaw(r);
            }
            anyGood |= IsGood(r);
        }

        if (lacking > 0 && wonderResource != 0)
        {
            return LackingResourceBonus(card, goods, wonderResource, lackingAllRaw);
        }
        if (_era == 2 && anyGood && !GoodsNearby())
        {
            return _w.NoGoodsNearbyEra2;
        }
        return 0;
    }

    /// <summary>How many goods the card lists: one per resource it gives, every choice of an either/or.</summary>
    private static int CountGoodsMade(Card card)
    {
        int count = 0;
        for (int i = 0; i < card.Effects.Count; i++)
        {
            if (card.Effects[i] is Gain gain && gain.Resource <= Resource.Papyrus)
            {
                count++;
            }
            else if (card.Effects[i] is OneOf oneOf)
            {
                count += oneOf.Choices.Length;
            }
        }
        return count;
    }

    /// <summary>Writes the goods the card makes into <paramref name="goods"/>, in effect order.</summary>
    private static void ListGoodsMade(Card card, Span<Resource> goods)
    {
        int n = 0;
        for (int i = 0; i < card.Effects.Count; i++)
        {
            if (card.Effects[i] is Gain gain && gain.Resource <= Resource.Papyrus)
            {
                goods[n] = gain.Resource;
                n++;
            }
            else if (card.Effects[i] is OneOf oneOf)
            {
                foreach (Resource r in oneOf.Choices)
                {
                    goods[n] = r;
                    n++;
                }
            }
        }
    }

    /// <summary>Whether the card gives one resource of a choice (an either/or card).</summary>
    private static bool IsEitherOrCard(Card card)
    {
        for (int i = 0; i < card.Effects.Count; i++)
        {
            if (card.Effects[i] is OneOf)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// What a resource the wonder lacks is worth this era. Era 3 has no resource cards (only a Halikarnassos
    /// discard-pile build can bring one back), so it is worth nothing there.
    /// </summary>
    private double WonderResourceValueThisEra()
    {
        switch (_era)
        {
            case 1:
                return _w.WonderResourceEra1;
            case 2:
                return _w.WonderResourceEra2;
            default:
                return 0;
        }
    }

    /// <summary>
    /// A card making a resource the wonder lacks: the era's value, plus per useful choice of an either/or card, less
    /// when raw materials are already cheap to buy.
    /// </summary>
    private double LackingResourceBonus(Card card, Span<Resource> goods, double wonderResource, bool lackingAllRaw)
    {
        double bonus = wonderResource;
        if (IsEitherOrCard(card))
        {
            bonus += _w.PerUsefulChoice * CountUsefulChoices(goods);
        }
        if (lackingAllRaw && CheapRawTrade())
        {
            bonus -= _w.CheapRawTradePenalty;
        }
        return bonus;
    }

    /// <summary>How many of the goods some wonder stage still to build needs.</summary>
    private int CountUsefulChoices(Span<Resource> goods)
    {
        int useful = 0;
        foreach (Resource r in goods)
        {
            if (Need(r) > 0)
            {
                useful++;
            }
        }
        return useful;
    }

    /// <summary>The most of a resource that one of the wonder stages still to build needs.</summary>
    private int Need(Resource r)
    {
        int need = 0;
        for (int s = _p.Board.Stage; s < _p.Board.StageCount; s++)
        {
            need = Math.Max(need, _p.Board.Stages[s].Cost.GetValueOrDefault(r));
        }
        return need;
    }

    /// <summary>
    /// How much of what's needed the player and its neighbours (what they can sell) produce, up to the need each.
    /// </summary>
    private int Available(Resource r)
    {
        int need = Need(r);
        int own = CountProducible(_p, r, need, false);
        int fromWest = CountProducible(_p.PlayerWest, r, need, true);
        int fromEast = CountProducible(_p.PlayerEast, r, need, true);
        return own + fromWest + fromEast;
    }

    /// <summary>
    /// How much of <paramref name="needed"/> this player could produce of a resource: its fixed production plus
    /// every either/or source that can give it (for a neighbour, only the brown cards they could sell).
    /// </summary>
    private static int CountProducible(PlayerState p, Resource resource, int needed, bool isNeighbor)
    {
        int count = p.Resources[resource];
        if (count >= needed)
        {
            return needed;
        }

        int resourceBit = 1 << (int)resource;
        foreach (Card card in p.PlayedCards)
        {
            if ((ChoiceMaskOf(card, isNeighbor) & resourceBit) != 0)
            {
                count++;
            }
        }
        if (!isNeighbor && IsRaw(resource) && p.Has(Ability.AnyRaw))
        {
            count++;
        }
        if (!isNeighbor && IsGood(resource) && p.Has(Ability.AnyManufactured))
        {
            count++;
        }

        return Math.Min(needed, count);
    }

    /// <summary>The card's either/or resources: all of them for the owner, the sellable ones for a neighbour.</summary>
    private static int ChoiceMaskOf(Card card, bool isNeighbor)
    {
        if (isNeighbor)
        {
            return card.SellableChoiceMask;
        }
        return card.ChoiceMask;
    }

    /// <summary>
    /// Whether the player already buys raw materials for 1 coin from a neighbour with plenty of raw material cards.
    /// </summary>
    private bool CheapRawTrade()
    {
        int west = RawCards(_p.PlayerWest);
        int east = RawCards(_p.PlayerEast);
        int plenty = _w.RawNeighbourPlenty;
        bool cheapFromEast = _p.Has(Ability.RawCheapEast) && east >= plenty;
        bool cheapFromWest = _p.Has(Ability.RawCheapWest) && west >= plenty;
        bool cheapFromEither = _p.Has(Ability.WonderRawCheap) && Math.Max(west, east) >= plenty;
        return cheapFromEast || cheapFromWest || cheapFromEither;
    }

    /// <summary>
    /// A trading post towards the neighbour: worth it when the player has few raw materials and they have some.
    /// </summary>
    private double TradingPostBonus(PlayerState neighbour)
    {
        int theirs = RawCards(neighbour);
        return TradeDiscountBonus(RawCards(_p), _w.FewOwnRaw, theirs);
    }

    /// <summary>The Marketplace: worth it when the player has few goods and its neighbours have some.</summary>
    private double MarketplaceBonus()
    {
        // each neighbour's own board
        int theirs = GoodsCards(_p.PlayerWest) + GoodsCards(_p.PlayerEast);
        return TradeDiscountBonus(GoodsCards(_p), _w.FewOwnGoods, theirs);
    }

    /// <summary>
    /// A discount card's bonus: only when the player has few cards of the kind and the neighbours have some.
    /// </summary>
    private double TradeDiscountBonus(int ownCards, int fewOwnCards, int neighbourCards)
    {
        if (ownCards <= fewOwnCards && neighbourCards >= 1)
        {
            return _w.TradeDiscount + _w.TradeDiscountPerCard * neighbourCards;
        }
        return 0;
    }

    /// <summary>Whether the player or a neighbour produces any manufactured good.</summary>
    private bool GoodsNearby()
    {
        return MakesGoods(_p) || MakesGoods(_p.PlayerWest) || MakesGoods(_p.PlayerEast);
    }

    private static bool MakesGoods(PlayerState q)
    {
        return q.Resources[Resource.Loom] + q.Resources[Resource.Glass] + q.Resources[Resource.Papyrus] > 0;
    }

    /// <summary>Raw material cards, plus 1 for a board that makes a raw material.</summary>
    private static int RawCards(PlayerState q)
    {
        int count = q.AmountOfType(CardType.Materials);
        if (IsRaw(q.Board.Production))
        {
            count++;
        }
        return count;
    }

    /// <summary>Manufactured good cards, plus 1 for a board that makes a good.</summary>
    private static int GoodsCards(PlayerState q)
    {
        int count = q.AmountOfType(CardType.Manufactured);
        if (IsGood(q.Board.Production))
        {
            count++;
        }
        return count;
    }

    private static bool IsRaw(Resource r)
    {
        return r <= Resource.Stone;
    }

    private static bool IsGood(Resource r)
    {
        return r is >= Resource.Loom and <= Resource.Papyrus;
    }
}
