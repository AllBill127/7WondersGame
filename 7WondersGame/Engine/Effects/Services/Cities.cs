using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Engine.Effects.Services;

internal static class Cities
{
    public static int CountCards(PlayerState p, Who who, CardType type) => Count(p, who, q => q.AmountOfType(type));

    public static int Count(PlayerState p, Who who, Func<PlayerState, int> count) => who switch
    {
        Who.Self => count(p),
        Who.Neighbours => count(p.PlayerEast) + count(p.PlayerWest),
        _ => count(p.PlayerEast) + count(p.PlayerWest) + count(p),
    };
}
