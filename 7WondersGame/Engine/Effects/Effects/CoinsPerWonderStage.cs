using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Coins when built, for each of the player's built wonder stages.</summary>
public sealed record CoinsPerWonderStage(int Coins) : IEffect
{
    public void OnBuild(PlayerState p) => p.Resources[Resource.Coins] += Coins * p.Board.Stage;
}
