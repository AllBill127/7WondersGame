using System.Globalization;
using System.Text.Json;
using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Agents.Common.Settings;
using _7Wonders.Agents.Heuristic;
using _7Wonders.Agents.Mcts;
using _7Wonders.Agents.Mcts.Configuration;
using _7Wonders.Agents.RandomChoice;

namespace _7Wonders.Agents.Catalog;

/// <summary>The agents that can be created by name, and how their settings are set.</summary>
public static class AgentCatalog
{
    private static readonly List<AgentCatalogEntry> BuiltIn = new()
    {
        new AgentCatalogEntry(
            "MctsAgent",
            typeof(MctsAgent),
            "configurable MCTS (by default SO-ISMCTS, max^n, win reward, HeuristicAgent rollouts)"),
        new AgentCatalogEntry(
            "HeuristicAgent",
            typeof(HeuristicAgent),
            "values every move (card worth, price, wonder, denial); settings are its weights"),
        new AgentCatalogEntry(
            "RandomAgent",
            typeof(RandomAgent),
            "random legal move"),
    };

    private static readonly List<AgentCatalogEntry> Registered = new();

    /// <summary>
    /// Every agent the catalog knows: the built-in ones, then any added with <see cref="Register"/>.
    /// </summary>
    public static IReadOnlyList<AgentCatalogEntry> Entries
    {
        get
        {
            lock (Registered)
            {
                return AllEntries();
            }
        }
    }

    /// <summary>
    /// Adds an agent defined outside this project (the tests use it). A name already known is ignored.
    /// </summary>
    public static void Register(AgentCatalogEntry entry)
    {
        lock (Registered)
        {
            if (EntryCalled(entry.Name, AllEntries()) == null)
            {
                Registered.Add(entry);
            }
        }
    }

    /// <summary>
    /// The agent called <paramref name="name"/> (any case). Throws an <see cref="AgentSpecificationException"/> if
    /// there is none.
    /// </summary>
    public static AgentCatalogEntry Find(string name)
    {
        AgentCatalogEntry? entry = EntryCalled(name, Entries);
        if (entry == null)
        {
            throw new AgentSpecificationException($"Unknown agent '{name}'. Run 'agents' to list them.");
        }
        return entry;
    }

    /// <summary>
    /// The built-in entries, then the registered ones. The caller holds the lock on the registered list.
    /// </summary>
    private static List<AgentCatalogEntry> AllEntries()
    {
        List<AgentCatalogEntry> entries = new(BuiltIn);
        entries.AddRange(Registered);
        return entries;
    }

    /// <summary>The first entry called <paramref name="name"/> (any case), or null.</summary>
    private static AgentCatalogEntry? EntryCalled(string name, IEnumerable<AgentCatalogEntry> entries)
    {
        foreach (AgentCatalogEntry entry in entries)
        {
            if (entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }
        return null;
    }

    /// <summary>The settings of an agent type, with their defaults.</summary>
    public static List<AgentSettingDefault> SettingsOf(AgentCatalogEntry entry)
    {
        IAgent sample = entry.Create();
        List<AgentSettingDefault> settings = new();
        foreach (AgentSetting setting in AgentSettings.Of(sample.GetType()))
        {
            settings.Add(new AgentSettingDefault(setting.Name, setting.Type, setting.GetValue(sample)));
        }
        return settings
            .OrderBy(setting => setting.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// The setting a key names (its name, in any case). Throws an <see cref="AgentSpecificationException"/> if the
    /// agent has no such setting.
    /// </summary>
    private static AgentSetting ResolveSetting(Type agentType, string key)
    {
        List<AgentSetting> fields = AgentSettings.Of(agentType).ToList();
        foreach (AgentSetting field in fields)
        {
            if (field.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return field;
            }
        }
        throw new AgentSpecificationException(
            $"{agentType.Name} has no setting '{key}' ({KnownSettingsText(fields)}).");
    }

    /// <summary>The settings an agent has, for an error message.</summary>
    private static string KnownSettingsText(List<AgentSetting> settings)
    {
        if (settings.Count == 0)
        {
            return "it has no settings";
        }
        return "known: " + string.Join(", ", settings.Select(setting => setting.Name));
    }

    /// <summary>
    /// Creates the agent and applies its settings. Throws an <see cref="AgentSpecificationException"/> for an
    /// unknown agent or setting, or a value the setting can't take.
    /// </summary>
    public static IAgent Create(AgentSpecification specification)
    {
        AgentCatalogEntry entry = Find(specification.Name);
        IAgent agent = entry.Create();
        foreach ((string key, string value) in specification.Settings)
        {
            AgentSetting setting = ResolveSetting(agent.GetType(), key);
            setting.SetValue(agent, ConvertValue(value, setting.Type, $"{specification.Name}:{key}"));
        }
        return agent;
    }

    private static object ConvertValue(string text, Type type, string where)
    {
        try
        {
            if (type.IsEnum)
            {
                return Enum.Parse(type, text, ignoreCase: true);
            }
            if (type == typeof(bool))
            {
                return bool.Parse(text);
            }
            if (type == typeof(string))
            {
                return text;
            }
            if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            {
                // whole-number settings accept "2000" and also sweep values like "2000.0"
                double roundedFrom = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                return Convert.ChangeType(Math.Round(roundedFrom), type, CultureInfo.InvariantCulture);
            }
            return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (IsBadValueError(exception))
        {
            throw new AgentSpecificationException($"{where}: '{text}' is not a valid {type.Name}.");
        }
    }

    /// <summary>Whether converting a setting's text failed because the text is not a value of the type.</summary>
    private static bool IsBadValueError(Exception exception)
    {
        return exception is FormatException
            || exception is OverflowException
            || exception is ArgumentException
            || exception is InvalidCastException;
    }

    /// <summary>
    /// The specification that rebuilds an agent from its name and its configuration JSON (as
    /// <see cref="IAgent.ConfigurationJson"/> writes it, e.g. in a results row).
    /// </summary>
    public static AgentSpecification FromResultRow(string agent, string configurationJson)
    {
        List<KeyValuePair<string, string>> settings = new();
        using (JsonDocument doc = JsonDocument.Parse(configurationJson))
        {
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                settings.Add(new KeyValuePair<string, string>(property.Name, SettingValueText(property.Value)));
            }
        }
        return new AgentSpecification(agent, settings);
    }

    /// <summary>
    /// A setting's value in a configuration JSON object as a specification writes it: a string without quotes.
    /// </summary>
    public static string SettingValueText(JsonElement value)
    {
        string? text = null;
        if (value.ValueKind == JsonValueKind.String)
        {
            text = value.GetString();
        }
        if (text == null)
        {
            return value.GetRawText();
        }
        return text;
    }
}
