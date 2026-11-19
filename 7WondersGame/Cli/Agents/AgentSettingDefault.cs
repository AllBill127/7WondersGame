namespace _7Wonders.Cli.Agents;

/// <summary>One setting of an agent type and its default value, for the 'agents' listing.</summary>
public sealed class AgentSettingDefault
{
    /// <summary>
    /// The setting <paramref name="name"/> of type <paramref name="type"/>, by default
    /// <paramref name="defaultValue"/>.
    /// </summary>
    public AgentSettingDefault(string name, Type type, object? defaultValue)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
    }

    /// <summary>The setting's name, as written in <c>--players</c>.</summary>
    public string Name { get; }

    /// <summary>The setting's type.</summary>
    public Type Type { get; }

    /// <summary>The value a new agent has.</summary>
    public object? DefaultValue { get; }
}
