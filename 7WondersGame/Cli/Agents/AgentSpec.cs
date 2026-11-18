using _7WondersGame.Cli.Arguments;

namespace _7WondersGame.Cli.Agents;

/// <summary>
/// One seat's agent as written on the command line: <c>Name[:setting=value...]</c>,
/// e.g. <c>Agent:c=1.5:iterations=2000</c>. A value may be a range <c>a..b</c> (see <see cref="Sweep"/>).
/// </summary>
public sealed class AgentSpec
{
    /// <summary>The agent <paramref name="name"/> with <paramref name="settings"/>, in the order written.</summary>
    public AgentSpec(string name, IReadOnlyList<KeyValuePair<string, string>> settings)
    {
        Name = name;
        Settings = settings;
    }

    /// <summary>The agent's name in the catalog.</summary>
    public string Name { get; }

    /// <summary>The settings written after the name, as setting/value pairs in the order written.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Settings { get; }

    /// <summary>Comma-separated specs, one per seat in seat order.</summary>
    public static List<AgentSpec> ParseList(string text)
    {
        var specs = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(Parse)
            .ToList();
        if (specs.Count == 0)
        {
            throw new UsageException("--players is empty.");
        }
        return specs;
    }

    /// <summary>One spec: the agent's name, then <c>setting=value</c> pairs, all separated by colons.</summary>
    public static AgentSpec Parse(string text)
    {
        string[] parts = text.Split(':', StringSplitOptions.TrimEntries);
        var settings = new List<KeyValuePair<string, string>>();
        foreach (string part in parts.Skip(1))
        {
            settings.Add(ParseSetting(part, text));
        }
        return new AgentSpec(parts[0], settings);
    }

    /// <summary>One <c>setting=value</c> part of <paramref name="specText"/>.</summary>
    private static KeyValuePair<string, string> ParseSetting(string part, string specText)
    {
        int equalsSignIndex = part.IndexOf('=');
        int lastIndex = part.Length - 1;
        if (equalsSignIndex <= 0 || equalsSignIndex == lastIndex)
        {
            throw new UsageException($"'{part}' in '{specText}' should be setting=value.");
        }

        int valueStart = equalsSignIndex + 1;
        string setting = part.Substring(0, equalsSignIndex).Trim();
        string value = part.Substring(valueStart).Trim();
        return new KeyValuePair<string, string>(setting, value);
    }

    /// <summary>The spec as written on the command line.</summary>
    public override string ToString()
    {
        return Name + string.Concat(Settings.Select(setting => $":{setting.Key}={setting.Value}"));
    }
}
