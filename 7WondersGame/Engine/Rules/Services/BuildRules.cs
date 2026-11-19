using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Payment.Dtos;
using _7WondersGame.Engine.Payment.Services;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Rules.Services;

/// <summary>
/// Can a player build a card or their next wonder stage now, and what would it cost? Reads the state
/// and changes nothing.
/// </summary>
public static class BuildRules
{
    /// <summary>Is there a next wonder stage, and can the player pay for it?</summary>
    public static bool CanBuildWonderStage(PlayerState player)
    {
        IReadOnlyDictionary<Resource, int>? cost = player.Wonder.NextStageCost;
        if (cost == null)
        {
            return false;
        }
        return PaymentPlanner.Plan(player, cost) != null;
    }

    /// <summary>
    /// A new list of the cards in <paramref name="cards"/> the player can build now (not a duplicate, and chained
    /// or affordable). With <paramref name="everyCardFree"/> (Halikarnassos, Olympia A's free card) cost doesn't
    /// matter.
    /// </summary>
    public static List<Card> BuildableCards(PlayerState player, List<Card> cards, bool everyCardFree = false)
    {
        List<Card> buildableCards = new();
        foreach (Card card in cards)
        {
            // the same card can't be built twice
            if (HasBuiltCardNamed(player, card))
            {
                continue;
            }

            if (everyCardFree || CanBuildByChaining(player, card) || PaymentPlanner.Plan(player, card.Cost) != null)
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
    public static int? BuildPrice(PlayerState player, Card card)
    {
        if (HasBuiltCardNamed(player, card))
        {
            return null;
        }
        if (CanBuildByChaining(player, card))
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

    /// <summary>The coins the card itself asks for (for example Tree Farm's 1 coin), 0 if none.</summary>
    public static int CoinCostOf(Card card)
    {
        if (card.Cost.TryGetValue(Resource.Coins, out int coins))
        {
            return coins;
        }
        return 0;
    }

    /// <summary>The no-duplicates rule: true if the city already has a card with this name.</summary>
    public static bool HasBuiltCardNamed(PlayerState player, Card card)
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
    public static bool CanBuildByChaining(PlayerState player, Card card)
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
