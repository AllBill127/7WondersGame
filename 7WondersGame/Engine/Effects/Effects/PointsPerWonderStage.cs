using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Effects.Services;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Victory points for each built wonder stage in <paramref name="Who"/>'s cities at the end.</summary>
public sealed record PointsPerWonderStage(int Vp, Who Who) : IEffect
{
    public int EndGameVp(PlayerState p) => Vp * Cities.Count(p, Who, q => q.Board.Stage);
}
