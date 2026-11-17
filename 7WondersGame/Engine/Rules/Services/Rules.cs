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
/// The rules, as static functions over <see cref="PlayerState"/> (E5): checking, pricing and making a move,
/// card effects, and (in Rules.Scoring.cs) battles and scoring. Extension methods, so they read as
/// <c>player.PlanMove(...)</c>. The turn order is in <see cref="Game"/>.
/// </summary>
public static partial class Rules
{
    // =========== Wonder ===========
    /// <summary>Is there a next wonder stage, and can the player pay for it?</summary>
    public static bool CanBuildWonder(this PlayerState p) =>
        p.Board.NextStageCost is { } cost && PaymentPlanner.Plan(p, cost) != null;

    // =========== Card related ===========
    /// <summary>
    /// Make a new list of playable cards from a given card list <br/>
    /// - playAllFree flag allows all cards in the list to be played for free
    /// </summary>
    /// <param name="cardList"></param>
    /// <param name="playAllFree">
    /// a flag that allows all cards in the list to be played for free (DiscardFree / FreeCardOnce effect)</param>
    /// <returns>
    /// a new List of playable cards
    /// </returns>
    public static List<Card> GetPlayableCards(this PlayerState p, List<Card> cardList, bool playAllFree = false)
    {
        List<Card> playableCards = new List<Card>();
        foreach (Card card in cardList)
        {
            // the same card can't be built twice
            if (p.HasPlayedCard(card))
                continue;

            if (playAllFree || p.CanChainBuildCard(card) || PaymentPlanner.Plan(p, card.Cost) != null)
                playableCards.Add(card);
        }
        return playableCards;
    }

    /// <summary>
    /// The coins building the card would cost the player now (trade plus the card's own coin cost; 0 when chained),
    /// or null if they can't build it. Changes nothing.
    /// </summary>
    public static int? BuildPrice(this PlayerState p, Card card)
    {
        if (p.HasPlayedCard(card))
            return null;
        if (p.CanChainBuildCard(card))
            return 0;
        return PaymentPlanner.Plan(p, card.Cost) is { } plan ? plan.Total + card.Cost.GetValueOrDefault(Resource.Coins) : null;
    }

    // =========== Moves: plan, place, settle (R5) ===========
    /// <summary>
    /// Step 1: checks a move against the current state and works out what it costs, changing nothing.
    /// Null if the rules don't allow it. <paramref name="source"/> is where the card comes from.
    /// </summary>
    public static PlannedMove? PlanMove(this PlayerState p, ActionType action, Card card, List<Card> source) => action switch
    {
        ActionType.Build => p.PlanBuild(action, card, source, free: false),
        // Olympia A stage 2: one free card per era
        ActionType.BuildFreeOnce => p.Has(Ability.FreeCardEachEra) && p.FreeCardOnce
            ? p.PlanBuild(action, card, source, free: true) : null,
        ActionType.BuildWonder => p.PlanWonder(card, source),
        ActionType.Discard => source.Contains(card) ? new PlannedMove(p, action, card, source, PaymentPlan.Free, 0) : null,
        _ => null,
    };

    /// <summary>Step 1 for Halikarnassos: a free build from the discard pile while <see cref="PlayerState.DiscardFree"/> is on.</summary>
    public static PlannedMove? PlanDiscardPileBuild(this PlayerState p, Card card, List<Card> discardPile) =>
        p.DiscardFree && p.PlanBuild(ActionType.Build, card, discardPile, free: true) is { } move
            ? move with { FromDiscardPile = true }
            : null;

    private static PlannedMove? PlanBuild(this PlayerState p, ActionType action, Card card, List<Card> source, bool free)
    {
        if (p.HasPlayedCard(card) || !source.Contains(card))
            return null;
        if (free || p.CanChainBuildCard(card))
            return new PlannedMove(p, action, card, source, PaymentPlan.Free, 0);
        if (PaymentPlanner.Plan(p, card.Cost) is { } plan)
            return new PlannedMove(p, action, card, source, plan, card.Cost.TryGetValue(Resource.Coins, out int coins) ? coins : 0);

        return null;
    }

