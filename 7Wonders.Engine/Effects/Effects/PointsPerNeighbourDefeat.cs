using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Effects.Services;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Effects.Effects;

/// <summary>Strategists Guild: victory points for each defeat token the neighbours hold at the end.</summary>
internal sealed class PointsPerNeighbourDefeat : IEffect
{
    /// <summary>An effect worth <paramref name="victoryPoints"/> per defeat token of the neighbours.</summary>
    public PointsPerNeighbourDefeat(int victoryPoints)
    {
        VictoryPoints = victoryPoints;
    }

    /// <summary>The victory points per defeat token.</summary>
    public int VictoryPoints { get; }

    /// <inheritdoc/>
    public int EndGameVictoryPoints(PlayerState owner)
    {
        return VictoryPoints * Cities.CountDefeatTokens(owner, Who.Neighbours);
    }
}
