using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Agents;

namespace _7WondersGameTests.TestHelpers;

/// <summary>
/// An agent with plain field settings, for the command-line tests (cheap to play, unlike MctsAgent).
/// It plays like <see cref="RandomAgent"/>.
/// </summary>
/// <remarks>
/// Its settings are public fields on purpose: the catalog reads an agent's non-readonly primitive fields as its
/// settings, and these tests check exactly that.
/// </remarks>
internal class SettingsTestAgent : RandomAgent
{
    /// <summary>A decimal setting.</summary>
    public double Weight = 1.0;

    /// <summary>A whole-number setting.</summary>
    public int Iterations = 100;

    /// <summary>Adds this agent to the catalog under its class name.</summary>
    public static void Register()
    {
        AgentCatalog.Register(
            new AgentCatalogEntry(nameof(SettingsTestAgent), typeof(SettingsTestAgent), "test agent with settings"));
    }
}
