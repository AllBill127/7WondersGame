using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Effects.Services;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Strategists Guild: victory points for each defeat token the neighbours hold at the end.</summary>
public sealed class PointsPerNeighbourDefeat : IEffect
{
    /// <summary>An effect worth <paramref name="vp"/> per defeat token of the neighbours.</summary>
    public PointsPerNeighbourDefeat(int vp)
    {
        Vp = vp;
    }

    /// <summary>The victory points per defeat token.</summary>
    public int Vp { get; }

    /// <inheritdoc/>
    public int EndGameVp(PlayerState p)
    {
        return Vp * Cities.CountDefeatTokens(p, Who.Neighbours);
    }
}
