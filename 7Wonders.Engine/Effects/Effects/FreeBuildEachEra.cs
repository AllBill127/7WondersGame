using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Effects.Effects;

/// <summary>
/// Olympia A stage 2: one card per era for free. The ability is on from this stage; the first use is available
/// straight away, and <see cref="Game"/> re-arms it at the start of each era.
/// </summary>
internal sealed class FreeBuildEachEra : IEffect
{
    /// <inheritdoc/>
    public void OnBuild(PlayerState owner, IEffectSource source)
    {
        owner.Abilities |= Ability.FreeBuildEachEra;
        owner.FreeBuildAvailable = true;
    }

    /// <inheritdoc/>
    public Ability Grants
    {
        get
        {
            return Ability.FreeBuildEachEra;
        }
    }
}
