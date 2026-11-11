using _7WondersGame.src.cli;

namespace _7WondersGameTests
{
    /// <summary>
    /// An agent with plain field settings, for the command-line tests (cheap to play, unlike MctsAgent).
    /// It plays like <see cref="RandomChoiceAI"/>.
    /// </summary>
    internal class SettingsTestAgent : RandomChoiceAI
    {
        public double C = 1.0;
        public int Iterations = 100;

        public static void Register() =>
            AgentCatalog.Register(new AgentCatalog.Entry(nameof(SettingsTestAgent), () => new SettingsTestAgent(), "test agent with settings"));
    }
}
