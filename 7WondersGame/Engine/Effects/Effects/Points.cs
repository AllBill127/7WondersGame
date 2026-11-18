using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Victory points at the end of the game.</summary>
public sealed class Points : IEffect
{
    /// <summary>An effect worth <paramref name="victoryPoints"/> victory points at the end.</summary>
    public Points(int victoryPoints)
    {
        Vp = victoryPoints;
    }

    /// <summary>The victory points.</summary>
    public int Vp { get; }

    /// <inheritdoc/>
    public int EndGameVp(PlayerState owner)
    {
        return Vp;
    }
}
