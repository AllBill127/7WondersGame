using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Victory points at the end of the game.</summary>
public sealed record Points(int Vp) : IEffect
{
    public int EndGameVp(PlayerState p) => Vp;
}
