using _7Wonders.Agents.Catalog;
using _7Wonders.Agents.Common.Settings;
using _7Wonders.Agents.RandomChoice;

namespace _7Wonders.Tests.TestHelpers;

/// <summary>
/// An agent with settings of its own (not in a setting group), for the command-line tests (cheap to play, unlike
/// MctsAgent). It plays like <see cref="RandomAgent"/>.
/// </summary>
internal class SettingsTestAgent : RandomAgent
{
    /// <summary>A decimal setting.</summary>
    [Setting]
    public double Weight { get; set; } = 1.0;

    /// <summary>A whole-number setting.</summary>
    [Setting]
    public int Iterations { get; set; } = 100;

    /// <summary>A public property without <see cref="SettingAttribute"/>: not a setting.</summary>
    public int NotASetting { get; set; } = 7;

    /// <summary>Adds this agent to the catalog under its class name.</summary>
    public static void Register()
    {
        AgentCatalog.Register(
            new AgentCatalogEntry(nameof(SettingsTestAgent), typeof(SettingsTestAgent), "test agent with settings"));
    }
}
