using System.Reflection;
using _7WondersGame.Agents.Common.Interfaces;

namespace _7WondersGame.Agents.Common.Settings;

/// <summary>One setting of an agent: a field of the agent, or of a <see cref="SettingsGroupAttribute"/> object it holds.</summary>
public sealed class AgentSetting
{
    private readonly FieldInfo _field;
    private readonly FieldInfo? _group;

    internal AgentSetting(string name, FieldInfo field, FieldInfo? group)
    {
        Name = name;
        _field = field;
        _group = group;
    }

    public string Name { get; }
    public Type Type => _field.FieldType;

    public object? GetValue(IAgent agent) => _field.GetValue(Owner(agent));
    public void SetValue(IAgent agent, object? value) => _field.SetValue(Owner(agent), value);

    private object Owner(IAgent agent) =>
        _group == null ? agent : _group.GetValue(agent) ?? throw new InvalidOperationException($"{_group.Name} is null");
}
