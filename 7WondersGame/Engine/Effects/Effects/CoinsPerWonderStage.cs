using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

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
    public void OnBuild(PlayerState p)
    {
        p.Resources[Resource.Coins] += Coins * p.Board.Stage;
    }
}
