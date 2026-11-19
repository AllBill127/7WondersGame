using System.Globalization;
using _7Wonders.Agents.Catalog;
using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Cli.Arguments;

namespace _7Wonders.Cli.Sweeps;

/// <summary>
/// A line-up as the command line writes it, where one setting may be a range <c>a..b</c> (a <see cref="Sweep"/>).
/// The catalog creates agents from plain values, so a range is replaced by one value first.
/// </summary>
public static class LineUp
{
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

        // creating every agent throws for bad names or values
        double? firstSweepValue = null;
        if (sweep != null)
        {
            firstSweepValue = sweep.From;
        }
        foreach (AgentSpecification specification in specifications)
        {
            CreateAgent(specification, firstSweepValue);
        }
        return sweep;
    }

    /// <summary>
    /// Creates the agent from the catalog. <paramref name="sweepValue"/> replaces the value of the setting that is
    /// a range (it must be given when the specification has one).
    /// </summary>
    public static IAgent CreateAgent(AgentSpecification specification, double? sweepValue)
    {
        List<KeyValuePair<string, string>> settings = new(specification.Settings.Count);
        foreach (KeyValuePair<string, string> setting in specification.Settings)
        {
            if (Sweep.TryParseRange(setting.Value, out _, out _))
            {
                settings.Add(new KeyValuePair<string, string>(setting.Key, SweepValueText(sweepValue)));
            }
            else
            {
                settings.Add(setting);
            }
        }
        return AgentCatalog.Create(new AgentSpecification(specification.Name, settings));
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
}
