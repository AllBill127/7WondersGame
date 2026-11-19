using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Effects.Effects;

/// <summary>Adds to a resource when built: production, coins or shields, or a science symbol.</summary>
public sealed class Gain : IEffect
{
    /// <summary>An effect that adds <paramref name="amount"/> of <paramref name="resource"/>.</summary>
    public Gain(Resource resource, int amount = 1)
    {
        Resource = resource;
        Amount = amount;
    }

    /// <summary>The resource that grows.</summary>
    public Resource Resource { get; }

    /// <summary>How much it grows by.</summary>
    public int Amount { get; }

    /// <inheritdoc/>
    public void OnBuild(PlayerState owner)
    {
        owner.Resources[Resource] += Amount;
    }
}
