using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>
/// Olympia A stage 2: one card per era for free. The ability is on from this stage; the first use is available
/// straight away, and <see cref="Game"/> re-arms it at the start of each era.
/// </summary>
public sealed record FreeCardEachEra : IEffect
{
    public void OnBuild(PlayerState p)
    {
        p.Abilities |= Ability.FreeCardEachEra;
        p.FreeCardOnce = true;
    }

    public Ability Grants => Ability.FreeCardEachEra;
}
