using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Payment.Dtos;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGameTests.TestHelpers;

/// <summary>
/// One player's move on its own, outside a turn: plan, place, settle, the way <see cref="Game.ResolveMoves"/> does
/// it for every player at once.
/// </summary>
internal static class MoveTestHelper
{
    /// <summary>
    /// Builds a card from <paramref name="source"/>: paid for (unless <paramref name="isFree"/> or chained), moved
    /// into the city, its effect applied.
    /// </summary>
    /// <returns>true if the card was built; otherwise false (nothing changes)</returns>
    public static bool BuildCard(PlayerState player, Card card, List<Card> source, bool isFree)
    {
        PlannedMove? plannedMove = PlanCard(player, card, source, isFree);
        return Resolve(player, plannedMove);
    }

    /// <summary>Builds the next wonder stage with a card from <paramref name="source"/>.</summary>
    /// <returns>true if the stage was built; otherwise false (nothing changes)</returns>
    public static bool BuildWonderStage(PlayerState player, Card card, List<Card> source)
    {
        PlannedMove? plannedMove = player.PlanMove(MoveAction.BuildWonderStage, card, source);
        return Resolve(player, plannedMove);
    }

    private static PlannedMove? PlanCard(PlayerState player, Card card, List<Card> source, bool isFree)
    {
        if (!isFree)
        {
            return player.PlanMove(MoveAction.Build, card, source);
        }

        List<Card> buildableForFree = player.BuildableCards(source, everyCardFree: true);
        if (!buildableForFree.Contains(card))
        {
            return null;
        }

        return new PlannedMove(player, MoveAction.Build, card, source, PaymentPlan.Free, 0);
    }

    private static bool Resolve(PlayerState player, PlannedMove? plannedMove)
    {
        if (plannedMove == null)
        {
            return false;
        }

        player.Place(plannedMove);
        player.Settle(plannedMove);
        return true;
    }
}
