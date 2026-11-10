using System.Reflection;

namespace _7WondersGame.src.models.Agents
{
    /// <summary>
    /// Decides one seat's moves (E5). The game asks it whenever the seat must decide (see
    /// <see cref="Game.PendingPlayers"/>); it only sees a <see cref="GameView"/>, a copy, so it can't change the game.
    /// </summary>
    public interface IAgent
    {
        /// <summary>The name in results and traces, and on the command line.</summary>
        string Name => GetType().Name;

        /// <summary>
        /// The move for the view's phase: in <see cref="Phase.ChooseMoves"/> and <see cref="Phase.SeventhCard"/> a
        /// move with a hand card; in <see cref="Phase.DiscardBuild"/> a Build of a discard-pile card, or Skip.
        /// </summary>
        Command Choose(GameView view);
    }

    /// <summary>An agent's settings: what the results log and the command line can set.</summary>
    public static class AgentSettings
    {
        /// <summary>
        /// The agent's settings as JSON, logged with every result row (B2): its settable fields (see
        /// <see cref="Fields"/>), sorted by name.
        /// </summary>
        public static string ConfigJson(this IAgent agent)
        {
            var config = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (name, field) in Fields(agent.GetType()))
                config.Add(name, field.FieldType.IsEnum ? field.GetValue(agent)?.ToString() : field.GetValue(agent));
            return System.Text.Json.JsonSerializer.Serialize(config);
        }

        /// <summary>
        /// An agent's settings: the instance fields its classes declare with a primitive, enum, string or decimal type,
        /// except readonly ones (fixed when the agent is made, e.g. by its catalog entry). Auto-property backing fields
        /// are named after the property.
        /// </summary>
        public static IEnumerable<(string Name, FieldInfo Field)> Fields(Type agentType)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (Type? t = agentType; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var field in t.GetFields(flags))
                {
                    Type ft = field.FieldType;
                    if (field.IsInitOnly || !(ft.IsPrimitive || ft.IsEnum || ft == typeof(string) || ft == typeof(decimal)))
                        continue;
                    // auto-property backing fields are named "<Name>k__BackingField"
                    string name = field.Name.StartsWith("<") ? field.Name[1..field.Name.IndexOf('>')] : field.Name;
                    if (seen.Add(name))
                        yield return (name, field);
                }
            }
        }
    }
}
