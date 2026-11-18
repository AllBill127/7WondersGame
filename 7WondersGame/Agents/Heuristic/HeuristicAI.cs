using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Games.Enums;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Payment.Dtos;
using _7WondersGame.Engine.Payment.Services;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Agents.Heuristic;

/// <summary>
/// The heuristic agent: the usual opponent, and the rollout policy of <see cref="Mcts.MctsAgent"/>.
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
/// <remarks>
/// <see cref="Choose(PlayerState, Phase, int, List{Card})"/> runs in every step of a search's rollouts: it and
/// everything it calls use no LINQ, no delegates and no allocations beyond the values object.
/// </remarks>
public sealed class HeuristicAI : IAgent
{
    private static readonly Command Skip = new(ActionType.Skip, null);

    /// <summary>Its numbers. Each is a setting of the agent.</summary>
    public HeuristicWeights Weights { get; init; } = new();

    /// <summary>The best move for the view's seat (see the class summary).</summary>
    public Command Choose(GameView view)
    {
        return Choose(view.Me, view.Phase, view.Era, view.DiscardPile);
    }

    /// <summary>
    /// The same choice without a <see cref="GameView"/>, for a search that plays its own copy of the game.
    /// <paramref name="me"/> must be part of a whole table (neighbours set); nothing changes.
    /// </summary>
    public Command Choose(PlayerState me, Phase phase, int era, List<Card> discardPile)
    {
        var values = new HeuristicValues(me, era, Weights);

        if (phase == Phase.DiscardBuild)
        {
            return ChooseDiscardPileBuild(me, discardPile, values);
        }

        List<Card> hand = me.HandCards;
        if (hand.Count == 0)
        {
            return Skip;
        }

        // Each price and value is worked out once and nothing is allocated that needn't be. The values (and so the
        // choices) are the same as working each out when needed.
        Span<double> denied = stackalloc double[hand.Count];
        FillDeniedValues(denied, hand, values);

        var best = new BestMove();
        ConsiderBuilds(ref best, me, values, denied);
        if (phase == Phase.ChooseMoves && me.FreeCardOnce)
        {
            ConsiderOlympiaFreeBuilds(ref best, me, values, denied);
        }

        int giveUp = CardToGiveUp(hand, values, denied);
        Card giveUpCard = hand[giveUp];
        double giveUpDenied = denied[giveUp];
        ConsiderWonderStage(ref best, me, values, giveUpCard, giveUpDenied);
        double discardValue = values.DiscardGain() - Weights.DiscardPenalty + giveUpDenied;
        best.Consider(ActionType.Discard, giveUpCard, discardValue);

        return MoveOrSkip(best.Action, best.Card);
    }

    /// <summary>Halikarnassos: the build is free, so only the card's worth counts.</summary>
    private static Command ChooseDiscardPileBuild(PlayerState me, List<Card> discardPile, HeuristicValues values)
    {
        List<Card> playable = me.GetPlayableCards(discardPile, playAllFree: true);
        Card? pick = HighestValueCard(playable, values);
        return MoveOrSkip(ActionType.Build, pick);
    }

    /// <summary>What taking each hand card keeps from the next player, by hand position (left at 0 if off).</summary>
    private void FillDeniedValues(Span<double> denied, List<Card> hand, HeuristicValues values)
    {
        if (Weights.Denial == 0)
        {
            return;
        }
        for (int i = 0; i < hand.Count; i++)
        {
            denied[i] = Weights.Denial * values.NextPlayerGain(hand[i]);
        }
    }

    /// <summary>Building each hand card the player can build: its worth minus its price.</summary>
    private void ConsiderBuilds(ref BestMove best, PlayerState me, HeuristicValues values, Span<double> denied)
    {
        List<Card> hand = me.HandCards;
        for (int i = 0; i < hand.Count; i++)
        {
            // a price means the card can be built (the cards GetPlayableCards gives)
            int? price = me.BuildPrice(hand[i]);
            if (price == null)
            {
                continue;
            }
            double value = values.Gain(hand[i]) - Weights.CoinValue * price.Value + denied[i];
            best.Consider(ActionType.Build, hand[i], value);
        }
    }

    /// <summary>Olympia A's free card on each hand card not built yet: its worth minus keeping the charge.</summary>
    private void ConsiderOlympiaFreeBuilds(
        ref BestMove best,
        PlayerState me,
        HeuristicValues values,
        Span<double> denied)
    {
        List<Card> hand = me.HandCards;
        double keep = FreeBuildKeepValue(hand);
        foreach (Card c in me.GetPlayableCards(hand, playAllFree: true))
        {
            int handIndex = hand.IndexOf(c);
            double value = values.Gain(c) - keep + denied[handIndex];
            best.Consider(ActionType.BuildFreeOnce, c, value);
        }
    }

    /// <summary>
    /// What keeping Olympia A's charge is worth: the charge is lost at the era end, so on the era's last choice
    /// (2 cards left) it is worth nothing.
    /// </summary>
    private double FreeBuildKeepValue(List<Card> hand)
    {
        if (hand.Count <= 2)
        {
            return 0;
        }
        return Weights.FreeBuildKeep;
    }

    /// <summary>
    /// The hand position of the card to give up for a wonder stage or a discard: the first with the highest denial
    /// minus gain.
    /// </summary>
    private static int CardToGiveUp(List<Card> hand, HeuristicValues values, Span<double> denied)
    {
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
        return giveUp;
    }

    /// <summary>
    /// The next wonder stage, if the player can pay for it: what it adds, plus a bonus, minus its price.
    /// </summary>
    private void ConsiderWonderStage(
        ref BestMove best,
        PlayerState me,
        HeuristicValues values,
        Card giveUpCard,
        double giveUpDenied)
    {
        int? stagePrice = WonderStagePrice(me);
        if (stagePrice == null)
        {
            return;
        }
        double value = values.WonderGain() + Weights.WonderStage - Weights.CoinValue * stagePrice.Value + giveUpDenied;
        best.Consider(ActionType.BuildWonder, giveUpCard, value);
    }

    /// <summary>
    /// The coins the next wonder stage costs, planned once; null when there is none or it can't be paid for (as
    /// <c>CanBuildWonder</c> says).
    /// </summary>
    private static int? WonderStagePrice(PlayerState me)
    {
        IReadOnlyDictionary<Resource, int>? stageCost = me.Board.NextStageCost;
        if (stageCost == null)
        {
            return null;
        }
        PaymentPlan? stagePlan = PaymentPlanner.Plan(me, stageCost);
        if (stagePlan == null)
        {
            return null;
        }
        return stagePlan.Value.Total;
    }

    /// <summary>The card with the highest value, the earliest one on a tie; null for no cards.</summary>
    private static Card? HighestValueCard(List<Card> cards, HeuristicValues values)
    {
        Card? best = null;
        double bestValue = double.NegativeInfinity;
        foreach (Card c in cards)
        {
            double v = values.Gain(c);
            if (best == null || v > bestValue)
            {
                best = c;
                bestValue = v;
            }
        }
        return best;
    }

    /// <summary>The move with the card, or Skip when there is no card.</summary>
    private static Command MoveOrSkip(ActionType action, Card? card)
    {
        if (card == null)
        {
            return Skip;
        }
        return new Command(action, card);
    }
}
