using _7WondersGame.src.models.Effects;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Agents
{
    /// <summary>
    /// What cards and wonder stages are worth to one player, for <see cref="HeuristicAI"/>, in victory points: how
    /// much the player's score would go up if the move were made now and the era then ended (a battle; Olympia B
    /// copies a guild), plus the bonuses in <see cref="HeuristicWeights"/> for what that score can't see yet
    /// (resources a wonder stage lacks, trading discounts, a first science card...).
    /// One object is one decision: it caches card values. It works on copies, so the player doesn't change.
    /// </summary>
    public sealed class HeuristicValues
    {
        private readonly PlayerState _p;
        private readonly int _era;
        private readonly HeuristicWeights _w;
        private readonly int _baseline;
        private readonly Dictionary<CardId, double> _gains = new();
        private HeuristicValues? _next;

        public HeuristicValues(PlayerState player, int era, HeuristicWeights weights)
        {
            _p = player;
            _era = era;
            _w = weights;
            _baseline = ScoreIfEraEnded(player.Clone());
        }

        /// <summary>What building the card would add, before its price.</summary>
        public double Gain(Card card)
        {
            if (_gains.TryGetValue(card.Id, out double gain))
                return gain;
            PlayerState s = _p.Clone();
            int coins = s.Resources[Resource.Coins];
            // in the city before its effect applies, like a real build (the Lighthouse counts itself)
            s.PlayedCards.Add(card);
            s.ApplyCardEffect(card);
            gain = ScoreIfEraEnded(s) - _baseline + Bonus(card, s.Resources[Resource.Coins] - coins);
            _gains[card.Id] = gain;
            return gain;
        }

        /// <summary>The coins building the card costs now (0 if the player can't build it).</summary>
        public int Price(Card card) => _p.BuildPrice(card) ?? 0;

        /// <summary>What building the next wonder stage would add to the score.</summary>
        public double WonderGain()
        {
            PlayerState s = _p.Clone();
            s.Board.AddStage(s);
            return ScoreIfEraEnded(s) - _baseline;
        }

        /// <summary>The coins the next wonder stage costs now (0 if there is none or it can't be paid for).</summary>
        public int WonderPrice() => _p.Board.NextStageCost is { } cost ? _p.PlanPayment(cost)?.Total ?? 0 : 0;

        /// <summary>What a discard's 3 coins would add to the score.</summary>
        public double DiscardGain()
        {
            PlayerState s = _p.Clone();
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

            Ability grants = card.Effects.Aggregate(Ability.None, (a, e) => a | e.Grants);
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
            var goods = new List<Resource>();
            bool eitherOr = false;
            foreach (IEffect effect in card.Effects)
            {
                if (effect is Gain gain && gain.Resource <= Resource.Papyrus)
                    goods.Add(gain.Resource);
                else if (effect is OneOf oneOf)
                {
                    goods.AddRange(oneOf.Choices);
                    eitherOr = true;
                }
            }
            if (goods.Count == 0)
                return 0;

            // era 3 has no resource cards (only a Halikarnassos discard-pile build can bring one back)
            double wonderResource = _era switch
            {
                1 => _w.WonderResourceEra1,
                2 => _w.WonderResourceEra2,
                _ => 0,
            };
            List<Resource> lacking = goods.Where(r => Need(r) > Available(r)).ToList();
            if (lacking.Count > 0 && wonderResource != 0)
            {
                double bonus = wonderResource;
                if (eitherOr)
                    bonus += _w.PerUsefulChoice * goods.Count(r => Need(r) > 0);
                if (lacking.All(IsRaw) && CheapRawTrade())
                    bonus -= _w.CheapRawTradePenalty;
                return bonus;
            }
            if (_era == 2 && goods.Any(IsGood) && !GoodsNearby())
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

        private bool GoodsNearby()
        {
            foreach (PlayerState q in new[] { _p, _p.PlayerWest, _p.PlayerEast })
                if (q.Resources[Resource.Loom] + q.Resources[Resource.Glass] + q.Resources[Resource.Papyrus] > 0)
                    return true;
            return false;
        }

        /// <summary>Raw material cards, plus 1 for a board that makes a raw material.</summary>
        private static int RawCards(PlayerState q) => q.AmountOfType(CardType.Materials) + (IsRaw(q.Board.Production) ? 1 : 0);

        /// <summary>Manufactured good cards, plus 1 for a board that makes a good.</summary>
        private static int GoodsCards(PlayerState q) => q.AmountOfType(CardType.Manufactured) + (IsGood(q.Board.Production) ? 1 : 0);

        private static bool IsRaw(Resource r) => r <= Resource.Stone;
        private static bool IsGood(Resource r) => r is >= Resource.Loom and <= Resource.Papyrus;
    }
}
