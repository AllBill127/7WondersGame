using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Effects.Effects;

/// <summary>Coins when built, for each of the player's built wonder stages.</summary>
public sealed class CoinsPerWonderStage : IEffect
{
    /// <summary>An effect that gives <paramref name="coins"/> per built wonder stage.</summary>
    public CoinsPerWonderStage(int coins)
    {
        Coins = coins;
    }

    /// <summary>The coins per wonder stage.</summary>
    public int Coins { get; }

    /// <inheritdoc/>
    public void OnBuild(PlayerState owner)
    {
        owner.Resources[Resource.Coins] += Coins * owner.Wonder.BuiltStageCount;
    }
}
