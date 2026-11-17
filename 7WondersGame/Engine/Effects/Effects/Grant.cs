using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>A lasting ability (see <see cref="Ability"/>).</summary>
public sealed record Grant(Ability Ability) : IEffect
{
    public void OnBuild(PlayerState p) => p.Abilities |= Ability;
    public Ability Grants => Ability;
}
