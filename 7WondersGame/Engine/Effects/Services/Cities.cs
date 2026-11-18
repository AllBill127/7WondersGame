using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Engine.Effects.Services;

/// <summary>
/// Counts things in the cities a "per card" or "per stage" effect looks at (<see cref="Who"/>). Runs in every
/// scoring of a search's rollouts, so each count is a plain <c>switch</c> with no delegates.
/// </summary>
internal static class Cities
{
    /// <summary>The cards of <paramref name="type"/> built in <paramref name="who"/>'s cities.</summary>
    public static int CountCards(PlayerState player, Who who, CardType type)
    {
        switch (who)
        {
            case Who.Self:
                return player.CountBuiltCardsOfType(type);
            case Who.Neighbours:
                return player.EastNeighbour.CountBuiltCardsOfType(type)
                       + player.WestNeighbour.CountBuiltCardsOfType(type);
            default:
                return player.EastNeighbour.CountBuiltCardsOfType(type)
                    + player.WestNeighbour.CountBuiltCardsOfType(type)
                    + player.CountBuiltCardsOfType(type);
        }
    }

    /// <summary>The wonder stages built in <paramref name="who"/>'s cities.</summary>
    public static int CountWonderStages(PlayerState player, Who who)
    {
        switch (who)
        {
            case Who.Self:
                return player.Wonder.BuiltStageCount;
            case Who.Neighbours:
                return player.EastNeighbour.Wonder.BuiltStageCount + player.WestNeighbour.Wonder.BuiltStageCount;
            default:
                return player.EastNeighbour.Wonder.BuiltStageCount
                       + player.WestNeighbour.Wonder.BuiltStageCount
                       + player.Wonder.BuiltStageCount;
        }
    }

    /// <summary>The defeat tokens held in <paramref name="who"/>'s cities.</summary>
    public static int CountDefeatTokens(PlayerState player, Who who)
    {
        switch (who)
        {
            case Who.Self:
                return player.DefeatTokens;
            case Who.Neighbours:
                return player.EastNeighbour.DefeatTokens + player.WestNeighbour.DefeatTokens;
            default:
                return player.EastNeighbour.DefeatTokens + player.WestNeighbour.DefeatTokens + player.DefeatTokens;
        }
    }
}
