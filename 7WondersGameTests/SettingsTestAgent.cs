using _7WondersGame.src.cli;
using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests
{
    /// <summary>
    /// An agent with settings, for the command-line tests (the built-in agents have none until the MCTS agent of
    /// fix-plan step 6). It plays like <see cref="RandomChoiceAI"/>.
    /// </summary>
    internal class SettingsTestAgent : RandomChoiceAI
    {
        public const string Name = "SettingsTestAgent";

        public double C = 1.0;
        public int Iterations = 100;

        public SettingsTestAgent(int id) : base(id) { }

        public static void Register() =>
            AgentCatalog.Register(new AgentCatalog.Entry(Name, id => new SettingsTestAgent(id), "test agent with settings"));

        public override object DeepCopy()
        {
            var copy = (SettingsTestAgent)MemberwiseClone();
            copy.Board = (Wonder)Board.DeepCopy();
            copy.HandCards = HandCards.ToList();
            copy.PlayedCards = PlayedCards.ToList();
            copy.PlayableCards = PlayableCards.ToList();
            copy.Resources = Resources.ToDictionary(kv => kv.Key, kv => kv.Value);
            return copy;
        }
    }
}
