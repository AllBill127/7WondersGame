using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Moves.Dtos;
using _7Wonders.Engine.Moves.Enums;
using _7Wonders.Engine.Payment.Dtos;
using _7Wonders.Engine.Payment.Services;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Rules.Services;

/// <summary>
/// The first of the three steps that carry out a move (see <see cref="Game.ResolveMoves"/>): checks it
/// against the turn-start state and prices it, changing nothing. <see cref="MoveResolution"/> does the
/// other two.
/// </summary>
internal static class MovePlanning
{
    /// <summary>
    /// Step 1: checks a move against the current state and works out what it costs, changing nothing.
    /// Null if the rules don't allow it. <paramref name="source"/> is where the card comes from.
    /// </summary>
    public static PlannedMove? PlanMove(PlayerState player, MoveAction action, Card card, List<Card> source)
    {
        switch (action)
        {
            case MoveAction.Build:
                return PlanBuild(player, action, card, source, free: false);
            case MoveAction.BuildWithFreeBuild:
                return PlanFreeOnceBuild(player, card, source);
            case MoveAction.BuildWonderStage:
                return PlanWonderStage(player, card, source);
            case MoveAction.Discard:
                return PlanDiscard(player, card, source);
            default:
                return null;
        }
    }

    /// <summary>
    /// Step 1 for Halikarnassos: a free build from the discard pile while
    /// <see cref="PlayerState.DiscardPileBuildPending"/> is on.
    /// </summary>
    public static PlannedMove? PlanDiscardPileBuild(PlayerState player, Card card, List<Card> discardPile)
    {
        if (!player.DiscardPileBuildPending)
        {
            return null;
        }
        PlannedMove? move = PlanBuild(player, MoveAction.Build, card, discardPile, free: true);
        if (move == null)
        {
            return null;
        }
        return move.AsDiscardPileBuild();
    }

    /// <summary>
    /// A build of a card from <paramref name="source"/>, paid for unless <paramref name="free"/> or chained.
    /// </summary>
    private static PlannedMove? PlanBuild(
        PlayerState player,
        MoveAction action,
        Card card,
        List<Card> source,
        bool free)
    {
        if (BuildRules.HasBuiltCardNamed(player, card) || !source.Contains(card))
        {
            return null;
        }
        if (free || BuildRules.CanBuildByChaining(player, card))
        {
            return new PlannedMove(player, action, card, source, PaymentPlan.Free, 0);
        }

        PaymentPlan? plan = PaymentPlanner.Plan(player, card.Cost);
        if (plan == null)
        {
            return null;
        }
        return new PlannedMove(player, action, card, source, plan.Value, BuildRules.CoinCostOf(card));
    }

    /// <summary>Olympia A stage 2: one free card per era, while the era's charge is unused.</summary>
    private static PlannedMove? PlanFreeOnceBuild(PlayerState player, Card card, List<Card> source)
    {
        if (!PlayerAbilities.Has(player, Ability.FreeBuildEachEra) || !player.FreeBuildAvailable)
        {
            return null;
        }
        return PlanBuild(player, MoveAction.BuildWithFreeBuild, card, source, free: true);
    }

    /// <summary>The next wonder stage, built with a card from <paramref name="source"/>.</summary>
    private static PlannedMove? PlanWonderStage(PlayerState player, Card card, List<Card> source)
    {
        IReadOnlyDictionary<Resource, int>? cost = player.Wonder.NextStageCost;
        if (cost == null)
        {
            return null;
        }
        PaymentPlan? plan = PaymentPlanner.Plan(player, cost);
        if (plan == null || !source.Contains(card))
        {
            return null;
        }
        return new PlannedMove(player, MoveAction.BuildWonderStage, card, source, plan.Value, 0);
    }

    /// <summary>Discarding a card from <paramref name="source"/> for coins; always allowed and free.</summary>
    private static PlannedMove? PlanDiscard(PlayerState player, Card card, List<Card> source)
    {
        if (!source.Contains(card))
        {
            return null;
        }
        return new PlannedMove(player, MoveAction.Discard, card, source, PaymentPlan.Free, 0);
    }
}
