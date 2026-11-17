using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Interfaces;

namespace _7WondersGame.Engine.Wonders.Dtos;

/// <summary>One wonder stage: what it costs and what it does (E3).</summary>
public sealed record WonderStage(IReadOnlyDictionary<Resource, int> Cost, params IEffect[] Effects);
