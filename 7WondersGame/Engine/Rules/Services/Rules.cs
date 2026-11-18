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
    public static bool CanBuildWonder(this PlayerState p)
    {
        IReadOnlyDictionary<Resource, int>? cost = p.Board.NextStageCost;
        if (cost == null)
        {
            return false;
        }
        return PaymentPlanner.Plan(p, cost) != null;
    }

    // =========== Card related ===========
    /// <summary>
    /// A new list of the cards in <paramref name="cardList"/> the player can build now (not a duplicate, and chained
    /// or affordable). With <paramref name="playAllFree"/> (Halikarnassos, Olympia A's free card) cost doesn't matter.
    /// </summary>
    public static List<Card> GetPlayableCards(this PlayerState p, List<Card> cardList, bool playAllFree = false)
    {
        List<Card> playableCards = new List<Card>();
        foreach (Card card in cardList)
        {
            // the same card can't be built twice
            if (p.HasPlayedCard(card))
            {
                continue;
            }

            if (playAllFree || p.CanChainBuildCard(card) || PaymentPlanner.Plan(p, card.Cost) != null)
            {
                playableCards.Add(card);
            }
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
        {
            return null;
        }
        if (p.CanChainBuildCard(card))
        {
            return 0;
        }

        PaymentPlan? plan = PaymentPlanner.Plan(p, card.Cost);
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
    public static PlannedMove? PlanMove(this PlayerState p, ActionType action, Card card, List<Card> source)
    {
        switch (action)
        {
            case ActionType.Build:
                return p.PlanBuild(action, card, source, free: false);
            case ActionType.BuildFreeOnce:
                return p.PlanFreeOnceBuild(card, source);
            case ActionType.BuildWonder:
                return p.PlanWonder(card, source);
            case ActionType.Discard:
                return p.PlanDiscard(card, source);
            default:
                return null;
        }
    }

    /// <summary>
    /// Step 1 for Halikarnassos: a free build from the discard pile while <see cref="PlayerState.DiscardFree"/> is on.
    /// </summary>
    public static PlannedMove? PlanDiscardPileBuild(this PlayerState p, Card card, List<Card> discardPile)
    {
        if (!p.DiscardFree)
        {
            return null;
        }
        PlannedMove? move = p.PlanBuild(ActionType.Build, card, discardPile, free: true);
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
        this PlayerState p,
        ActionType action,
        Card card,
        List<Card> source,
        bool free)
    {
        if (p.HasPlayedCard(card) || !source.Contains(card))
        {
            return null;
        }
        if (free || p.CanChainBuildCard(card))
        {
            return new PlannedMove(p, action, card, source, PaymentPlan.Free, 0);
        }

        PaymentPlan? plan = PaymentPlanner.Plan(p, card.Cost);
        if (plan == null)
        {
            return null;
        }
        return new PlannedMove(p, action, card, source, plan.Value, CoinCostOf(card));
    }

    /// <summary>Olympia A stage 2: one free card per era, while the era's charge is unused.</summary>
    private static PlannedMove? PlanFreeOnceBuild(this PlayerState p, Card card, List<Card> source)
    {
        if (!p.Has(Ability.FreeCardEachEra) || !p.FreeCardOnce)
        {
            return null;
        }
        return p.PlanBuild(ActionType.BuildFreeOnce, card, source, free: true);
    }

    /// <summary>The next wonder stage, built with a card from <paramref name="source"/>.</summary>
    private static PlannedMove? PlanWonder(this PlayerState p, Card card, List<Card> source)
    {
        IReadOnlyDictionary<Resource, int>? cost = p.Board.NextStageCost;
        if (cost == null)
        {
            return null;
        }
        PaymentPlan? plan = PaymentPlanner.Plan(p, cost);
        if (plan == null || !source.Contains(card))
        {
            return null;
        }
        return new PlannedMove(p, ActionType.BuildWonder, card, source, plan.Value, 0);
    }

    /// <summary>Discarding a card from <paramref name="source"/> for coins; always allowed and free.</summary>
    private static PlannedMove? PlanDiscard(this PlayerState p, Card card, List<Card> source)
    {
        if (!source.Contains(card))
        {
            return null;
        }
        return new PlannedMove(p, ActionType.Discard, card, source, PaymentPlan.Free, 0);
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
    public static void Place(this PlayerState p, PlannedMove move)
    {
        p.Resources[Resource.Coins] -= move.Payment.Total + move.CoinCost;
        move.Source.Remove(move.Card);
        // The card is in the city before its effect applies, so the Lighthouse counts itself.
        // Sources: https://rules.dized.com/7-wonders/rule/question/19075b63-591e-4e99-8dfc-5fa34b726973,
        //          https://rules.dized.com/7-wonders/rule/symbols/third-age-cards
        if (move.Action is ActionType.Build or ActionType.BuildFreeOnce)
        {
            p.PlayedCards.Add(move.Card);
        }
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
                UseUpFreeBuildCharges(p, move);
                break;
            case ActionType.BuildWonder:
                p.Board.AddStage(p);
                break;
            case ActionType.Discard:
                p.Resources[Resource.Coins] += CoinsForDiscard;
                break;
        }
    }

    /// <summary>
    /// Turns off the one-use charge a free build used: Olympia A's free card (re-armed at the start of the next era)
    /// or Halikarnassos' discard pile build.
    /// </summary>
    private static void UseUpFreeBuildCharges(PlayerState p, PlannedMove move)
    {
        if (move.Action == ActionType.BuildFreeOnce)
        {
            p.FreeCardOnce = false;
        }
        if (move.FromDiscardPile)
        {
            p.DiscardFree = false;
        }
    }

    /// <summary>
    /// A card's effects when it is built: production, coins, shields, science or an ability
    /// (see <see cref="Card.Effects"/>).
    /// </summary>
    public static void ApplyCardEffect(this PlayerState p, Card card)
    {
        for (int i = 0; i < card.Effects.Count; i++)
        {
            card.Effects[i].OnBuild(p);
        }
    }

    /// <summary>The no-duplicates rule: true if the city already has a card with this name.</summary>
    private static bool HasPlayedCard(this PlayerState p, Card card)
    {
        foreach (Card playedCard in p.PlayedCards)
        {
            if (playedCard.SameNameAs(card))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Is the card free because the city has the card it chains from?</summary>
    private static bool CanChainBuildCard(this PlayerState p, Card card)
    {
        CardId freeWithId = card.FreeWithId;
        if (freeWithId == CardId.None)
        {
            return false;
        }

        // Forum is free with either trading post
        if (freeWithId == CardId.AnyTradingPost && HasBuiltEitherTradingPost(p))
        {
            return true;
        }

        foreach (Card playedCard in p.PlayedCards)
        {
            if (playedCard.Id == freeWithId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Has the city the East or the West Trading Post?</summary>
    private static bool HasBuiltEitherTradingPost(PlayerState p)
    {
        foreach (Card playedCard in p.PlayedCards)
        {
            if (playedCard.Id is CardId.EastTradingPost or CardId.WestTradingPost)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>How many cards of one type (colour) the player has built.</summary>
    public static int AmountOfType(this PlayerState p, CardType cardType)
    {
        int amount = 0;
        foreach (Card card in p.PlayedCards)
        {
            if (card.Type == cardType)
            {
                amount++;
            }
        }
        return amount;
    }

    // =========== Abilities ===========
    /// <summary>Does a built card or wonder stage give the player this ability?</summary>
    public static bool Has(this PlayerState p, Ability ability)
    {
        Ability allAbilities = p.Abilities | p.Board.Abilities;
        return (allAbilities & ability) != 0;
    }

    /// <summary>Science symbols of the player's choice: Scientists Guild, Babylon.</summary>
    public static int ScienceWildcards(this PlayerState p)
    {
        int wildcards = p.Board.ScienceWildcards;
        foreach (Card card in p.PlayedCards)
        {
            wildcards += card.ScienceWildcards;
        }
        return wildcards;
    }
}
