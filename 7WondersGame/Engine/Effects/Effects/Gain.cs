using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Adds to a resource when built: production, coins or shields, or a science symbol.</summary>
public sealed record Gain(Resource Resource, int Amount = 1) : IEffect
{
    public void OnBuild(PlayerState p) => p.Resources[Resource] += Amount;
}
