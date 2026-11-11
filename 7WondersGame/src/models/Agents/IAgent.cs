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

        /// <summary>
        /// What the agent measured over its game (e.g. search time and iterations), as JSON for the results'
        /// agentStats column. An agent is made for one game. "{}" for none.
        /// </summary>
        string StatsJson() => "{}";
    }

    /// <summary>
    /// Marks a class whose settings count as the agent's own when an agent has a field of that type (e.g.
    /// <see cref="HeuristicWeights"/>). They are named without a prefix, so the command line reads
    /// <c>HeuristicAI:CoinValue=0.7</c>. The agent must make its own instance, since setting one changes it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SettingsGroupAttribute : Attribute { }

    /// <summary>One setting of an agent: a field of the agent, or of a <see cref="SettingsGroupAttribute"/> object it holds.</summary>
    public sealed class AgentSetting
    {
        private readonly FieldInfo _field;
        private readonly FieldInfo? _group;

        internal AgentSetting(string name, FieldInfo field, FieldInfo? group)
        {
            Name = name;
            _field = field;
            _group = group;
        }

        public string Name { get; }
        public Type Type => _field.FieldType;

        public object? GetValue(IAgent agent) => _field.GetValue(Owner(agent));
        public void SetValue(IAgent agent, object? value) => _field.SetValue(Owner(agent), value);

        private object Owner(IAgent agent) =>
            _group == null ? agent : _group.GetValue(agent) ?? throw new InvalidOperationException($"{_group.Name} is null");
    }

    /// <summary>An agent's settings: what the results log and the command line can set.</summary>
    public static class AgentSettings
    {
        /// <summary>
        /// The agent's settings as JSON, logged with every result row (B2): its settings (see <see cref="Of"/>),
        /// sorted by name.
        /// </summary>
        public static string ConfigJson(this IAgent agent)
        {
            var config = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            foreach (AgentSetting setting in Of(agent.GetType()))
            {
                object? value = setting.GetValue(agent);
                config.Add(setting.Name, setting.Type.IsEnum ? value?.ToString() : value);
            }
            return System.Text.Json.JsonSerializer.Serialize(config);
        }

        /// <summary>
        /// An agent's settings: the instance fields its classes declare with a primitive, enum, string or decimal type,
        /// except readonly ones (fixed when the agent is made, e.g. by its catalog entry). Auto-property backing fields
        /// are named after the property. A field whose type has <see cref="SettingsGroupAttribute"/> adds that type's
        /// settings (found the same way) under their own names.
        /// </summary>
        public static IEnumerable<AgentSetting> Of(Type agentType)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (name, field) in Fields(agentType, includeGroups: true))
            {
                if (field.FieldType.IsDefined(typeof(SettingsGroupAttribute), inherit: false))
                {
                    foreach (var (inner, innerField) in Fields(field.FieldType, includeGroups: false))
                        if (seen.Add(inner))
                            yield return new AgentSetting(inner, innerField, field);
                }
                else if (seen.Add(name))
                    yield return new AgentSetting(name, field, null);
            }
        }

        private static IEnumerable<(string Name, FieldInfo Field)> Fields(Type type, bool includeGroups)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type? t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var field in t.GetFields(flags))
                {
                    Type ft = field.FieldType;
                    // auto-property backing fields are named "<Name>k__BackingField"
                    string name = field.Name.StartsWith("<") ? field.Name[1..field.Name.IndexOf('>')] : field.Name;
                    // a group is changed in place, so its own field may be readonly
                    if (includeGroups && ft.IsDefined(typeof(SettingsGroupAttribute), inherit: false))
                        yield return (name, field);
                    else if (!field.IsInitOnly && (ft.IsPrimitive || ft.IsEnum || ft == typeof(string) || ft == typeof(decimal)))
                        yield return (name, field);
                }
            }
        }
    }
}
