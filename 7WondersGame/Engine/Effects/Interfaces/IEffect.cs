using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Interfaces;

/// <summary>
/// What a card or wonder stage does (E3). <see cref="OnBuild"/> runs once, when it is built;
/// <see cref="EndGameVp"/> is its share of the final score. Effects are shared by every game, so they hold no state.
/// </summary>
public interface IEffect
{
    void OnBuild(PlayerState p) { }
    int EndGameVp(PlayerState p) => 0;
    /// <summary>The lasting abilities it gives, if any.</summary>
    Ability Grants => Ability.None;
}
