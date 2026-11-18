using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>
/// Olympia A stage 2: one card per era for free. The ability is on from this stage; the first use is available
/// straight away, and <see cref="Game"/> re-arms it at the start of each era.
/// </summary>
public sealed class OlympiaFreeBuildEachEra : IEffect
{
    /// <inheritdoc/>
    public void OnBuild(PlayerState owner)
    {
        owner.Abilities |= Ability.OlympiaFreeBuildEachEra;
        owner.OlympiaFreeBuildAvailable = true;
    }

    /// <inheritdoc/>
    public Ability Grants
    {
        get
        {
            return Ability.OlympiaFreeBuildEachEra;
        }
    }
}
