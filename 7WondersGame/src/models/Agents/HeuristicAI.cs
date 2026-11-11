using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Agents
{
    /// <summary>
    /// The heuristic agent (fix plan step 5, G), which replaces DeterministicAI as the opponent and the rollout policy.
    /// It values each move it may make and takes the best one (all the numbers are in <see cref="Weights"/>):
    /// <list type="bullet">
    /// <item>build a card: what the card is worth (<see cref="HeuristicValues.Gain"/>), minus its price;</item>
    /// <item>Olympia A's free card: the same without the price, minus what keeping the charge is worth;</item>
    /// <item>a wonder stage: what the stage adds to the score, plus a bonus, minus its price;</item>
    /// <item>a discard: what the 3 coins add to the score, minus a penalty.</item>
    /// </list>
    /// Every move takes one card out of the hand, which keeps it from the next player: that is worth
    /// <see cref="HeuristicWeights.Denial"/> per point it would give them. For a wonder stage or a discard the card is
    /// the one worth least to this player (it may come round again) and most to the next player.
    /// Ties go to the earlier card in the hand, and to build, free build, wonder, discard in that order.
    /// It only reads the state.
    /// </summary>
    public sealed class HeuristicAI : IAgent
    {
        private static readonly Command Skip = new(ActionType.Skip, null);

        /// <summary>Its numbers. Each is a setting of the agent.</summary>
        public HeuristicWeights Weights { get; init; } = new();

        public Command Choose(GameView view) => Choose(view.Me, view.Phase, view.Era, view.DiscardPile);

        /// <summary>
        /// The same choice without a <see cref="GameView"/>, for a search that plays its own copy of the game
        /// (fix plan step 6). <paramref name="me"/> must be part of a whole table (neighbours set); nothing changes.
        /// </summary>
        public Command Choose(PlayerState me, Phase phase, int era, List<Card> discardPile)
        {
            HeuristicWeights w = Weights;
            var values = new HeuristicValues(me, era, w);

            if (phase == Phase.DiscardBuild)
            {
                // Halikarnassos: free, so only the card's worth counts
                Card? pick = Best(me.GetPlayableCards(discardPile, playAllFree: true), values.Gain);
                return pick == null ? Skip : new Command(ActionType.Build, pick);
            }

            List<Card> hand = me.HandCards;
            if (hand.Count == 0)
                return Skip;

            // This runs in every step of a search's rollouts, so each price and value is worked out once and nothing
            // is allocated that needn't be. The values (and so the choices) are the same as working each out when needed.
            // What taking each card keeps from the next player:
            Span<double> denied = stackalloc double[hand.Count];
            if (w.Denial != 0)
                for (int i = 0; i < hand.Count; i++)
                    denied[i] = w.Denial * values.NextPlayerGain(hand[i]);

            var best = new BestMove();
            for (int i = 0; i < hand.Count; i++)
            {
                // a price means the card can be built (the cards GetPlayableCards gives)
                if (me.BuildPrice(hand[i]) is int price)
                    best.Consider(ActionType.Build, hand[i], values.Gain(hand[i]) - w.CoinValue * price + denied[i]);
            }

            if (phase == Phase.ChooseMoves && me.FreeCardOnce)
            {
                // the charge is lost at the era end, so on the era's last choice keeping it is worth nothing
                double keep = hand.Count <= 2 ? 0 : w.FreeBuildKeep;
                foreach (Card c in me.GetPlayableCards(hand, playAllFree: true))
                    best.Consider(ActionType.BuildFreeOnce, c, values.Gain(c) - keep + denied[hand.IndexOf(c)]);
            }

            // the card to give up for a wonder stage or a discard: the first with the highest denial minus gain
            int giveUp = 0;
            double giveUpValue = denied[0] - values.Gain(hand[0]);
            for (int i = 1; i < hand.Count; i++)
            {
                double v = denied[i] - values.Gain(hand[i]);
                if (v > giveUpValue)
                {
                    giveUp = i;
                    giveUpValue = v;
                }
            }
            // the stage's price, planned once (it is null when it can't be built, as CanBuildWonder says)
            if (me.Board.NextStageCost is { } stageCost && me.PlanPayment(stageCost) is { } stagePlan)
                best.Consider(ActionType.BuildWonder, hand[giveUp],
                    values.WonderGain() + w.WonderStage - w.CoinValue * stagePlan.Total + denied[giveUp]);
            best.Consider(ActionType.Discard, hand[giveUp], values.DiscardGain() - w.DiscardPenalty + denied[giveUp]);

            return best.Card == null ? Skip : new Command(best.Action, best.Card);
        }

        /// <summary>The best move so far (none: Skip): the first one with the highest value.</summary>
        private struct BestMove
        {
            public ActionType Action;
            public Card? Card;
            private double _value;

            public BestMove() : this(default, null) { }
            private BestMove(ActionType action, Card? card) => (Action, Card, _value) = (action, card, double.NegativeInfinity);

            public void Consider(ActionType action, Card card, double value)
            {
                if (value > _value)
                {
                    Action = action;
                    Card = card;
                    _value = value;
                }
            }
        }

        /// <summary>The card with the highest value, the earliest one on a tie; null for no cards.</summary>
        private static Card? Best(List<Card> cards, Func<Card, double> value)
        {
            Card? best = null;
            double bestValue = double.NegativeInfinity;
            foreach (Card c in cards)
            {
                double v = value(c);
                if (best == null || v > bestValue)
                {
                    best = c;
                    bestValue = v;
                }
            }
            return best;
        }
    }
}
