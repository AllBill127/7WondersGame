using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Effects.Services;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Victory points for each card of the given types in <paramref name="Who"/>'s cities at the end.</summary>
public sealed record PointsPerCard(int Vp, Who Who, params CardType[] Types) : IEffect
{
    public int EndGameVp(PlayerState p) => Vp * Types.Sum(type => Cities.CountCards(p, Who, type));
}
