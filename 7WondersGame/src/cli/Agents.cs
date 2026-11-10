using System.Globalization;
using System.Reflection;
using System.Text.Json;
using _7WondersGame.src.models.Agents;

namespace _7WondersGame.src.cli
{
    /// <summary>
    /// One seat's agent as written on the command line: <c>Name[:setting=value...]</c>,
    /// e.g. <c>Agent:c=1.5:iterations=2000</c>. A value may be a range <c>a..b</c> (see <see cref="Sweep"/>).
    /// </summary>
    public sealed record AgentSpec(string Name, IReadOnlyList<KeyValuePair<string, string>> Settings)
    {
        /// <summary>Comma-separated specs, one per seat in seat order.</summary>
        public static List<AgentSpec> ParseList(string text)
        {
            var specs = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(Parse).ToList();
            if (specs.Count == 0)
                throw new UsageException("--players is empty.");
            return specs;
        }

        public static AgentSpec Parse(string text)
        {
            string[] parts = text.Split(':', StringSplitOptions.TrimEntries);
            var settings = new List<KeyValuePair<string, string>>();
            foreach (string part in parts.Skip(1))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0 || eq == part.Length - 1)
                    throw new UsageException($"'{part}' in '{text}' should be setting=value.");
                settings.Add(new(part[..eq].Trim(), part[(eq + 1)..].Trim()));
            }
            return new AgentSpec(parts[0], settings);
        }

        public override string ToString() =>
            Name + string.Concat(Settings.Select(s => $":{s.Key}={s.Value}"));
    }

    /// <summary>
    /// A setting given as a range <c>a..b</c>: the run plays every deal once for each of <c>Steps</c> evenly spaced values
    /// from a to b (both included). Only one setting in a run may be a range.
    /// </summary>
    public sealed record Sweep(int Seat, string Setting, double From, double To, int Steps)
    {
        // same arithmetic as the old C sweep (minC + k * ((maxC - minC) / intervals)), so its values are reproduced exactly
        public double ValueAt(int step) => From + step * ((To - From) / (Steps - 1));

        public static bool TryParseRange(string value, out double from, out double to)
        {
            from = to = 0;
            int dots = value.IndexOf("..", StringComparison.Ordinal);
            return dots > 0 &&
                   double.TryParse(value[..dots], NumberStyles.Float, CultureInfo.InvariantCulture, out from) &&
                   double.TryParse(value[(dots + 2)..], NumberStyles.Float, CultureInfo.InvariantCulture, out to);
        }
    }

    /// <summary>The agents the command line can create, and how their settings are set.</summary>
    public static class AgentCatalog
    {
        public sealed record Entry(string Name, Func<IAgent> Create, string Description);

        /// <summary>
        /// Short names for settings (the full field names work too). Empty until the configurable MCTS agent
        /// (fix plan step 6) adds its own.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<Entry> BuiltIn = new()
        {
            new("DeterministicAI", () => new DeterministicAI(), "rule-based heuristic (era-specific card scores)"),
            new("RandomChoiceAI", () => new RandomChoiceAI(), "random legal move"),
            new("PriorityStructureRandomChoiceAI", () => new PriorityRandomChoiceAI(ActionPriority.Structure), "random card, prefers building a card"),
            new("PriorityWonderRandomChoiceAI", () => new PriorityRandomChoiceAI(ActionPriority.Wonder), "random card, prefers building a wonder stage"),
        };

        private static readonly List<Entry> Registered = new();

        /// <summary>Every agent the command line knows: the built-in ones, then any added with <see cref="Register"/>.</summary>
        public static IReadOnlyList<Entry> Entries
        {
            get { lock (Registered) return BuiltIn.Concat(Registered).ToList(); }
        }

        /// <summary>Adds an agent defined outside this project (the tests use it). A name already known is ignored.</summary>
        public static void Register(Entry entry)
        {
            lock (Registered)
                if (!BuiltIn.Concat(Registered).Any(e => e.Name.Equals(entry.Name, StringComparison.OrdinalIgnoreCase)))
                    Registered.Add(entry);
        }

        public static Entry Find(string name) =>
            Entries.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new UsageException($"Unknown agent '{name}'. Run 'agents' to list them.");

        /// <summary>The settable fields of an agent type, with their defaults.</summary>
        public static List<(string Name, Type Type, object? Default)> SettingsOf(Entry entry)
        {
            IAgent sample = entry.Create();
            return AgentSettings.Fields(sample.GetType())
                .Select(s => (s.Name, s.Field.FieldType, s.Field.GetValue(sample)))
                .OrderBy(s => s.Name, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>The setting a key names (an alias or a field name, any case), or a usage error.</summary>
        public static (string Name, FieldInfo Field) ResolveSetting(Type agentType, string key)
        {
            string wanted = Aliases.TryGetValue(key, out string? full) ? full : key;
            var fields = AgentSettings.Fields(agentType).ToList();
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
            Entry entry = Find(spec.Name);
            IAgent agent = entry.Create();
            foreach (var (key, value) in spec.Settings)
            {
                var (name, field) = ResolveSetting(agent.GetType(), key);
                string text = value;
                if (Sweep.TryParseRange(value, out _, out _))
                    text = (sweepValue ?? throw new InvalidOperationException("range setting without a sweep value"))
                        .ToString("R", CultureInfo.InvariantCulture);
                field.SetValue(agent, ConvertValue(text, field.FieldType, $"{spec.Name}:{key}"));
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
}
