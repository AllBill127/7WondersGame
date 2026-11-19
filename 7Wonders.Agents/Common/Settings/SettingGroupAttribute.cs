using _7Wonders.Agents.Heuristic;

namespace _7Wonders.Agents.Common.Settings;

/// <summary>
/// Marks an agent's property that holds an object of settings (e.g. <see cref="HeuristicAgent.Weights"/>): the
/// object's <see cref="SettingAttribute"/> properties count as the agent's own settings. They are named without a
/// prefix, so the command line reads <c>HeuristicAgent:CoinValueInVictoryPoints=0.7</c>. The agent must make its
/// own instance, since setting one changes it.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class SettingGroupAttribute : Attribute
{
}
