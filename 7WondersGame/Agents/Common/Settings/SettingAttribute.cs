namespace _7WondersGame.Agents.Common.Settings;

/// <summary>
/// Marks a public property (with a public getter and setter) as an agent setting: the command line can set it
/// (<c>--players Agent:Name=value</c>) and the results log it in the configurationJson column. Used on the agent's
/// own properties, or on the properties of an object the agent holds in a <see cref="SettingGroupAttribute"/>
/// property. Nothing else is a setting.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingAttribute : Attribute
{
}
