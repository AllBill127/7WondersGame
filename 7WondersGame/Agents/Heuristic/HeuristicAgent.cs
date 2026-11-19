using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Common.Settings;
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
/// <see cref="HeuristicWeights.NextPlayerDenialWeight"/> per point it would give them. For a wonder stage or a discard
/// the card is the one worth least to this player (it may come round again) and most to the next player.
/// Ties go to the earlier card in the hand, and to build, free build, wonder, discard in that order.
/// It only reads the state.
/// </summary>
/// <remarks>
/// <see cref="Choose(PlayerState, Phase, int, List{Card})"/> runs in every step of a search's rollouts: it and
/// everything it calls use no LINQ, no delegates and no allocations beyond the values object.
/// </remarks>
public sealed class HeuristicAgent : IAgent
{
    private static readonly Move Skip = new(MoveAction.Skip, null);

    /// <summary>Its numbers. Each is a setting of the agent.</summary>
    [SettingGroup]
    public HeuristicWeights Weights { get; init; } = new();

    /// <summary>The best move for the view's seat (see the class summary).</summary>
    public Move Choose(GameView view)
    {
        return Choose(view.DecidingPlayer, view.Phase, view.Era, view.DiscardPile);
    }

    /// <summary>
    /// The same choice without a <see cref="GameView"/>, for a search that plays its own copy of the game.
    /// <paramref name="player"/> must be part of a whole table (neighbours set); nothing changes.
    /// </summary>
    public Move Choose(PlayerState player, Phase phase, int era, List<Card> discardPile)
    {
        HeuristicValues values = new(player, era, Weights);

        if (phase == Phase.DiscardPileBuild)
        {
            return ChooseDiscardPileBuild(player, discardPile, values);
        }

        List<Card> hand = player.Hand;
        if (hand.Count == 0)
        {
            return Skip;
        }

        // Each price and value is worked out once and nothing is allocated that needn't be. The values (and so the
        // choices) are the same as working each out when needed.
        Span<double> denied = stackalloc double[hand.Count];
        FillDeniedValues(denied, hand, values);

        BestMove best = new();
        ConsiderBuilds(ref best, player, values, denied);
        if (phase == Phase.ChooseMoves && player.OlympiaFreeBuildAvailable)
        {
            ConsiderOlympiaFreeBuilds(ref best, player, values, denied);
        }

        int giveUp = CardToGiveUp(hand, values, denied);
        Card giveUpCard = hand[giveUp];
        double giveUpDenied = denied[giveUp];
        ConsiderWonderStage(ref best, player, values, giveUpCard, giveUpDenied);
        double discardValue = values.DiscardGain() - Weights.DiscardPenalty + giveUpDenied;
        best.Consider(MoveAction.Discard, giveUpCard, discardValue);

        return MoveOrSkip(best.Action, best.Card);
    }

    /// <summary>Halikarnassos: the build is free, so only the card's worth counts.</summary>
    private static Move ChooseDiscardPileBuild(PlayerState player, List<Card> discardPile, HeuristicValues values)
    {
        List<Card> buildable = player.BuildableCards(discardPile, everyCardFree: true);
        Card? pick = HighestValueCard(buildable, values);
        return MoveOrSkip(MoveAction.Build, pick);
    }

    /// <summary>What taking each hand card keeps from the next player, by hand position (left at 0 if off).</summary>
    private void FillDeniedValues(Span<double> denied, List<Card> hand, HeuristicValues values)
    {
        if (Weights.NextPlayerDenialWeight == 0)
        {
            return;
        }
        for (int cardIndex = 0; cardIndex < hand.Count; cardIndex++)
        {
            denied[cardIndex] = Weights.NextPlayerDenialWeight * values.NextPlayerGain(hand[cardIndex]);
        }
    }

