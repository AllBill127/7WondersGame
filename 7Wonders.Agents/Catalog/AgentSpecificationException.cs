namespace _7Wonders.Agents.Catalog;

/// <summary>
/// An agent specification the catalog can't create: an unknown agent or setting, a value its setting can't take,
/// or a setting not written as <c>setting=value</c>. The message says what is wrong.
/// </summary>
public sealed class AgentSpecificationException : Exception
{
    /// <summary>The mistake described by <paramref name="message"/>.</summary>
    public AgentSpecificationException(string message) : base(message)
    {
    }
}
