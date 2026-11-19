using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>
/// A science symbol of the player's choice at the end (Scientists Guild, Babylon). Scored in
/// <see cref="Scoring.CalculateScientificScore"/>, since science points don't add up card by card.
/// </summary>
public sealed class ScienceWildcard : IEffect
{
}
