using System.Globalization;
using System.Text.Json;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Common.Settings;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.Mcts;
using _7WondersGame.Agents.Mcts.Configuration;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Arguments;

namespace _7WondersGame.Cli.Agents;

/// <summary>The agents the command line can create, and how their settings are set.</summary>
public static class AgentCatalog
{
    private static readonly List<AgentCatalogEntry> BuiltIn = new()
    {
        new AgentCatalogEntry(
            "MctsAgent",
            typeof(MctsAgent),
            "configurable MCTS (by default SO-ISMCTS, max^n, win reward, HeuristicAgent rollouts); 3-4 players"),
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
    /// Every agent the command line knows: the built-in ones, then any added with <see cref="Register"/>.
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

    /// <summary>The agent called <paramref name="name"/> (any case), or a usage error.</summary>
    public static AgentCatalogEntry Find(string name)
    {
        AgentCatalogEntry? entry = EntryCalled(name, Entries);
        if (entry == null)
        {
            throw new UsageException($"Unknown agent '{name}'. Run 'agents' to list them.");
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
    public static List<(string Name, Type Type, object? Default)> SettingsOf(AgentCatalogEntry entry)
    {
        IAgent sample = entry.Create();
        List<(string Name, Type Type, object? Default)> settings = new();
        foreach (AgentSetting setting in AgentSettings.Of(sample.GetType()))
        {
            settings.Add((setting.Name, setting.Type, setting.GetValue(sample)));
        }
        return settings
            .OrderBy(setting => setting.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The setting a key names (its name, in any case), or a usage error.</summary>
    public static AgentSetting ResolveSetting(Type agentType, string key)
    {
        List<AgentSetting> fields = AgentSettings.Of(agentType).ToList();
        foreach (AgentSetting field in fields)
        {
            if (field.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return field;
            }
        }
        throw new UsageException($"{agentType.Name} has no setting '{key}' ({KnownSettingsText(fields)}).");
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
    /// Creates the agent and applies its settings. <paramref name="sweepValue"/> replaces the value of the
    /// setting that is a range (it must be given when the specification has one).
    /// </summary>
    public static IAgent Create(AgentSpecification specification, double? sweepValue = null)
    {
        AgentCatalogEntry entry = Find(specification.Name);
        IAgent agent = entry.Create();
        foreach ((string key, string value) in specification.Settings)
        {
            AgentSetting setting = ResolveSetting(agent.GetType(), key);
            string text = value;
            if (Sweep.TryParseRange(value, out _, out _))
            {
                text = SweepValueText(sweepValue);
            }
            setting.SetValue(agent, ConvertValue(text, setting.Type, $"{specification.Name}:{key}"));
        }
        return agent;
    }

    /// <summary>The value a range setting takes in this game, as text that parses back to the same number.</summary>
    private static string SweepValueText(double? sweepValue)
    {
        if (sweepValue == null)
        {
            throw new InvalidOperationException("range setting without a sweep value");
        }
        return sweepValue.Value.ToString("R", CultureInfo.InvariantCulture);
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
            throw new UsageException($"{where}: '{text}' is not a valid {type.Name}.");
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
    /// Checks a line-up before any game starts: every agent and setting exists, values parse, and at most one
    /// setting is a range. Returns that range, if any.
    /// </summary>
    public static Sweep? Validate(IReadOnlyList<AgentSpecification> specifications, int? steps)
    {
        Sweep? sweep = null;
        for (int seat = 0; seat < specifications.Count; seat++)
        {
            foreach ((string key, string value) in specifications[seat].Settings)
            {
                if (!Sweep.TryParseRange(value, out double from, out double to))
                {
                    continue;
                }
                if (sweep != null)
                {
                    throw new UsageException("Only one setting may be a range a..b.");
                }
                if (steps == null || steps.Value < 2)
                {
                    throw new UsageException($"{specifications[seat]} has a range; give --steps (2 or more values).");
                }
                sweep = new Sweep(seat, key, from, to, steps.Value);
            }
        }
        if (sweep == null && steps != null)
        {
            throw new UsageException("--steps only applies when a setting is a range a..b.");
        }

        // creating every agent throws a usage error for bad names or values
        double? firstSweepValue = null;
        if (sweep != null)
        {
            firstSweepValue = sweep.From;
        }
        foreach (AgentSpecification specification in specifications)
        {
            Create(specification, firstSweepValue);
        }
        return sweep;
    }

    /// <summary>The specification that rebuilds an agent from a results row (agent name + configurationJson).</summary>
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
    /// A setting's value in a configurationJson object as the command line writes it: a string without quotes.
    /// </summary>
    internal static string SettingValueText(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString()!;
        }
        return value.GetRawText();
    }
}
