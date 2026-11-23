using _7Wonders.Agents.Common.Interfaces;

namespace _7Wonders.Agents.Catalog;

/// <summary>An agent the command line can create: its name, its class, and its 'agents' list line.</summary>
public sealed class AgentCatalogEntry
{
    /// <summary>
    /// The agent called <paramref name="name"/>, an instance of <paramref name="agentType"/> (a class implementing
    /// <see cref="IAgent"/> with a constructor without parameters).
    /// </summary>
    public AgentCatalogEntry(string name, Type agentType, string description)
    {
        // typeof(IAgent) is the interface's run-time description; IsAssignableFrom asks whether the class implements
        // it.
        if (!typeof(IAgent).IsAssignableFrom(agentType))
        {
            throw new ArgumentException($"{agentType.Name} is not an agent.", nameof(agentType));
        }
        Name = name;
        AgentType = agentType;
        Description = description;
    }

    /// <summary>The name used in <c>--players</c>.</summary>
    public string Name { get; }

    /// <summary>The agent's class.</summary>
    public Type AgentType { get; }

    /// <summary>What the agent does, for the 'agents' list.</summary>
    public string Description { get; }

    /// <summary>Makes a new agent with its default settings.</summary>
    public IAgent Create()
    {
        // Activator.CreateInstance calls the class's constructor without parameters, for a class known only at run
        // time; `is not IAgent agent` checks the result is an agent and names it `agent`.
        object? created = Activator.CreateInstance(AgentType);
        if (created is not IAgent agent)
        {
            throw new InvalidOperationException($"{AgentType.Name} could not be created as an agent.");
        }
        return agent;
    }
}
