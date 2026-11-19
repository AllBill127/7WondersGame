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

    // =========== Wonder ===========
    /// <summary>Is there a next wonder stage, and can the player pay for it?</summary>
    public static bool CanBuildWonder(this PlayerState player)
    {
        IReadOnlyDictionary<Resource, int>? cost = player.Wonder.NextStageCost;
        if (cost == null)
        {
            return false;
        }
        return PaymentPlanner.Plan(player, cost) != null;
    }

    // =========== Card related ===========
    /// <summary>
    /// A new list of the cards in <paramref name="cards"/> the player can build now (not a duplicate, and chained
    /// or affordable). With <paramref name="everyCardFree"/> (Halikarnassos, Olympia A's free card) cost doesn't
    /// matter.
    /// </summary>
    public static List<Card> BuildableCards(this PlayerState player, List<Card> cards, bool everyCardFree = false)
    {
        List<Card> buildableCards = new();
        foreach (Card card in cards)
        {
            // the same card can't be built twice
            if (player.HasPlayedCard(card))
            {
                continue;
            }

            if (everyCardFree || player.CanBuildByChaining(card) || PaymentPlanner.Plan(player, card.Cost) != null)
            {
                buildableCards.Add(card);
            }
        }
        return buildableCards;
    }

    /// <summary>
    /// The coins building the card would cost the player now (trade plus the card's own coin cost; 0 when chained),
    /// or null if they can't build it. Changes nothing.
    /// </summary>
    public static int? BuildPrice(this PlayerState player, Card card)
    {
        if (player.HasPlayedCard(card))
        {
            return null;
        }
        if (player.CanBuildByChaining(card))
        {
            return 0;
        }

        PaymentPlan? plan = PaymentPlanner.Plan(player, card.Cost);
        if (plan == null)
        {
            return null;
        }
        return plan.Value.Total + CoinCostOf(card);
    }

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
        if (player.HasPlayedCard(card) || !source.Contains(card))
        {
            return null;
        }
        if (free || player.CanBuildByChaining(card))
        {
            return new PlannedMove(player, action, card, source, PaymentPlan.Free, 0);
        }

        PaymentPlan? plan = PaymentPlanner.Plan(player, card.Cost);
        if (plan == null)
        {
            return null;
        }
        return new PlannedMove(player, action, card, source, plan.Value, CoinCostOf(card));
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

    /// <summary>The coins the card itself asks for (for example Tree Farm's 1 coin), 0 if none.</summary>
    private static int CoinCostOf(Card card)
    {
        if (card.Cost.TryGetValue(Resource.Coins, out int coins))
        {
            return coins;
        }
        return 0;
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

    /// <summary>The no-duplicates rule: true if the city already has a card with this name.</summary>
    private static bool HasPlayedCard(this PlayerState player, Card card)
    {
        foreach (Card playedCard in player.BuiltCards)
        {
            if (playedCard.SameNameAs(card))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Is the card free because the city has the card it chains from?</summary>
    private static bool CanBuildByChaining(this PlayerState player, Card card)
    {
        CardId freeWithId = card.FreeIfBuiltCardId;
        if (freeWithId == CardId.None)
        {
            return false;
        }

        // Forum is free with either trading post
        if (freeWithId == CardId.AnyTradingPost && HasBuiltEitherTradingPost(player))
        {
            return true;
        }

        foreach (Card playedCard in player.BuiltCards)
        {
            if (playedCard.Id == freeWithId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Has the city the East or the West Trading Post?</summary>
    private static bool HasBuiltEitherTradingPost(PlayerState player)
    {
        foreach (Card playedCard in player.BuiltCards)
        {
            if (playedCard.Id is CardId.EastTradingPost or CardId.WestTradingPost)
            {
                return true;
            }
        }
        return false;
    }
}
