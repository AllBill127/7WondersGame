using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Rules.Services;

/// <summary>
/// The last two of the three steps that carry out a move (see <see cref="Game.ResolveMoves"/>), after
/// <see cref="MovePlanning"/>: placing it (the player pays and the card leaves its source), then settling
/// it (the neighbours are paid and the move takes effect).
/// </summary>
public static class MoveResolution
{
    /// <summary>The coins a discarded card gives.</summary>
    public const int CoinsForDiscard = 3;

    /// <summary>
    /// Step 2: the player pays their own coins (to the neighbours and the bank) and the card leaves where it came
    /// from. A built card goes into the city. Nobody receives anything yet.
    /// </summary>
    public static void Place(PlannedMove move)
    {
        PlayerState player = move.Player;
        player.Resources[Resource.Coins] -= move.Payment.Total + move.CoinCost;
        move.Source.Remove(move.Card);
        // The card is in the city before its effect applies, so the Lighthouse counts itself.
        // Sources: https://rules.dized.com/7-wonders/rule/question/19075b63-591e-4e99-8dfc-5fa34b726973,
        //          https://rules.dized.com/7-wonders/rule/symbols/third-age-cards
        if (move.Action is MoveAction.Build or MoveAction.BuildWithOlympiaFreeBuild)
        {
            player.BuiltCards.Add(move.Card);
        }
    }

    /// <summary>
    /// Step 3: the neighbours receive their trade coins and the move takes effect: the card's effect, the wonder
    /// stage, or 3 coins for a discard. A discarded card goes to the discard pile in <see cref="Game.ResolveMoves"/>.
    /// </summary>
    public static void Settle(PlannedMove move)
    {
        PlayerState player = move.Player;
        player.WestNeighbour.Resources[Resource.Coins] += move.Payment.ToWest;
        player.EastNeighbour.Resources[Resource.Coins] += move.Payment.ToEast;
        switch (move.Action)
        {
            case MoveAction.Build:
            case MoveAction.BuildWithOlympiaFreeBuild:
                CardEffects.ApplyCardEffect(player, move.Card);
                UseUpFreeBuildCharges(player, move);
                break;
            case MoveAction.BuildWonderStage:
                player.Wonder.AddStage(player);
                break;
            case MoveAction.Discard:
                player.Resources[Resource.Coins] += CoinsForDiscard;
                break;
        }
    }

    /// <summary>
    /// Turns off the one-use charge a free build used: Olympia A's free card (re-armed at the start of the next era)
    /// or Halikarnassos' discard pile build.
    /// </summary>
    private static void UseUpFreeBuildCharges(PlayerState player, PlannedMove move)
    {
        if (move.Action == MoveAction.BuildWithOlympiaFreeBuild)
        {
            player.OlympiaFreeBuildAvailable = false;
        }
        if (move.FromDiscardPile)
        {
            player.HalikarnassosDiscardBuildPending = false;
        }
    }
}
