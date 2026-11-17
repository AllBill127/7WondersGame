using _7WondersGame.Cli.Arguments;

namespace _7WondersGame.Cli.Agents;

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