    /// <summary>Building each hand card the player can build: its worth minus its price.</summary>
    private void ConsiderBuilds(ref BestMove best, PlayerState player, HeuristicValues values, Span<double> denied)
    {
        List<Card> hand = player.Hand;
        for (int cardIndex = 0; cardIndex < hand.Count; cardIndex++)
        {
            // a price means the card can be built (the cards BuildableCards gives)
            int? price = player.BuildPrice(hand[cardIndex]);
            if (price == null)
            {
                continue;
            }
            double value = values.Gain(hand[cardIndex])
                           - Weights.CoinValueInVictoryPoints * price.Value
                           + denied[cardIndex];
            best.Consider(MoveAction.Build, hand[cardIndex], value);
        }
    }

    /// <summary>Olympia A's free card on each hand card not built yet: its worth minus keeping the charge.</summary>
    private void ConsiderOlympiaFreeBuilds(
        ref BestMove best,
        PlayerState player,
        HeuristicValues values,
        Span<double> denied)
    {
        List<Card> hand = player.Hand;
        double keep = KeepOlympiaFreeBuildValue(hand);
        foreach (Card card in player.BuildableCards(hand, everyCardFree: true))
        {
            int handIndex = hand.IndexOf(card);
            double value = values.Gain(card) - keep + denied[handIndex];
            best.Consider(MoveAction.BuildWithOlympiaFreeBuild, card, value);
        }
    }

    /// <summary>
    /// What keeping Olympia A's charge is worth: the charge is lost at the era end, so on the era's last choice
    /// (2 cards left) it is worth nothing.
    /// </summary>
    private double KeepOlympiaFreeBuildValue(List<Card> hand)
    {
        if (hand.Count <= 2)
        {
            return 0;
        }
        return Weights.KeepOlympiaFreeBuildValue;
    }

    /// <summary>
    /// The hand position of the card to give up for a wonder stage or a discard: the first with the highest denial
    /// minus gain.
    /// </summary>
    private static int CardToGiveUp(List<Card> hand, HeuristicValues values, Span<double> denied)
    {
        int giveUp = 0;
        double giveUpValue = denied[0] - values.Gain(hand[0]);
        for (int cardIndex = 1; cardIndex < hand.Count; cardIndex++)
        {
            double cardGiveUpValue = denied[cardIndex] - values.Gain(hand[cardIndex]);
            if (cardGiveUpValue > giveUpValue)
            {
                giveUp = cardIndex;
                giveUpValue = cardGiveUpValue;
            }
        }
        return giveUp;
    }

    /// <summary>
    /// The next wonder stage, if the player can pay for it: what it adds, plus a bonus, minus its price.
    /// </summary>
    private void ConsiderWonderStage(
        ref BestMove best,
        PlayerState player,
        HeuristicValues values,
        Card giveUpCard,
        double giveUpDenied)
    {
        int? stagePrice = WonderStagePrice(player);
        if (stagePrice == null)
        {
            return;
        }
        double value = values.WonderGain()
                       + Weights.WonderStageBonus
                       - Weights.CoinValueInVictoryPoints * stagePrice.Value
                       + giveUpDenied;
        best.Consider(MoveAction.BuildWonderStage, giveUpCard, value);
    }

    /// <summary>
    /// The coins the next wonder stage costs, planned once; null when there is none or it can't be paid for (as
    /// <c>CanBuildWonder</c> says).
    /// </summary>
    private static int? WonderStagePrice(PlayerState player)
    {
        IReadOnlyDictionary<Resource, int>? stageCost = player.Wonder.NextStageCost;
        if (stageCost == null)
        {
            return null;
        }
        PaymentPlan? stagePlan = PaymentPlanner.Plan(player, stageCost);
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
        foreach (Card card in cards)
        {
            double value = values.Gain(card);
            if (best == null || value > bestValue)
            {
                best = card;
                bestValue = value;
            }
        }
        return best;
    }

    /// <summary>The move with the card, or Skip when there is no card.</summary>
    private static Move MoveOrSkip(MoveAction action, Card? card)
    {
        if (card == null)
        {
            return Skip;
        }
        return new Move(action, card);
    }
}
