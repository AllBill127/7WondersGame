using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Payment.Dtos;
using _7WondersGame.Engine.Payment.Services;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Rules.Services;

/// <summary>
/// The rules, as static functions over <see cref="PlayerState"/>: checking, pricing and making a move, card
/// effects, and (in Rules.Scoring.cs) battles and scoring. Extension methods, so they read as
/// <c>player.PlanMove(...)</c>. The turn order is in <see cref="Game"/>.
/// </summary>
public static partial class Rules
{
    /// <summary>The coins a discarded card gives.</summary>
    public const int CoinsForDiscard = 3;

    // =========== Moves: plan, place, settle ===========
    /// <summary>
    /// Step 1: checks a move against the current state and works out what it costs, changing nothing.
    /// Null if the rules don't allow it. <paramref name="source"/> is where the card comes from.
    /// </summary>
    public static PlannedMove? PlanMove(this PlayerState player, MoveAction action, Card card, List<Card> source)
    {
        switch (action)
        {
            case MoveAction.Build:
                return player.PlanBuild(action, card, source, free: false);
            case MoveAction.BuildWithOlympiaFreeBuild:
                return player.PlanFreeOnceBuild(card, source);
            case MoveAction.BuildWonderStage:
                return player.PlanWonder(card, source);
            case MoveAction.Discard:
                return player.PlanDiscard(card, source);
            default:
                return null;
        }
    }

    /// <summary>
    /// Step 1 for Halikarnassos: a free build from the discard pile while
    /// <see cref="PlayerState.HalikarnassosDiscardBuildPending"/> is on.
    /// </summary>
    public static PlannedMove? PlanDiscardPileBuild(this PlayerState player, Card card, List<Card> discardPile)
    {
        if (!player.HalikarnassosDiscardBuildPending)
        {
            return null;
        }
        PlannedMove? move = player.PlanBuild(MoveAction.Build, card, discardPile, free: true);
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
        this PlayerState player,
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
    private static PlannedMove? PlanFreeOnceBuild(this PlayerState player, Card card, List<Card> source)
    {
        if (!PlayerAbilities.Has(player, Ability.OlympiaFreeBuildEachEra) || !player.OlympiaFreeBuildAvailable)
        {
            return null;
        }
        return player.PlanBuild(MoveAction.BuildWithOlympiaFreeBuild, card, source, free: true);
    }

    /// <summary>The next wonder stage, built with a card from <paramref name="source"/>.</summary>
    private static PlannedMove? PlanWonder(this PlayerState player, Card card, List<Card> source)
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
    private static PlannedMove? PlanDiscard(this PlayerState player, Card card, List<Card> source)
    {
        if (!source.Contains(card))
        {
            return null;
        }
        return new PlannedMove(player, MoveAction.Discard, card, source, PaymentPlan.Free, 0);
    }

    /// <summary>
    /// Step 2: the player pays their own coins (to the neighbours and the bank) and the card leaves where it came
    /// from. A built card goes into the city. Nobody receives anything yet.
    /// </summary>
    public static void Place(this PlayerState player, PlannedMove move)
    {
        player.Resources[Resource.Coins] -= move.Payment.Total + move.CoinCost;
        move.Source.Remove(move.Card);
        // The card is in the city before its effect applies, so the Lighthouse counts itself.
        // Sources: https://rules.dized.com/7-wonders/rule/question/19075b63-591e-4e99-8dfc-5fa34b726973,
        //          https://rules.dized.com/7-wonders/rule/symbols/third-age-cards
        if (move.Action is MoveAction.Build or MoveAction.BuildWithOlympiaFreeBuild)
        {
            player.BuiltCards.Add(move.Card);
        }
    }

    /// <summary>
    /// Step 3: the neighbours receive their trade coins and the move takes effect: the card's effect, the wonder
    /// stage, or 3 coins for a discard. A discarded card goes to the discard pile in <see cref="Game.ResolveMoves"/>.
    /// </summary>
    public static void Settle(this PlayerState player, PlannedMove move)
    {
        player.WestNeighbour.Resources[Resource.Coins] += move.Payment.ToWest;
        player.EastNeighbour.Resources[Resource.Coins] += move.Payment.ToEast;
        switch (move.Action)
        {
            case MoveAction.Build:
            case MoveAction.BuildWithOlympiaFreeBuild:
                CardEffects.ApplyCardEffect(player, move.Card);
                UseUpFreeBuildCharges(player, move);
                break;
            case MoveAction.BuildWonderStage:
                player.Wonder.AddStage(player);
                break;
            case MoveAction.Discard:
                player.Resources[Resource.Coins] += CoinsForDiscard;
                break;
        }
    }

    /// <summary>
    /// Turns off the one-use charge a free build used: Olympia A's free card (re-armed at the start of the next era)
    /// or Halikarnassos' discard pile build.
    /// </summary>
    private static void UseUpFreeBuildCharges(PlayerState player, PlannedMove move)
    {
        if (move.Action == MoveAction.BuildWithOlympiaFreeBuild)
        {
            player.OlympiaFreeBuildAvailable = false;
        }
        if (move.FromDiscardPile)
        {
            player.HalikarnassosDiscardBuildPending = false;
        }
    }
}
