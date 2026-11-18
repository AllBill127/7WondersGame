using _7WondersGame.Agents.Heuristic;

namespace _7WondersGame.Agents.Common.Settings;

/// <summary>
/// Marks a class whose settings count as the agent's own when an agent has a field of that type (e.g.
/// <see cref="HeuristicWeights"/>). They are named without a prefix, so the command line reads
/// <c>HeuristicAgent:CoinValueInVictoryPoints=0.7</c>. The agent must make its own instance, since setting one changes
/// it.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SettingsGroupAttribute : Attribute { }
