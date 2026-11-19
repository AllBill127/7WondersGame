using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Effects.Effects;

/// <summary>A lasting ability (see <see cref="Ability"/>).</summary>
internal sealed class Grant : IEffect
{
    /// <summary>An effect that gives the player <paramref name="ability"/> for the rest of the game.</summary>
    public Grant(Ability ability)
    {
        Ability = ability;
    }

    /// <summary>The ability given.</summary>
    public Ability Ability { get; }

    /// <inheritdoc/>
    public void OnBuild(PlayerState owner)
    {
        owner.Abilities |= Ability;
    }

    /// <inheritdoc/>
    public Ability Grants
    {
        get
        {
            return Ability;
        }
    }
}
