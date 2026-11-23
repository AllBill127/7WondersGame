using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Effects.Services;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Effects.Effects;

/// <summary>Victory points for each card of the given types in <see cref="Who"/>'s cities at the end.</summary>
internal sealed class PointsPerCard : IEffect
{
    /// <summary>
    /// An effect worth <paramref name="victoryPoints"/> per card of <paramref name="types"/> in
    /// <paramref name="who"/>'s cities.
    /// </summary>
    public PointsPerCard(int victoryPoints, Who who, params CardType[] types)
    {
        VictoryPoints = victoryPoints;
        Who = who;
        Types = types;
    }

    /// <summary>The victory points per card.</summary>
    public int VictoryPoints { get; }

    /// <summary>Whose cities are counted.</summary>
    public Who Who { get; }

    /// <summary>The kinds of card counted.</summary>
    public CardType[] Types { get; }

    /// <inheritdoc/>
    public int EndGameVictoryPoints(PlayerState owner)
    {
        int cardCount = 0;
        foreach (CardType type in Types)
        {
            cardCount += Cities.CountCards(owner, Who, type);
        }
        return VictoryPoints * cardCount;
    }
}
