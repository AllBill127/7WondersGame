using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Wonders.Dtos;

namespace _7Wonders.Engine.Rules.Services;

/// <summary>Where a player's abilities come from: the built cards and wonder stages that give them.</summary>
public static class EffectSources
{
    /// <summary>The player's built cards, then built wonder stages, that give <paramref name="ability"/>.</summary>
    public static List<IEffectSource> Granting(PlayerState player, Ability ability)
    {
        List<IEffectSource> sources = new();
        foreach (Card card in player.BuiltCards)
        {
            if (Grants(card.Effects, ability))
            {
                sources.Add(card);
            }
        }
        for (int stageIndex = 0; stageIndex < player.Wonder.BuiltStageCount; stageIndex++)
        {
            WonderStage stage = player.Wonder.Stages[stageIndex];
            if (Grants(stage.Effects, ability))
            {
                sources.Add(stage);
            }
        }
        return sources;
    }

    private static bool Grants(IReadOnlyList<IEffect> effects, Ability ability)
    {
        foreach (IEffect effect in effects)
        {
            if ((effect.Grants & ability) != 0)
            {
                return true;
            }
        }
        return false;
    }
}
