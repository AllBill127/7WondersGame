using _7WondersGame.Agents.Common.Interfaces;

namespace _7WondersGame.Cli.Agents;

/// <summary>An agent the command line can create: its name, how to make one, and its 'agents' list line.</summary>
public sealed class AgentCatalogEntry
{
    /// <summary>The agent called <paramref name="name"/>, made by <paramref name="create"/>.</summary>
    public AgentCatalogEntry(string name, Func<IAgent> create, string description)
    {
        Name = name;
        Create = create;
        Description = description;
    }

    /// <summary>The name used in <c>--players</c>.</summary>
    public string Name { get; }

    /// <summary>Makes a new agent with its default settings.</summary>
    public Func<IAgent> Create { get; }

    /// <summary>What the agent does, for the 'agents' list.</summary>
    public string Description { get; }
}
