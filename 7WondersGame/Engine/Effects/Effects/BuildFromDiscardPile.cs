using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Halikarnassos: at the end of this turn, build one card from the discard pile for free.</summary>
public sealed record BuildFromDiscardPile : IEffect
{
    public void OnBuild(PlayerState p) => p.DiscardFree = true;
}
