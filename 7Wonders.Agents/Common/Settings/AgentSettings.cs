using System.Reflection;
using System.Text.Json;
using _7Wonders.Agents.Common.Interfaces;

namespace _7Wonders.Agents.Common.Settings;

/// <summary>An agent's settings: what the results log and the command line can set.</summary>
internal static class AgentSettings
{
    /// <summary>
    /// The agent's settings as JSON, logged with every result row: its settings (see <see cref="Of"/>), sorted by
    /// name.
    /// </summary>
    public static string ConfigurationJson(this IAgent agent)
    {
        // StringComparer.Ordinal sorts by the characters' codes, the same on every machine (a culture's sort order
        // could differ between machines and change the JSON).
        SortedDictionary<string, object?> settingValues = new(StringComparer.Ordinal);
        foreach (AgentSetting setting in Of(agent.GetType()))
        {
            settingValues.Add(setting.Name, JsonValueOf(setting, agent));
        }
        return JsonSerializer.Serialize(settingValues);
    }

    /// <summary>
    /// An agent type's settings, sorted by name: its public <see cref="SettingAttribute"/> properties, and those of
    /// the type of each of its <see cref="SettingGroupAttribute"/> properties. Throws if two have the same name or a
    /// setting has no public setter.
    /// </summary>
    public static List<AgentSetting> Of(Type agentType)
    {
        // Reflection: the program reads the agent class's description at run time. GetProperties lists its public
        // instance properties (the BindingFlags say which kinds); IsDefined asks whether a property is marked with
        // the attribute, [Setting] or [SettingGroup]. So a new setting needs only the attribute, no list to update.
        List<AgentSetting> settings = new();
        foreach (PropertyInfo property in agentType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.IsDefined(typeof(SettingAttribute), inherit: true))
            {
                settings.Add(CheckedSetting(property, null));
            }
            if (property.IsDefined(typeof(SettingGroupAttribute), inherit: true))
            {
                AddGroupSettings(settings, property);
            }
        }
        CheckNamesDiffer(settings, agentType);
        // OrderBy sorts by what `setting => setting.Name` picks from each setting: the name.
        return settings.OrderBy(setting => setting.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>Adds the settings of the object type in the agent's <paramref name="group"/> property.</summary>
    private static void AddGroupSettings(List<AgentSetting> settings, PropertyInfo group)
    {
        PropertyInfo[] groupProperties = group.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (PropertyInfo property in groupProperties)
        {
            if (property.IsDefined(typeof(SettingAttribute), inherit: true))
            {
                settings.Add(CheckedSetting(property, group));
            }
        }
    }

    /// <summary>The setting of <paramref name="property"/>; throws if the command line couldn't set it.</summary>
    private static AgentSetting CheckedSetting(PropertyInfo property, PropertyInfo? group)
    {
        bool hasPublicGetter = property.GetGetMethod() != null;
        bool hasPublicSetter = property.GetSetMethod() != null;
        if (!hasPublicGetter || !hasPublicSetter)
        {
            throw new InvalidOperationException(
                $"The setting {property.Name} needs a public getter and setter, so the command line can set it.");
        }
        return new AgentSetting(property, group);
    }

    /// <summary>Throws if two settings have the same name (any case, as the command line reads them).</summary>
    private static void CheckNamesDiffer(List<AgentSetting> settings, Type agentType)
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (AgentSetting setting in settings)
        {
            if (!names.Add(setting.Name))
            {
                throw new InvalidOperationException($"{agentType.Name} has two settings named {setting.Name}.");
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
}
