using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Interfaces;

namespace _7WondersGame.Engine.Wonders.Dtos;

/// <summary>One wonder stage: what it costs and what it does (E3).</summary>
public sealed class WonderStage
{
    /// <summary>A stage costing <paramref name="cost"/> that has <paramref name="effects"/> when built.</summary>
    public WonderStage(IReadOnlyDictionary<Resource, int> cost, params IEffect[] effects)
    {
        Cost = cost;
        Effects = effects;
    }

    /// <summary>The resources (and coins) the stage costs.</summary>
    public IReadOnlyDictionary<Resource, int> Cost { get; }

    /// <summary>What the stage does when built and at the end of the game.</summary>
    public IEffect[] Effects { get; }
}
