using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Moves.Enums;
using _7Wonders.Engine.Payment.Dtos;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;

namespace _7Wonders.Engine.Moves.Dtos;

/// <summary>
/// A move that has been checked and priced but not carried out yet. Moves made at the same time go through
/// three steps, each for every player before the next: <see cref="MovePlanning.PlanMove"/> against the turn-start
/// state, <see cref="MoveResolution.Place"/>, then <see cref="MoveResolution.Settle"/> (see
/// <see cref="Game.ResolveMoves"/>).
/// </summary>
internal sealed class PlannedMove
{
    /// <summary>
    /// A planned move, not marked as a free build from the discard pile (<see cref="AsDiscardPileBuild"/> marks it).
    /// </summary>
    /// <param name="player">The player making the move.</param>
    /// <param name="action">What the player does.</param>
    /// <param name="card">The card built, used for a wonder stage or discarded.</param>
    /// <param name="source">Where the card comes from: the hand, or the discard pile (Halikarnassos).</param>
    /// <param name="payment">Coins to each neighbour for the resources bought from them.</param>
    /// <param name="coinCost">The card's own coin cost, paid to the bank.</param>
    public PlannedMove(PlayerState player, MoveAction action, Card card, List<Card> source, PaymentPlan payment,
                       int coinCost)
    {
        Player = player;
        Action = action;
        Card = card;
        Source = source;
        Payment = payment;
        CoinCost = coinCost;
    }

    /// <summary>The player making the move.</summary>
    public PlayerState Player { get; }

    /// <summary>What the player does.</summary>
    public MoveAction Action { get; }

    /// <summary>The card built, used for a wonder stage or discarded.</summary>
    public Card Card { get; }

    /// <summary>Where the card comes from: the hand, or the discard pile (Halikarnassos).</summary>
    public List<Card> Source { get; }

    /// <summary>Coins to each neighbour for the resources bought from them.</summary>
    public PaymentPlan Payment { get; }

    /// <summary>The card's own coin cost, paid to the bank.</summary>
    public int CoinCost { get; }

    /// <summary>Halikarnassos: a free build from the discard pile, which uses up the effect.</summary>
    public bool FromDiscardPile { get; private set; }

    /// <summary>A copy of this move marked as Halikarnassos' free build from the discard pile.</summary>
    public PlannedMove AsDiscardPileBuild()
    {
        PlannedMove discardPileBuild = new(Player, Action, Card, Source, Payment, CoinCost);
        discardPileBuild.FromDiscardPile = true;
        return discardPileBuild;
    }
}
