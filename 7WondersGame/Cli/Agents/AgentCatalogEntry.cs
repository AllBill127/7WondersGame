using _7WondersGame.Agents.Common.Interfaces;

namespace _7WondersGame.Cli.Agents;

public sealed record AgentCatalogEntry(string Name, Func<IAgent> Create, string Description);
