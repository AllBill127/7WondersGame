using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Effects.Services;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Coins when built, for each card of a type in <paramref name="Who"/>'s cities (built cards count).</summary>
public sealed record CoinsPerCard(CardType Type, int Coins, Who Who) : IEffect
{
    public void OnBuild(PlayerState p) => p.Resources[Resource.Coins] += Coins * Cities.CountCards(p, Who, Type);
}
