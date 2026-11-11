using _7WondersGame.src.models.Effects;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Agents
{
    /// <summary>
    /// What cards and wonder stages are worth to one player, for <see cref="HeuristicAI"/>, in victory points: how
    /// much the player's score would go up if the move were made now and the era then ended (a battle; Olympia B
    /// copies a guild), plus the bonuses in <see cref="HeuristicWeights"/> for what that score can't see yet
    /// (resources a wonder stage lacks, trading discounts, a first science card...).
    /// One object is one decision: it caches card values. It works on a copy (one, reset before each try), so the
    /// player doesn't change.
    /// </summary>
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
        private HeuristicValues? _next;
        // the copy each try is made on, reset to the player first
        private PlayerState? _scratch;

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
                _scratch = _p.Clone();
            else
                _scratch.CopyFrom(_p);
            return _scratch;
        }

        /// <summary>What building the card would add, before its price.</summary>
        public double Gain(Card card)
        {
            for (int i = 0; i < _gainCount; i++)
                if (_gainIds[i] == card.Id)
                    return _gains[i];
            PlayerState s = Copy();
            int coins = s.Resources[Resource.Coins];
            // in the city before its effect applies, like a real build (the Lighthouse counts itself)
            s.PlayedCards.Add(card);
            s.ApplyCardEffect(card);
            double gain = ScoreIfEraEnded(s) - _baseline + Bonus(card, s.Resources[Resource.Coins] - coins);
            if (_gainCount == _gains.Length)
            {
                Array.Resize(ref _gainIds, _gainCount * 2);
                Array.Resize(ref _gains, _gainCount * 2);
            }
            _gainIds[_gainCount] = card.Id;
            _gains[_gainCount++] = gain;
            return gain;
        }

        /// <summary>The coins building the card costs now (0 if the player can't build it).</summary>
        public int Price(Card card) => _p.BuildPrice(card) ?? 0;

        /// <summary>What building the next wonder stage would add to the score.</summary>
        public double WonderGain()
        {
            PlayerState s = Copy();
            s.Board.AddStage(s);
            return ScoreIfEraEnded(s) - _baseline;
        }

        /// <summary>The coins the next wonder stage costs now (0 if there is none or it can't be paid for).</summary>
        public int WonderPrice() => _p.Board.NextStageCost is { } cost ? _p.PlanPayment(cost)?.Total ?? 0 : 0;

        /// <summary>What a discard's 3 coins would add to the score.</summary>
        public double DiscardGain()
        {
            PlayerState s = Copy();
            s.Resources[Resource.Coins] += 3;
            return ScoreIfEraEnded(s) - _baseline;
        }

        /// <summary>
        /// What the card would be worth to the player who gets this hand next (west in eras 1 and 3, east in era 2),
        /// before its price, or 0 if they couldn't build it.
        /// </summary>
        public double NextPlayerGain(Card card)
        {
            PlayerState next = _era == 2 ? _p.PlayerEast : _p.PlayerWest;
            if (next == null || next == _p || next.BuildPrice(card) == null)
                return 0;
            _next ??= new HeuristicValues(next, _era, _w);
            return _next.Gain(card);
        }

        private int ScoreIfEraEnded(PlayerState s)
        {
            s.Battle(_era);
            s.CopyGuild();
            return s.CalculateScore();
        }

        // =========== bonuses ===========

        private double Bonus(Card card, int coinsGained)
        {
            double bonus = ProductionBonus(card);

            Ability grants = Ability.None;
            for (int i = 0; i < card.Effects.Count; i++)
                grants |= card.Effects[i].Grants;
            if ((grants & Ability.RawCheapEast) != 0)
                bonus += TradingPostBonus(_p.PlayerEast);
            if ((grants & Ability.RawCheapWest) != 0)
                bonus += TradingPostBonus(_p.PlayerWest);
            if ((grants & Ability.ManufCheap) != 0)
                bonus += MarketplaceBonus();

            if (card.Type == CardType.Scientific && _era <= 2)
                bonus += _p.AmountOfType(CardType.Scientific) == 0 && _p.AmountOfType(CardType.Materials) >= 2
                    ? (_era == 1 ? _w.FirstScienceEra1 : _w.FirstScienceEra2)
                    : _w.ScienceEarly;
            if (card.Type == CardType.Civilian && _era == 1)
                bonus += _w.CivilianEra1;
            // in era 1 everyone starts poor, so this starts in era 2
            if (_era >= 2 && coinsGained > 0 && _p.Resources[Resource.Coins] <= _w.PoorCoins)
                bonus += _w.PoorCoinBonus * coinsGained;
            return bonus;
        }

        /// <summary>A resource card: is it a resource the wonder stages still to build lack?</summary>
        private double ProductionBonus(Card card)
        {
            // the goods the card makes, listed once per effect (no list: this runs for every card a rollout looks at)
            int count = 0;
            for (int i = 0; i < card.Effects.Count; i++)
            {
                if (card.Effects[i] is Gain gain && gain.Resource <= Resource.Papyrus)
                    count++;
                else if (card.Effects[i] is OneOf oneOf)
                    count += oneOf.Choices.Length;
            }
            if (count == 0)
                return 0;
            Span<Resource> goods = stackalloc Resource[count];
            bool eitherOr = false;
            int n = 0;
            for (int i = 0; i < card.Effects.Count; i++)
            {
                if (card.Effects[i] is Gain gain && gain.Resource <= Resource.Papyrus)
                    goods[n++] = gain.Resource;
                else if (card.Effects[i] is OneOf oneOf)
                {
                    foreach (Resource r in oneOf.Choices)
                        goods[n++] = r;
                    eitherOr = true;
                }
            }

            // era 3 has no resource cards (only a Halikarnassos discard-pile build can bring one back)
            double wonderResource = _era switch
            {
                1 => _w.WonderResourceEra1,
                2 => _w.WonderResourceEra2,
                _ => 0,
            };
            int lacking = 0;
            bool lackingAllRaw = true, anyGood = false;
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
                double bonus = wonderResource;
                if (eitherOr)
                {
                    int useful = 0;
                    foreach (Resource r in goods)
                        if (Need(r) > 0)
                            useful++;
                    bonus += _w.PerUsefulChoice * useful;
                }
                if (lackingAllRaw && CheapRawTrade())
                    bonus -= _w.CheapRawTradePenalty;
                return bonus;
            }
            if (_era == 2 && anyGood && !GoodsNearby())
                return _w.NoGoodsNearbyEra2;
            return 0;
        }

        /// <summary>The most of a resource that one of the wonder stages still to build needs.</summary>
        private int Need(Resource r)
        {
            int need = 0;
            for (int s = _p.Board.Stage; s < _p.Board.StageCount; s++)
                need = Math.Max(need, _p.Board.Stages[s].Cost.GetValueOrDefault(r));
            return need;
        }

        /// <summary>How much of what's needed the player and its neighbours (what they can sell) produce, up to the need each.</summary>
        private int Available(Resource r)
        {
            int need = Need(r);
            return _p.CountProducible(r, need, false) +
                   _p.PlayerWest.CountProducible(r, need, true) +
                   _p.PlayerEast.CountProducible(r, need, true);
        }

        private bool CheapRawTrade()
        {
            int west = RawCards(_p.PlayerWest), east = RawCards(_p.PlayerEast);
            int plenty = _w.RawNeighbourPlenty;
            return _p.Has(Ability.RawCheapEast) && east >= plenty ||
                   _p.Has(Ability.RawCheapWest) && west >= plenty ||
                   _p.Has(Ability.WonderRawCheap) && Math.Max(west, east) >= plenty;
        }

        private double TradingPostBonus(PlayerState neighbour)
        {
            int theirs = RawCards(neighbour);
            return RawCards(_p) <= _w.FewOwnRaw && theirs >= 1 ? _w.TradeDiscount + _w.TradeDiscountPerCard * theirs : 0;
        }

        private double MarketplaceBonus()
        {
            // each neighbour's own board (the old heuristic checked this player's board for both, R10)
            int theirs = GoodsCards(_p.PlayerWest) + GoodsCards(_p.PlayerEast);
            return GoodsCards(_p) <= _w.FewOwnGoods && theirs >= 1 ? _w.TradeDiscount + _w.TradeDiscountPerCard * theirs : 0;
        }

        private bool GoodsNearby() => MakesGoods(_p) || MakesGoods(_p.PlayerWest) || MakesGoods(_p.PlayerEast);

        private static bool MakesGoods(PlayerState q) =>
            q.Resources[Resource.Loom] + q.Resources[Resource.Glass] + q.Resources[Resource.Papyrus] > 0;

        /// <summary>Raw material cards, plus 1 for a board that makes a raw material.</summary>
        private static int RawCards(PlayerState q) => q.AmountOfType(CardType.Materials) + (IsRaw(q.Board.Production) ? 1 : 0);

        /// <summary>Manufactured good cards, plus 1 for a board that makes a good.</summary>
        private static int GoodsCards(PlayerState q) => q.AmountOfType(CardType.Manufactured) + (IsGood(q.Board.Production) ? 1 : 0);

        private static bool IsRaw(Resource r) => r <= Resource.Stone;
        private static bool IsGood(Resource r) => r is >= Resource.Loom and <= Resource.Papyrus;
    }
}
