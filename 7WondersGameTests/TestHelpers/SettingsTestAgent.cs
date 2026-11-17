using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Agents;

namespace _7WondersGameTests.TestHelpers;

/// <summary>
/// An agent with plain field settings, for the command-line tests (cheap to play, unlike MctsAgent).
/// It plays like <see cref="RandomChoiceAI"/>.
/// </summary>
internal class SettingsTestAgent : RandomChoiceAI
{
    public double C = 1.0;
    public int Iterations = 100;

    public static void Register() =>
        AgentCatalog.Register(new AgentCatalogEntry(nameof(SettingsTestAgent), () => new SettingsTestAgent(), "test agent with settings"));
}
