using System.Reflection;
using System.Text.Json;
using _7WondersGame.Agents.Common.Interfaces;

namespace _7WondersGame.Agents.Common.Settings;

/// <summary>An agent's settings: what the results log and the command line can set.</summary>
public static class AgentSettings
{
    /// <summary>
    /// The agent's settings as JSON, logged with every result row: its settings (see <see cref="Of"/>), sorted by
    /// name.
    /// </summary>
    public static string ConfigJson(this IAgent agent)
    {
        var config = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (AgentSetting setting in Of(agent.GetType()))
        {
            config.Add(setting.Name, JsonValueOf(setting, agent));
        }
        return JsonSerializer.Serialize(config);
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
            if (IsSettingsGroup(field.FieldType))
            {
                foreach (var (inner, innerField) in Fields(field.FieldType, includeGroups: false))
                {
                    if (seen.Add(inner))
                    {
                        yield return new AgentSetting(inner, innerField, field);
                    }
                }
            }
            else if (seen.Add(name))
            {
                yield return new AgentSetting(name, field, null);
            }
        }
    }

    /// <summary>The setting's value as it goes into the JSON: an enum by its name.</summary>
    private static object? JsonValueOf(AgentSetting setting, IAgent agent)
    {
        object? value = setting.GetValue(agent);
        if (value != null && setting.Type.IsEnum)
        {
            return value.ToString();
        }
        return value;
    }

    /// <summary>The type's setting fields and their names, the type's own first, then its base classes'.</summary>
    private static IEnumerable<(string Name, FieldInfo Field)> Fields(Type type, bool includeGroups)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        for (Type? t = type; t != null && t != typeof(object); t = t.BaseType)
        {
            foreach (var field in t.GetFields(flags))
            {
                if (IsSetting(field, includeGroups))
                {
                    yield return (SettingName(field), field);
                }
            }
        }
    }

    /// <summary>
    /// Whether the field is a setting: a non-readonly field of a simple type, or (when asked for) a settings group.
    /// </summary>
    private static bool IsSetting(FieldInfo field, bool includeGroups)
    {
        Type ft = field.FieldType;
        // a group is changed in place, so its own field may be readonly
        if (includeGroups && IsSettingsGroup(ft))
        {
            return true;
        }
        if (field.IsInitOnly)
        {
            return false;
        }
        return ft.IsPrimitive || ft.IsEnum || ft == typeof(string) || ft == typeof(decimal);
    }

    private static bool IsSettingsGroup(Type type)
    {
        return type.IsDefined(typeof(SettingsGroupAttribute), inherit: false);
    }

    /// <summary>The field's name, or for an auto-property's backing field the property's name.</summary>
    private static string SettingName(FieldInfo field)
    {
        if (field.Name.StartsWith("<"))
        {
            return BackingFieldPropertyName(field);
        }
        return field.Name;
    }

    /// <summary>The property name inside a backing field's name, "&lt;Name&gt;k__BackingField".</summary>
    private static string BackingFieldPropertyName(FieldInfo field)
    {
        int closingBracket = field.Name.IndexOf('>');
        int nameLength = closingBracket - 1;
        return field.Name.Substring(1, nameLength);
    }
}
