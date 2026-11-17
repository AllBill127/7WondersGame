using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Payment.Dtos;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Moves.Dtos;

/// <summary>
/// A move that has been checked and priced but not carried out yet (R5). Moves made at the same time go through
/// three steps, each for every player before the next: <see cref="Rules.PlanMove"/> against the turn-start state,
/// <see cref="Rules.Place"/>, then <see cref="Rules.Settle"/> (see <see cref="Game.ResolveMoves"/>).
/// </summary>
/// <param name="Source">Where the card comes from: the hand, or the discard pile (Halikarnassos).</param>
/// <param name="Payment">Coins to each neighbour for the resources bought from them.</param>
/// <param name="CoinCost">The card's own coin cost, paid to the bank.</param>
public sealed record PlannedMove(PlayerState Player, ActionType Action, Card Card, List<Card> Source, PaymentPlan Payment, int CoinCost)
{
    /// <summary>Halikarnassos: a free build from the discard pile, which uses up the effect.</summary>
    public bool FromDiscardPile { get; init; }
}
