using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Effects.Services;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Victory points for each built wonder stage in <see cref="Who"/>'s cities at the end.</summary>
public sealed class PointsPerWonderStage : IEffect
{
    /// <summary>
    /// An effect worth <paramref name="victoryPoints"/> per wonder stage in <paramref name="who"/>'s cities.
    /// </summary>
    public PointsPerWonderStage(int victoryPoints, Who who)
    {
        Vp = victoryPoints;
        Who = who;
    }

    /// <summary>The victory points per wonder stage.</summary>
    public int Vp { get; }

    /// <summary>Whose cities are counted.</summary>
    public Who Who { get; }

    /// <inheritdoc/>
    public int EndGameVp(PlayerState owner)
    {
        return Vp * Cities.CountWonderStages(owner, Who);
    }
}
