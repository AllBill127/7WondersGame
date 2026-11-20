using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Effects.Effects;

/// <summary>Halikarnassos: at the end of this turn, build one card from the discard pile for free.</summary>
internal sealed class BuildFromDiscardPile : IEffect
{
    /// <inheritdoc/>
    public void OnBuild(PlayerState owner)
    {
        owner.DiscardPileBuildPending = true;
    }
}
