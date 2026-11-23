using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Rules.Services;

/// <summary>What building a card does: each of its effects' <c>OnBuild</c>.</summary>
public static class CardEffects
{
    /// <summary>
    /// A card's effects when it is built: production, coins, shields, science or an ability
    /// (see <see cref="Card.Effects"/>).
    /// </summary>
    public static void ApplyCardEffect(PlayerState player, Card card)
    {
        for (int effectIndex = 0; effectIndex < card.Effects.Count; effectIndex++)
        {
            card.Effects[effectIndex].OnBuild(player, card);
        }
    }
}
