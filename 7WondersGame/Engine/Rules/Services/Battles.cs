using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Rules.Services;

/// <summary>The battles between neighbours at the end of each era.</summary>
public static class Battles
{
    /// <summary>
    /// The player's battles with both neighbours at the end of an era: a victory token worth
    /// <see cref="VictoryTokensForWinningBattle"/> for each neighbour with fewer shields, a defeat token for each with
    /// more, nothing on a draw.
    /// </summary>
    public static void FightEraBattles(PlayerState player, int era)
    {
        int tokensForWin = VictoryTokensForWinningBattle(era);
        BattleWithNeighbour(player, player.EastNeighbour, tokensForWin);
        BattleWithNeighbour(player, player.WestNeighbour, tokensForWin);
    }

    /// <summary>What winning one battle is worth: 1 in era I, 3 in era II, 5 in era III.</summary>
    public static int VictoryTokensForWinningBattle(int era)
    {
        switch (era)
        {
            case 2:
                return 3;
            case 3:
                return 5;
            default:
                return 1;
        }
    }

    /// <summary>
    /// One battle: the player gains a victory or a defeat token against one neighbour (the neighbour's own tokens
    /// come from their own <see cref="FightEraBattles"/>).
    /// </summary>
    private static void BattleWithNeighbour(PlayerState player, PlayerState neighbour, int tokensForWin)
    {
        int shields = player.Resources[Resource.Shields];
        int neighbourShields = neighbour.Resources[Resource.Shields];
        if (shields > neighbourShields)
        {
            player.VictoryTokens += tokensForWin;
        }
        else if (shields < neighbourShields)
        {
            player.DefeatTokens += 1;
        }
    }
}