    private static PlannedMove? PlanWonder(this PlayerState p, Card card, List<Card> source)
    {
        if (p.Board.NextStageCost is { } cost && PaymentPlanner.Plan(p, cost) is { } plan && source.Contains(card))
            return new PlannedMove(p, ActionType.BuildWonder, card, source, plan, 0);
        return null;
    }

    /// <summary>
    /// Step 2: the player pays their own coins (to the neighbours and the bank) and the card leaves where it came
    /// from. A built card goes into the city. Nobody receives anything yet.
    /// </summary>
    public static void Place(this PlayerState p, PlannedMove move)
    {
        p.Resources[Resource.Coins] -= move.Payment.Total + move.CoinCost;
        move.Source.Remove(move.Card);
        // The card is in the city before its effect applies, so the Lighthouse counts itself.
        // Sources: https://rules.dized.com/7-wonders/rule/question/19075b63-591e-4e99-8dfc-5fa34b726973,
        //          https://rules.dized.com/7-wonders/rule/symbols/third-age-cards
        if (move.Action is ActionType.Build or ActionType.BuildFreeOnce)
            p.PlayedCards.Add(move.Card);
    }

    /// <summary>
    /// Step 3: the neighbours receive their trade coins and the move takes effect: the card's effect, the wonder
    /// stage, or 3 coins for a discard. A discarded card goes to the discard pile in <see cref="Game.ResolveMoves"/>.
    /// </summary>
    public static void Settle(this PlayerState p, PlannedMove move)
    {
        p.PlayerWest.Resources[Resource.Coins] += move.Payment.ToWest;
        p.PlayerEast.Resources[Resource.Coins] += move.Payment.ToEast;
        switch (move.Action)
        {
            case ActionType.Build:
            case ActionType.BuildFreeOnce:
                p.ApplyCardEffect(move.Card);
                // used up: Olympia A's is re-armed at the start of the next era
                if (move.Action == ActionType.BuildFreeOnce)
                    p.FreeCardOnce = false;
                if (move.FromDiscardPile)
                    p.DiscardFree = false;
                break;
            case ActionType.BuildWonder:
                p.Board.AddStage(p);
                break;
            case ActionType.Discard:
                p.Resources[Resource.Coins] += 3;
                break;
        }
    }

    /// <summary>A card's effects when it is built: production, coins, shields, science or an ability (see <see cref="Card.Effects"/>).</summary>
    public static void ApplyCardEffect(this PlayerState p, Card card)
    {
        for (int i = 0; i < card.Effects.Count; i++)
            card.Effects[i].OnBuild(p);
    }

    /// <summary>The no-duplicates rule: true if the city already has a card with this name.</summary>
    private static bool HasPlayedCard(this PlayerState p, Card card)
    {
        foreach (Card playedCard in p.PlayedCards)
            if (playedCard.SameNameAs(card))
                return true;

        return false;
    }

    private static bool CanChainBuildCard(this PlayerState p, Card card)
    {
        CardId freeWithId = card.FreeWithId;

        if (freeWithId != CardId.None)
        {
            // Forum is free with either trading post
            if (freeWithId == CardId.AnyTradingPost)
                foreach (Card playedCard in p.PlayedCards)
                    if (playedCard.Id is CardId.EastTradingPost or CardId.WestTradingPost)
                        return true;

            // General case:
            foreach (Card playedCard in p.PlayedCards)
            {
                if (playedCard.Id == freeWithId)
                    return true;
            }
        }

        return false;
    }

    public static int AmountOfType(this PlayerState p, CardType cardType)
    {
        int amount = 0;
        foreach (Card card in p.PlayedCards)
            if (card.Type == cardType)
                amount++;

        return amount;
    }

    // =========== Abilities ===========
    /// <summary>Does a built card or wonder stage give the player this ability?</summary>
    public static bool Has(this PlayerState p, Ability ability) => ((p.Abilities | p.Board.Abilities) & ability) != 0;

    /// <summary>Science symbols of the player's choice: Scientists Guild, Babylon.</summary>
    public static int ScienceWildcards(this PlayerState p)
    {
        int wildcards = p.Board.ScienceWildcards;
        foreach (Card card in p.PlayedCards)
            wildcards += card.ScienceWildcards;
        return wildcards;
    }
}
