using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Halikarnassos: at the end of this turn, build one card from the discard pile for free.</summary>
public sealed class BuildFromDiscardPile : IEffect
{
    /// <inheritdoc/>
    public void OnBuild(PlayerState owner)
    {
        owner.DiscardFree = true;
    }
}
