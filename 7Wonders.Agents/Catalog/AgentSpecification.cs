
namespace _7Wonders.Agents.Catalog;

/// <summary>
/// One seat's agent as written on the command line: <c>Name[:setting=value...]</c>,
/// e.g. <c>Agent:Weight=1.5:iterations=2000</c>.
/// </summary>
public sealed class AgentSpecification
{
    /// <summary>The agent <paramref name="name"/> with <paramref name="settings"/>, in the order written.</summary>
    public AgentSpecification(string name, IReadOnlyList<KeyValuePair<string, string>> settings)
    {
        Name = name;
        Settings = settings;
    }

    /// <summary>The agent's name in the catalog.</summary>
    public string Name { get; }

    /// <summary>The settings written after the name, as setting/value pairs in the order written.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Settings { get; }

    /// <summary>
    /// One specification: the agent's name, then <c>setting=value</c> pairs, all separated by colons. Throws an
    /// <see cref="AgentSpecificationException"/> for a setting not written as <c>setting=value</c>.
    /// </summary>
    public static AgentSpecification Parse(string text)
    {
        string[] parts = text.Split(':', StringSplitOptions.TrimEntries);
        List<KeyValuePair<string, string>> settings = new();
        foreach (string part in parts.Skip(1))
        {
            settings.Add(ParseSetting(part, text));
        }
        return new AgentSpecification(parts[0], settings);
    }

    /// <summary>One <c>setting=value</c> part of <paramref name="specText"/>.</summary>
    private static KeyValuePair<string, string> ParseSetting(string part, string specText)
    {
        int equalsSignIndex = part.IndexOf('=');
        int lastIndex = part.Length - 1;
        if (equalsSignIndex <= 0 || equalsSignIndex == lastIndex)
        {
            throw new AgentSpecificationException($"'{part}' in '{specText}' should be setting=value.");
        }

        int valueStart = equalsSignIndex + 1;
        string setting = part.Substring(0, equalsSignIndex).Trim();
        string value = part.Substring(valueStart).Trim();
        return new KeyValuePair<string, string>(setting, value);
    }

    /// <summary>The specification as written on the command line.</summary>
    public override string ToString()
    {
        return Name + string.Concat(Settings.Select(setting => $":{setting.Key}={setting.Value}"));
    }
}
