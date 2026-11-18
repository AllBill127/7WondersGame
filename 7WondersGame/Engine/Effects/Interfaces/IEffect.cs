using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Interfaces;

/// <summary>
/// What a card or wonder stage does. <see cref="OnBuild"/> runs once, when it is built;
/// <see cref="EndGameVictoryPoints"/> is its share of the final score. Effects are shared by every game, so they hold
/// no state.
/// </summary>
public interface IEffect
{
    /// <summary>Changes the owner's city when the card or stage is built. Does nothing unless overridden.</summary>
    void OnBuild(PlayerState owner)
    {
    }

    /// <summary>The victory points it is worth to its owner at the end of the game; 0 unless overridden.</summary>
    int EndGameVictoryPoints(PlayerState owner)
    {
        return 0;
    }

    /// <summary>The lasting abilities it gives, if any.</summary>
    Ability Grants
    {
        get
        {
            return Ability.None;
        }
    }
}
