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

    /// <summary>Short names for settings (the full names work too, in any case).</summary>
    public static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "n", nameof(MctsConfig.Iterations) },
        { "eps", nameof(MctsConfig.Epsilon) },
    };

    private static readonly List<AgentCatalogEntry> BuiltIn = new()
    {
        new("MctsAgent", () => new MctsAgent(), "configurable MCTS (by default SO-ISMCTS, max^n, win reward, HeuristicAI rollouts); 3-4 players"),
        new("HeuristicAI", () => new HeuristicAI(), "values every move (card worth, price, wonder, denial); settings are its weights"),
        new("RandomChoiceAI", () => new RandomChoiceAI(), "random legal move"),
    };

    private static readonly List<AgentCatalogEntry> Registered = new();

    /// <summary>Every agent the command line knows: the built-in ones, then any added with <see cref="Register"/>.</summary>
    public static IReadOnlyList<AgentCatalogEntry> Entries
    {
        get { lock (Registered) return BuiltIn.Concat(Registered).ToList(); }
    }

    /// <summary>Adds an agent defined outside this project (the tests use it). A name already known is ignored.</summary>
    public static void Register(AgentCatalogEntry entry)
    {
        lock (Registered)
            if (!BuiltIn.Concat(Registered).Any(e => e.Name.Equals(entry.Name, StringComparison.OrdinalIgnoreCase)))
                Registered.Add(entry);
    }

    public static AgentCatalogEntry Find(string name) =>
        Entries.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new UsageException($"Unknown agent '{name}'. Run 'agents' to list them.");

    /// <summary>The settings of an agent type, with their defaults.</summary>
    public static List<(string Name, Type Type, object? Default)> SettingsOf(AgentCatalogEntry entry)
    {
        IAgent sample = entry.Create();
        return AgentSettings.Of(sample.GetType())
            .Select(s => (s.Name, s.Type, s.GetValue(sample)))
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The setting a key names (an alias or a field name, any case), or a usage error.</summary>
    public static AgentSetting ResolveSetting(Type agentType, string key)
    {
        string wanted = Aliases.TryGetValue(key, out string? full) ? full : key;
        var fields = AgentSettings.Of(agentType).ToList();
        foreach (var f in fields)
            if (f.Name.Equals(wanted, StringComparison.OrdinalIgnoreCase))
                return f;
        string known = fields.Count == 0 ? "it has no settings" : "known: " + string.Join(", ", fields.Select(f => f.Name));
        throw new UsageException($"{agentType.Name} has no setting '{key}' ({known}).");
    }

    /// <summary>
    /// Creates the agent and applies its settings. <paramref name="sweepValue"/> replaces the value of the
    /// setting that is a range (it must be given when the spec has one).
    /// </summary>
    public static IAgent Create(AgentSpec spec, double? sweepValue = null)
    {
        AgentCatalogEntry entry = Find(spec.Name);
        IAgent agent = entry.Create();
        foreach (var (key, value) in spec.Settings)
        {
            AgentSetting setting = ResolveSetting(agent.GetType(), key);
            string text = value;
            if (Sweep.TryParseRange(value, out _, out _))
                text = (sweepValue ?? throw new InvalidOperationException("range setting without a sweep value"))
                    .ToString("R", CultureInfo.InvariantCulture);
            setting.SetValue(agent, ConvertValue(text, setting.Type, $"{spec.Name}:{key}"));
        }
        return agent;
    }

    private static object ConvertValue(string text, Type type, string where)
    {
        try
        {
            if (type.IsEnum)
                return Enum.Parse(type, text, ignoreCase: true);
            if (type == typeof(bool))
                return bool.Parse(text);
            if (type == typeof(string))
                return text;
            if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            {
                // whole-number settings accept "2000" and also sweep values like "2000.0"
                double d = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                return Convert.ChangeType(Math.Round(d), type, CultureInfo.InvariantCulture);
            }
            return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or InvalidCastException)
        {
            throw new UsageException($"{where}: '{text}' is not a valid {type.Name}.");
        }
    }

    /// <summary>
    /// Checks a line-up before any game starts: every agent and setting exists, values parse, and at most one
    /// setting is a range. Returns that range, if any.
    /// </summary>
    public static Sweep? Validate(IReadOnlyList<AgentSpec> specs, int? steps)
    {
        Sweep? sweep = null;
        for (int seat = 0; seat < specs.Count; seat++)
        {
            foreach (var (key, value) in specs[seat].Settings)
            {
                if (!Sweep.TryParseRange(value, out double from, out double to))
                    continue;
                if (sweep != null)
                    throw new UsageException("Only one setting may be a range a..b.");
                if (steps is null or < 2)
                    throw new UsageException($"{specs[seat]} has a range; give --steps (2 or more values).");
                sweep = new Sweep(seat, key, from, to, steps.Value);
            }
        }
        if (sweep == null && steps != null)
            throw new UsageException("--steps only applies when a setting is a range a..b.");
        foreach (AgentSpec spec in specs)
            Create(spec, sweep?.From);  // throws a usage error for bad names or values
        return sweep;
    }

    /// <summary>The spec that rebuilds an agent from a results row (agent name + configJson).</summary>
    public static AgentSpec FromResultRow(string agent, string configJson)
    {
        var settings = new List<KeyValuePair<string, string>>();
        using JsonDocument doc = JsonDocument.Parse(configJson);
        foreach (JsonProperty p in doc.RootElement.EnumerateObject())
            settings.Add(new(p.Name, p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString()! : p.Value.GetRawText()));
        return new AgentSpec(agent, settings);
    }
}
