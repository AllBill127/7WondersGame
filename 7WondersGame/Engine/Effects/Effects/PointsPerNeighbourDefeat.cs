using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Effects.Services;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Strategists Guild: victory points for each defeat token the neighbours hold at the end.</summary>
public sealed class PointsPerNeighbourDefeat : IEffect
{
    /// <summary>An effect worth <paramref name="victoryPoints"/> per defeat token of the neighbours.</summary>
    public PointsPerNeighbourDefeat(int victoryPoints)
    {
        Vp = victoryPoints;
    }

    /// <summary>The victory points per defeat token.</summary>
    public int Vp { get; }

    /// <inheritdoc/>
    public int EndGameVictoryPoints(PlayerState owner)
    {
        return Vp * Cities.CountDefeatTokens(owner, Who.Neighbours);
    }
}
