using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Coins when built.</summary>
public sealed class Coins : IEffect
{
    /// <summary>An effect that gives <paramref name="amount"/> coins.</summary>
    public Coins(int amount)
    {
        Amount = amount;
    }

    /// <summary>The coins given.</summary>
    public int Amount { get; }

    /// <inheritdoc/>
    public void OnBuild(PlayerState p)
    {
        p.Resources[Resource.Coins] += Amount;
    }
}
