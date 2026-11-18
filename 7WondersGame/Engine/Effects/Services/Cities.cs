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
                return player.AmountOfType(type);
            case Who.Neighbours:
                return player.PlayerEast.AmountOfType(type) + player.PlayerWest.AmountOfType(type);
            default:
                return player.PlayerEast.AmountOfType(type)
                    + player.PlayerWest.AmountOfType(type)
                    + player.AmountOfType(type);
        }
    }

    /// <summary>The wonder stages built in <paramref name="who"/>'s cities.</summary>
    public static int CountWonderStages(PlayerState player, Who who)
    {
        switch (who)
        {
            case Who.Self:
                return player.Board.Stage;
            case Who.Neighbours:
                return player.PlayerEast.Board.Stage + player.PlayerWest.Board.Stage;
            default:
                return player.PlayerEast.Board.Stage + player.PlayerWest.Board.Stage + player.Board.Stage;
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
                return player.PlayerEast.DefeatTokens + player.PlayerWest.DefeatTokens;
            default:
                return player.PlayerEast.DefeatTokens + player.PlayerWest.DefeatTokens + player.DefeatTokens;
        }
    }
}
