namespace _7Wonders.Cli.ResultsAnalysis;

/// <summary>A group of rows by player: an agent with one configuration (the swept agent apart).</summary>
public sealed class PlayerGroup
{
    /// <summary>The group <paramref name="key"/> of <paramref name="agent"/> with its configuration.</summary>
    public PlayerGroup(string key, string agent, string configurationJson, bool swept)
    {
        Key = key;
        Agent = agent;
        ConfigurationJson = configurationJson;
        Swept = swept;
    }

    /// <summary>The group's key (see <see cref="Analysis.KeyOf"/>).</summary>
    public string Key { get; }

    /// <summary>The agent's name.</summary>
    public string Agent { get; }

    /// <summary>The agent's settings, as the configurationJson column has them.</summary>
    public string ConfigurationJson { get; }

    /// <summary>Whether these are the rows of the swept agent of a range run.</summary>
    public bool Swept { get; }
}
