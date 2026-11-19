using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Rules.Services;

namespace _7Wonders.Engine.Effects.Effects;

/// <summary>
/// A science symbol of the player's choice at the end (Scientists Guild, Babylon). Scored in
/// <see cref="Scoring.CalculateScientificScore"/>, since science points don't add up card by card.
/// </summary>
internal sealed class ScienceWildcard : IEffect
{
}
