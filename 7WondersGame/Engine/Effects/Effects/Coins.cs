using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Coins when built.</summary>
public sealed record Coins(int Amount) : IEffect
{
    public void OnBuild(PlayerState p) => p.Resources[Resource.Coins] += Amount;
}
