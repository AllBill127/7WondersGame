using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Effects.Services;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>Coins when built, for each card of a type in <see cref="Who"/>'s cities (built cards count).</summary>
public sealed class CoinsPerCard : IEffect
{
    /// <summary>
    /// An effect that gives <paramref name="coins"/> per card of <paramref name="type"/> in
    /// <paramref name="who"/>'s cities.
    /// </summary>
    public CoinsPerCard(CardType type, int coins, Who who)
    {
        Type = type;
        Coins = coins;
        Who = who;
    }

    /// <summary>The kind of card counted.</summary>
    public CardType Type { get; }

    /// <summary>The coins per card.</summary>
    public int Coins { get; }

    /// <summary>Whose cities are counted.</summary>
    public Who Who { get; }

    /// <inheritdoc/>
    public void OnBuild(PlayerState p)
    {
        p.Resources[Resource.Coins] += Coins * Cities.CountCards(p, Who, Type);
    }
}
