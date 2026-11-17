using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Effects.Services;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Strategists Guild: victory points for each defeat token the neighbours hold at the end.</summary>
public sealed record PointsPerNeighbourDefeat(int Vp) : IEffect
{
    public int EndGameVp(PlayerState p) => Vp * Cities.Count(p, Who.Neighbours, q => q.DefeatTokens);
}
