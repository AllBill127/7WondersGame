using System.Reflection;
using _7WondersGame.Agents.Common.Interfaces;

namespace _7WondersGame.Agents.Common.Settings;

/// <summary>
/// One setting of an agent: a <see cref="SettingAttribute"/> property of the agent, or of the object in one of its
/// <see cref="SettingGroupAttribute"/> properties.
/// </summary>
public sealed class AgentSetting
{
    private readonly PropertyInfo _property;
    private readonly PropertyInfo? _group;

    /// <summary>
    /// The setting <paramref name="property"/> of the agent, or of the object in the agent's <paramref name="group"/>
    /// property when there is one. It is named after the property.
    /// </summary>
    internal AgentSetting(PropertyInfo property, PropertyInfo? group)
    {
        _property = property;
        _group = group;
    }

    /// <summary>The setting's name on the command line and in the results: the property's name.</summary>
    public string Name
    {
        get
        {
            return _property.Name;
        }
    }

    /// <summary>The setting's value type.</summary>
    public Type Type
    {
        get
        {
            return _property.PropertyType;
        }
    }

    /// <summary>The setting's value in <paramref name="agent"/>.</summary>
    public object? GetValue(IAgent agent)
    {
        return _property.GetValue(Owner(agent));
    }

    /// <summary>Sets the setting in <paramref name="agent"/> (in its settings group, if it has one).</summary>
    public void SetValue(IAgent agent, object? value)
    {
        _property.SetValue(Owner(agent), value);
    }

    /// <summary>The object holding the property: the agent itself, or its settings group.</summary>
    private object Owner(IAgent agent)
    {
        if (_group == null)
        {
            return agent;
        }
        object? groupObject = _group.GetValue(agent);
        if (groupObject == null)
        {
            throw new InvalidOperationException($"{_group.Name} is null");
        }
        return groupObject;
    }
}
