using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Rules.Services;

/// <summary>
/// The end-of-game victory points: one method per category, and <see cref="CalculateScore"/> for the
/// total.
/// </summary>
public static class Scoring
{
    private const int CoinsPerVictoryPoint = 3;
    private const int VictoryPointsPerScienceSet = 7;

    /// <summary>
    /// Adds up the player's victory points from every category, stores the total in
    /// <see cref="PlayerState.VictoryPoints"/> and returns it.
    /// </summary>
    public static int CalculateScore(PlayerState player)
    {
        int civilian = CalculateCivilianScore(player);
        int commercial = CalculateCommercialScore(player);
        int money = CalculateMoneyScore(player);
        int guilds = CalculateGuildScore(player);
        int science = CalculateScientificScore(player);
        int military = CalculateMilitaryScore(player);
        int wonder = CalculateWonderScore(player);

        int total = civilian + commercial + money + guilds + science + military + wonder;
        player.VictoryPoints = total;
        return player.VictoryPoints;
    }

    /// <summary>The player's victory points from civilian (blue) cards.</summary>
    public static int CalculateCivilianScore(PlayerState player)
    {
        return CardPoints(player, CardType.Civilian);
    }

    /// <summary>The player's victory points from commercial (yellow) cards.</summary>
    public static int CalculateCommercialScore(PlayerState player)
    {
        return CardPoints(player, CardType.Commercial);
    }

    /// <summary>The player's victory points from coins: 1 for every 3 coins.</summary>
    public static int CalculateMoneyScore(PlayerState player)
    {
        return player.Resources[Resource.Coins] / CoinsPerVictoryPoint;
    }

    /// <summary>
    /// The player's victory points from guild (purple) cards (the Scientists Guild's wildcard is in the science
    /// score).
    /// </summary>
    public static int CalculateGuildScore(PlayerState player)
    {
        return CardPoints(player, CardType.Guild);
    }

    /// <summary>The end-game VP of the player's cards of one type.</summary>
    private static int CardPoints(PlayerState player, CardType type)
    {
        int victoryPoints = 0;
        foreach (Card card in player.BuiltCards)
        {
            if (card.Type != type)
            {
                continue;
            }
            for (int effectIndex = 0; effectIndex < card.Effects.Count; effectIndex++)
            {
                victoryPoints += card.Effects[effectIndex].EndGameVictoryPoints(player);
            }
        }
        return victoryPoints;
    }

    /// <summary>The player's victory points from battles: victory tokens minus defeat tokens.</summary>
    public static int CalculateMilitaryScore(PlayerState player)
    {
        return player.VictoryTokens - player.DefeatTokens;
    }

    /// <summary>The player's victory points from science symbols, with the wildcards placed best.</summary>
    public static int CalculateScientificScore(PlayerState player)
    {
        int gear = player.Resources[Resource.Gear];
        int tablet = player.Resources[Resource.Tablet];
        int compass = player.Resources[Resource.Compass];
        return BestScienceScore(gear, compass, tablet, PlayerAbilities.ScienceWildcards(player));
    }

    /// <summary>
    /// Science VP with each wildcard placed on the best symbol, found by trying every placement (3^k, k &lt;= 2).
    /// </summary>
    public static int BestScienceScore(int gear, int compass, int tablet, int wildcards)
    {
        if (wildcards == 0)
        {
            return ScienceScore(gear, compass, tablet);
        }

        int wildcardsLeft = wildcards - 1;
        int asGear = BestScienceScore(gear + 1, compass, tablet, wildcardsLeft);
        int asCompass = BestScienceScore(gear, compass + 1, tablet, wildcardsLeft);
        int asTablet = BestScienceScore(gear, compass, tablet + 1, wildcardsLeft);
        return Math.Max(asGear, Math.Max(asCompass, asTablet));
    }

    /// <summary>
    /// Rulebook science VP: each symbol type scores its count squared, and each complete set of three different
    /// symbols scores 7.
    /// </summary>
    public static int ScienceScore(int gear, int compass, int tablet)
    {
        int completedSets = Math.Min(gear, Math.Min(compass, tablet));
        int squares = gear * gear + compass * compass + tablet * tablet;
        return squares + VictoryPointsPerScienceSet * completedSets;
    }

    /// <summary>The player's victory points from built wonder stages.</summary>
    public static int CalculateWonderScore(PlayerState player)
    {
        return player.Wonder.WonderPoints(player);
    }
}
