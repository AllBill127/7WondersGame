using System.Reflection;
using _7WondersGame.Agents.Common.Interfaces;

namespace _7WondersGame.Agents.Common.Settings;

/// <summary>
/// One setting of an agent: a field of the agent, or of a <see cref="SettingsGroupAttribute"/> object it holds.
/// </summary>
public sealed class AgentSetting
{
    private readonly FieldInfo _field;
    private readonly FieldInfo? _group;

    /// <summary>
    /// A setting called <paramref name="name"/>, stored in <paramref name="field"/> of the agent, or of the
    /// object in the agent's <paramref name="group"/> field when there is one.
    /// </summary>
    internal AgentSetting(string name, FieldInfo field, FieldInfo? group)
    {
        Name = name;
        _field = field;
        _group = group;
    }

    /// <summary>The setting's name on the command line and in the results.</summary>
    public string Name { get; }

    /// <summary>The setting's value type.</summary>
    public Type Type
    {
        get
        {
            return _field.FieldType;
        }
    }

    /// <summary>The setting's value in <paramref name="agent"/>.</summary>
    public object? GetValue(IAgent agent)
    {
        return _field.GetValue(Owner(agent));
    }

    /// <summary>Sets the setting in <paramref name="agent"/> (in its settings group, if it has one).</summary>
    public void SetValue(IAgent agent, object? value)
    {
        _field.SetValue(Owner(agent), value);
    }

    /// <summary>The object holding the field: the agent itself, or its settings group.</summary>
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
