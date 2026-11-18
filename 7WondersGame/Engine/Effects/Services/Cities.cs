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
    public static int CountCards(PlayerState p, Who who, CardType type)
    {
        switch (who)
        {
            case Who.Self:
                return p.AmountOfType(type);
            case Who.Neighbours:
                return p.PlayerEast.AmountOfType(type) + p.PlayerWest.AmountOfType(type);
            default:
                return p.PlayerEast.AmountOfType(type) + p.PlayerWest.AmountOfType(type) + p.AmountOfType(type);
        }
    }

    /// <summary>The wonder stages built in <paramref name="who"/>'s cities.</summary>
    public static int CountWonderStages(PlayerState p, Who who)
    {
        switch (who)
        {
            case Who.Self:
                return p.Board.Stage;
            case Who.Neighbours:
                return p.PlayerEast.Board.Stage + p.PlayerWest.Board.Stage;
            default:
                return p.PlayerEast.Board.Stage + p.PlayerWest.Board.Stage + p.Board.Stage;
        }
    }

    /// <summary>The defeat tokens held in <paramref name="who"/>'s cities.</summary>
    public static int CountDefeatTokens(PlayerState p, Who who)
    {
        switch (who)
        {
            case Who.Self:
                return p.DefeatTokens;
            case Who.Neighbours:
                return p.PlayerEast.DefeatTokens + p.PlayerWest.DefeatTokens;
            default:
                return p.PlayerEast.DefeatTokens + p.PlayerWest.DefeatTokens + p.DefeatTokens;
        }
    }
}
