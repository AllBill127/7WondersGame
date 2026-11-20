using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Wonders.Dtos;

namespace _7Wonders.Engine.Effects.Interfaces;

/// <summary>What has effects: a <see cref="Card"/> or a <see cref="WonderStage"/>.</summary>
public interface IEffectSource
{
    /// <summary>The name to show: the card's name, or the board and stage, e.g. "OlympiaA stage 2".</summary>
    string Name { get; }
}
