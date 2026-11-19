using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Rules.Services;

/// <summary>Counts in one player's city that the card effects and the heuristics use.</summary>
public static class CityCounts
{
    /// <summary>How many cards of one type (colour) the player has built.</summary>
    public static int CountBuiltCardsOfType(PlayerState player, CardType cardType)
    {
        int amount = 0;
        foreach (Card card in player.BuiltCards)
        {
            if (card.Type == cardType)
            {
                amount++;
            }
        }
        return amount;
    }
}
