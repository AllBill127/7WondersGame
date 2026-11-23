using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Effects.Services;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Effects.Effects;

/// <summary>Victory points for each built wonder stage in <see cref="Who"/>'s cities at the end.</summary>
internal sealed class PointsPerWonderStage : IEffect
{
    /// <summary>
    /// An effect worth <paramref name="victoryPoints"/> per wonder stage in <paramref name="who"/>'s cities.
    /// </summary>
    public PointsPerWonderStage(int victoryPoints, Who who)
    {
        VictoryPoints = victoryPoints;
        Who = who;
    }

    /// <summary>The victory points per wonder stage.</summary>
    public int VictoryPoints { get; }

    /// <summary>Whose cities are counted.</summary>
    public Who Who { get; }

    /// <inheritdoc/>
    public int EndGameVictoryPoints(PlayerState owner)
    {
        return VictoryPoints * Cities.CountWonderStages(owner, Who);
    }
}
