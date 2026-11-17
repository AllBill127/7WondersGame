using System.Reflection;
using _7WondersGame.Agents.Common.Interfaces;

namespace _7WondersGame.Agents.Common.Settings;

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
