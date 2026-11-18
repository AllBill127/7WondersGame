using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Effects;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;
using static _7WondersGame.Engine.Rules.Services.Rules;

namespace _7WondersGame.Agents.Heuristic;

/// <summary>
/// What cards and wonder stages are worth to one player, for <see cref="HeuristicAgent"/>, in victory points: how
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
    private readonly PlayerState _player;
    private readonly int _era;
    private readonly HeuristicWeights _weights;
    private readonly int _baseline;
    // the gains worked out so far (a decision looks at a handful of cards, so a list beats a dictionary)
    private CardId[] _gainIds = new CardId[8];
    private double[] _gains = new double[8];
    private int _gainCount;
    // the next player's values, made the first time a card's gain for them is asked for: most hands never ask,
    // and a decision is made in every rollout step, so making it up front would allocate for nothing
    private HeuristicValues? _nextPlayerValues;
    // the copy each try is made on, reset to the player first
    private PlayerState? _scratch;

    /// <summary>Values for one decision of <paramref name="player"/> in <paramref name="era"/>.</summary>
    public HeuristicValues(PlayerState player, int era, HeuristicWeights weights)
    {
        _player = player;
        _era = era;
        _weights = weights;
        _baseline = ScoreIfEraEnded(Copy());
    }

    /// <summary>The player as it is now, on the scratch copy.</summary>
    private PlayerState Copy()
    {
        if (_scratch == null)
        {
            _scratch = _player.Clone();
        }
        else
        {
            _scratch.CopyFrom(_player);
        }
        return _scratch;
    }

    /// <summary>What building the card would add, before its price.</summary>
    public double Gain(Card card)
    {
        for (int gainIndex = 0; gainIndex < _gainCount; gainIndex++)
        {
            if (_gainIds[gainIndex] == card.Id)
            {
                return _gains[gainIndex];
            }
        }
        double gain = WorkOutGain(card);
        RememberGain(card, gain);
        return gain;
    }

    /// <summary>Builds the card on the scratch copy and scores it.</summary>
    private double WorkOutGain(Card card)
    {
        PlayerState scratchPlayer = Copy();
        int coinsBefore = scratchPlayer.Resources[Resource.Coins];

        // in the city before its effect applies, like a real build (the Lighthouse counts itself)
        scratchPlayer.BuiltCards.Add(card);
        scratchPlayer.ApplyCardEffect(card);

        int score = ScoreIfEraEnded(scratchPlayer);
        int coinsGained = scratchPlayer.Resources[Resource.Coins] - coinsBefore;
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
        PlayerState scratchPlayer = Copy();
        scratchPlayer.Wonder.AddStage(scratchPlayer);
        return ScoreIfEraEnded(scratchPlayer) - _baseline;
    }

    /// <summary>What a discard's 3 coins would add to the score.</summary>
    public double DiscardGain()
    {
        PlayerState scratchPlayer = Copy();
        scratchPlayer.Resources[Resource.Coins] += CoinsForDiscard;
        return ScoreIfEraEnded(scratchPlayer) - _baseline;
    }

    /// <summary>
    /// What the card would be worth to the player who gets this hand next (west in eras 1 and 3, east in era 2),
    /// before its price, or 0 if they couldn't build it.
    /// </summary>
    public double NextPlayerGain(Card card)
    {
        PlayerState next = NextPlayer();
        if (next == null || next == _player || next.BuildPrice(card) == null)
        {
            return 0;
        }
        _nextPlayerValues ??= new HeuristicValues(next, _era, _weights);
        return _nextPlayerValues.Gain(card);
    }

    /// <summary>The player who gets this hand next: east in era 2, west in eras 1 and 3.</summary>
    private PlayerState NextPlayer()
    {
        if (_era == 2)
        {
            return _player.EastNeighbour;
        }
        return _player.WestNeighbour;
    }

    /// <summary>The player's score if the era ended now: a battle, then Olympia B's guild copy.</summary>
    private int ScoreIfEraEnded(PlayerState scratchPlayer)
    {
        scratchPlayer.Battle(_era);
        scratchPlayer.CopyGuild();
        return scratchPlayer.CalculateScore();
    }

    // =========== bonuses ===========

    /// <summary>What the card is worth beyond the score it adds now.</summary>
    private double Bonus(Card card, int coinsGained)
    {
        double bonus = ProductionBonus(card);

        Ability grants = AbilitiesGrantedBy(card);
        if ((grants & Ability.RawMaterialsCheapFromEastNeighbour) != 0)
        {
            bonus += TradingPostBonus(_player.EastNeighbour);
        }
        if ((grants & Ability.RawMaterialsCheapFromWestNeighbour) != 0)
        {
            bonus += TradingPostBonus(_player.WestNeighbour);
        }
        if ((grants & Ability.ManufacturedGoodsCheapFromBothNeighbours) != 0)
        {
            bonus += MarketplaceBonus();
        }

        if (card.Type == CardType.Scientific && _era <= 2)
        {
            bonus += ScienceBonus();
        }
        if (card.Type == CardType.Civilian && _era == 1)
        {
            bonus += _weights.CivilianCardBonusEra1;
        }
        // in era 1 everyone starts poor, so this starts in era 2
        if (_era >= 2 && coinsGained > 0 && _player.Resources[Resource.Coins] <= _weights.PoorCoinThreshold)
        {
            bonus += _weights.PoorPlayerCoinGainBonus * coinsGained;
        }
        return bonus;
    }

    /// <summary>Every lasting ability the card's effects give.</summary>
    private static Ability AbilitiesGrantedBy(Card card)
    {
        Ability grants = Ability.None;
        for (int effectIndex = 0; effectIndex < card.Effects.Count; effectIndex++)
        {
            grants |= card.Effects[effectIndex].Grants;
        }
        return grants;
    }

    /// <summary>
    /// A science card in era 1 or 2: more for the player's first one when it already has 2 raw material cards.
    /// </summary>
    private double ScienceBonus()
    {
        bool isFirstScience = _player.CountBuiltCardsOfType(CardType.Scientific) == 0;
        bool hasRawMaterials = _player.CountBuiltCardsOfType(CardType.Materials) >= 2;
        if (!isFirstScience || !hasRawMaterials)
        {
            return _weights.EarlyScienceCardBonus;
        }
        if (_era == 1)
        {
            return _weights.FirstScienceCardBonusEra1;
        }
        return _weights.FirstScienceCardBonusEra2;
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
        foreach (Resource resource in goods)
        {
            if (Need(resource) > Available(resource))
            {
                lacking++;
                lackingAllRaw &= IsRaw(resource);
            }
            anyGood |= IsGood(resource);
        }

        if (lacking > 0 && wonderResource != 0)
        {
            return LackingResourceBonus(card, goods, wonderResource, lackingAllRaw);
        }
        if (_era == 2 && anyGood && !GoodsNearby())
        {
            return _weights.NoManufacturedGoodsNearbyBonusEra2;
        }
        return 0;
    }

    /// <summary>How many goods the card lists: one per resource it gives, every choice of an either/or.</summary>
    private static int CountGoodsMade(Card card)
    {
        int count = 0;
        for (int effectIndex = 0; effectIndex < card.Effects.Count; effectIndex++)
        {
            if (card.Effects[effectIndex] is Gain gain && gain.Resource <= Resource.Papyrus)
            {
                count++;
            }
            else if (card.Effects[effectIndex] is OneOf oneOf)
            {
                count += oneOf.Choices.Length;
            }
        }
        return count;
    }

    /// <summary>Writes the goods the card makes into <paramref name="goods"/>, in effect order.</summary>
    private static void ListGoodsMade(Card card, Span<Resource> goods)
    {
        int goodsCount = 0;
        for (int effectIndex = 0; effectIndex < card.Effects.Count; effectIndex++)
        {
            if (card.Effects[effectIndex] is Gain gain && gain.Resource <= Resource.Papyrus)
            {
                goods[goodsCount] = gain.Resource;
                goodsCount++;
            }
            else if (card.Effects[effectIndex] is OneOf oneOf)
            {
                foreach (Resource resource in oneOf.Choices)
                {
                    goods[goodsCount] = resource;
                    goodsCount++;
                }
            }
        }
    }

    /// <summary>Whether the card gives one resource of a choice (an either/or card).</summary>
    private static bool IsEitherOrCard(Card card)
    {
        for (int effectIndex = 0; effectIndex < card.Effects.Count; effectIndex++)
        {
            if (card.Effects[effectIndex] is OneOf)
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
                return _weights.MissingWonderResourceBonusEra1;
            case 2:
                return _weights.MissingWonderResourceBonusEra2;
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
            bonus += _weights.BonusPerUsefulResourceChoice * CountUsefulChoices(goods);
        }
        if (lackingAllRaw && CheapRawTrade())
        {
            bonus -= _weights.CheapRawMaterialTradePenalty;
        }
        return bonus;
    }

    /// <summary>How many of the goods some wonder stage still to build needs.</summary>
    private int CountUsefulChoices(Span<Resource> goods)
    {
        int useful = 0;
        foreach (Resource resource in goods)
        {
            if (Need(resource) > 0)
            {
                useful++;
            }
        }
        return useful;
    }

    /// <summary>The most of a resource that one of the wonder stages still to build needs.</summary>
    private int Need(Resource resource)
    {
        int need = 0;
        for (int stageIndex = _player.Wonder.BuiltStageCount; stageIndex < _player.Wonder.StageCount; stageIndex++)
        {
            need = Math.Max(need, _player.Wonder.Stages[stageIndex].Cost.GetValueOrDefault(resource));
        }
        return need;
    }

    /// <summary>
    /// How much of what's needed the player and its neighbours (what they can sell) produce, up to the need each.
    /// </summary>
    private int Available(Resource resource)
    {
        int need = Need(resource);
        int own = CountProducible(_player, resource, need, false);
        int fromWest = CountProducible(_player.WestNeighbour, resource, need, true);
        int fromEast = CountProducible(_player.EastNeighbour, resource, need, true);
        return own + fromWest + fromEast;
    }

    /// <summary>
    /// How much of <paramref name="needed"/> this player could produce of a resource: its fixed production plus
    /// every either/or source that can give it (for a neighbour, only the brown cards they could sell).
    /// </summary>
    private static int CountProducible(PlayerState player, Resource resource, int needed, bool isNeighbor)
    {
        int count = player.Resources[resource];
        if (count >= needed)
        {
            return needed;
        }

        int resourceBit = 1 << (int)resource;
        foreach (Card card in player.BuiltCards)
        {
            if ((ChoiceMaskOf(card, isNeighbor) & resourceBit) != 0)
            {
                count++;
            }
        }
        if (!isNeighbor && IsRaw(resource) && player.Has(Ability.AnyRawMaterialEachTurn))
        {
            count++;
        }
        if (!isNeighbor && IsGood(resource) && player.Has(Ability.AnyManufacturedGoodEachTurn))
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
        int west = RawCards(_player.WestNeighbour);
        int east = RawCards(_player.EastNeighbour);
        int plenty = _weights.NeighbourRawMaterialCardsForCheapTrade;
        bool cheapFromEast = _player.Has(Ability.RawMaterialsCheapFromEastNeighbour) && east >= plenty;
        bool cheapFromWest = _player.Has(Ability.RawMaterialsCheapFromWestNeighbour) && west >= plenty;
        bool cheapFromEither = _player.Has(Ability.RawMaterialsCheapFromBothNeighbours)
                               && Math.Max(west, east) >= plenty;
        return cheapFromEast || cheapFromWest || cheapFromEither;
    }

    /// <summary>
    /// A trading post towards the neighbour: worth it when the player has few raw materials and they have some.
    /// </summary>
    private double TradingPostBonus(PlayerState neighbour)
    {
        int theirs = RawCards(neighbour);
        return TradingDiscountBonus(RawCards(_player), _weights.FewOwnRawMaterialCardsThreshold, theirs);
    }

    /// <summary>The Marketplace: worth it when the player has few goods and its neighbours have some.</summary>
    private double MarketplaceBonus()
    {
        // each neighbour's own board
        int theirs = GoodsCards(_player.WestNeighbour) + GoodsCards(_player.EastNeighbour);
        return TradingDiscountBonus(GoodsCards(_player), _weights.FewOwnManufacturedGoodsCardsThreshold, theirs);
    }

    /// <summary>
    /// A discount card's bonus: only when the player has few cards of the kind and the neighbours have some.
    /// </summary>
    private double TradingDiscountBonus(int ownCards, int fewOwnCards, int neighbourCards)
    {
        if (ownCards <= fewOwnCards && neighbourCards >= 1)
        {
            return _weights.TradingDiscountBonus + _weights.TradingDiscountBonusPerNeighbourCard * neighbourCards;
        }
        return 0;
    }

    /// <summary>Whether the player or a neighbour produces any manufactured good.</summary>
    private bool GoodsNearby()
    {
        return MakesGoods(_player) || MakesGoods(_player.WestNeighbour) || MakesGoods(_player.EastNeighbour);
    }

    private static bool MakesGoods(PlayerState cityOwner)
    {
        int goods = cityOwner.Resources[Resource.Loom]
            + cityOwner.Resources[Resource.Glass]
            + cityOwner.Resources[Resource.Papyrus];
        return goods > 0;
    }

    /// <summary>Raw material cards, plus 1 for a board that makes a raw material.</summary>
    private static int RawCards(PlayerState cityOwner)
    {
        int count = cityOwner.CountBuiltCardsOfType(CardType.Materials);
        if (IsRaw(cityOwner.Wonder.Production))
        {
            count++;
        }
        return count;
    }

    /// <summary>Manufactured good cards, plus 1 for a board that makes a good.</summary>
    private static int GoodsCards(PlayerState cityOwner)
    {
        int count = cityOwner.CountBuiltCardsOfType(CardType.Manufactured);
        if (IsGood(cityOwner.Wonder.Production))
        {
            count++;
        }
        return count;
    }

    private static bool IsRaw(Resource resource)
    {
        return resource <= Resource.Stone;
    }

    private static bool IsGood(Resource resource)
    {
        return resource is >= Resource.Loom and <= Resource.Papyrus;
    }
}
