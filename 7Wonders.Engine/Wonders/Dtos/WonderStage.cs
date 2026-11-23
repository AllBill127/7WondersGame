using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Wonders.Enums;

namespace _7Wonders.Engine.Wonders.Dtos;

/// <summary>One wonder stage: what it costs and what it does.</summary>
public sealed class WonderStage : IEffectSource
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

    /// <summary>The board the stage is on (set by the board).</summary>
    public WonderId Wonder { get; internal set; }

    /// <summary>The stage's number on its board, from 1 (set by the board).</summary>
    public int Number { get; internal set; }

    /// <summary>The board and stage, e.g. "OlympiaA stage 2".</summary>
    public string Name
    {
        get
        {
            return $"{Wonder} stage {Number}";
        }
    }
}
