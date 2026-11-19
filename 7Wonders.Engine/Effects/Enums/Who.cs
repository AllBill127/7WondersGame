namespace _7Wonders.Engine.Effects.Enums;

/// <summary>Whose city a "per card" or "per stage" effect counts.</summary>
internal enum Who
{
    /// <summary>Only the player.</summary>
    Self,
    /// <summary>The west and east neighbours, not the player.</summary>
    Neighbours,
    /// <summary>The player and both neighbours.</summary>
    All,
}
