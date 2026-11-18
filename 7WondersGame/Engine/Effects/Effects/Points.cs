using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Victory points at the end of the game.</summary>
public sealed class Points : IEffect
{
    /// <summary>An effect worth <paramref name="victoryPoints"/> victory points at the end.</summary>
    public Points(int victoryPoints)
    {
        VictoryPoints = victoryPoints;
    }

    /// <summary>The victory points.</summary>
    public int VictoryPoints { get; }

    /// <inheritdoc/>
    public int EndGameVictoryPoints(PlayerState owner)
    {
        return VictoryPoints;
    }
}
