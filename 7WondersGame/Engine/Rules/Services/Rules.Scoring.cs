using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Rules.Services;

public static partial class Rules
{
    private const int CoinsPerVictoryPoint = 3;
    private const int VictoryPointsPerScienceSet = 7;

    // =========== Before scoring ===========
    /// <summary>
    /// The player's battles with both neighbours at the end of an era: a victory token worth
    /// <see cref="VictoryTokensForWinningBattle"/> for each neighbour with fewer shields, a defeat token for each with
    /// more, nothing on a draw.
    /// </summary>
    public static void Battle(this PlayerState player, int era)
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
    /// come from their own <see cref="Battle"/>).
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

    /// <summary>
    /// Olympia B stage 3: at the end of the game, copy one guild built by a neighbour, as if built. <br/>
    /// - each candidate is scored with the real CalculateScore(); the best one is kept (ties go to the first
    ///   candidate, west then east) <br/>
    /// - the copy stays in BuiltCards, so Shipowners counts it and a copied Scientists Guild gives its wildcard <br/>
    /// - a guild the player already has is skipped (no two identical structures) <br/>
    /// - should be called before calculating scores <br/>
    /// Sources: https://rules.dized.com/7-wonders/rule/wonder-boards/the-statue-of-zeus-in-olympia,
    ///          https://rules.dized.com/7-wonders/rule/symbols/guilds
    /// </summary>
    /// <returns>true if a guild was copied; otherwise false</returns>
    public static bool CopyGuild(this PlayerState player)
    {
        if (!player.Has(Ability.CopyGuild))
        {
            return false;
        }

        Card? bestGuild = BestGuildToCopy(player);
        if (bestGuild == null)
        {
            return false;
        }
        player.BuiltCards.Add(bestGuild);
        return true;
    }

    /// <summary>
    /// The neighbours' guild that scores the player the most, or null if there is none to copy. Leaves the player
    /// as it was.
    /// </summary>
    private static Card? BestGuildToCopy(PlayerState player)
    {
        // guilds only score at the end (none has an effect when built), so trying one is adding and removing it
        int victoryPointsBeforeTrying = player.VictoryPoints;
        Card? bestGuild = null;
        int bestScore = int.MinValue;

        foreach (Card guild in player.WestNeighbour.BuiltCards)
        {
            if (CanCopyGuild(player, guild))
            {
                KeepGuildIfBetter(player, guild, ref bestGuild, ref bestScore);
            }
        }
        foreach (Card guild in player.EastNeighbour.BuiltCards)
        {
            // a guild both neighbours have is only tried once, from the west
            bool alreadyTried = player.WestNeighbour.BuiltCards.Contains(guild);
            if (CanCopyGuild(player, guild) && !alreadyTried)
            {
                KeepGuildIfBetter(player, guild, ref bestGuild, ref bestScore);
            }
        }

        player.VictoryPoints = victoryPointsBeforeTrying;
        return bestGuild;
    }

    /// <summary>Is the card a guild the player doesn't have yet?</summary>
    private static bool CanCopyGuild(PlayerState player, Card card)
    {
        return card.Type == CardType.Guild && !player.HasPlayedCard(card);
    }

    /// <summary>
    /// Scores the player with <paramref name="guild"/> added, and keeps it as the best guild if it scores more than
    /// <paramref name="bestScore"/> (the first of equal ones stays).
    /// </summary>
    private static void KeepGuildIfBetter(PlayerState player, Card guild, ref Card? bestGuild, ref int bestScore)
    {
        player.BuiltCards.Add(guild);
        int score = player.CalculateScore();
        RemoveLastBuiltCard(player);
        if (score > bestScore)
        {
            bestScore = score;
            bestGuild = guild;
        }
    }

    /// <summary>Takes back the card added last to the player's city.</summary>
    private static void RemoveLastBuiltCard(PlayerState player)
    {
        int lastIndex = player.BuiltCards.Count - 1;
        player.BuiltCards.RemoveAt(lastIndex);
    }

    // =========== End of game scoring related ===========
    /// <summary>The player's victory points from civilian (blue) cards.</summary>
    public static int CalculateCivilianScore(this PlayerState player)
    {
        return player.CardPoints(CardType.Civilian);
    }

    /// <summary>The player's victory points from commercial (yellow) cards.</summary>
    public static int CalculateCommercialScore(this PlayerState player)
    {
        return player.CardPoints(CardType.Commercial);
    }

    /// <summary>The player's victory points from coins: 1 for every 3 coins.</summary>
    public static int CalculateMoneyScore(this PlayerState player)
    {
        return player.Resources[Resource.Coins] / CoinsPerVictoryPoint;
    }

    /// <summary>
    /// The player's victory points from guild (purple) cards (the Scientists Guild's wildcard is in the science
    /// score).
    /// </summary>
    public static int CalculateGuildScore(this PlayerState player)
    {
        return player.CardPoints(CardType.Guild);
    }

    /// <summary>The end-game VP of the player's cards of one type.</summary>
    private static int CardPoints(this PlayerState player, CardType type)
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
    public static int CalculateMilitaryScore(this PlayerState player)
    {
        return player.VictoryTokens - player.DefeatTokens;
    }

    /// <summary>The player's victory points from science symbols, with the wildcards placed best.</summary>
    public static int CalculateScientificScore(this PlayerState player)
    {
        int gear = player.Resources[Resource.Gear];
        int tablet = player.Resources[Resource.Tablet];
        int compass = player.Resources[Resource.Compass];
        return BestScienceScore(gear, compass, tablet, player.ScienceWildcards());
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
    public static int CalculateWonderScore(this PlayerState player)
    {
        return player.Wonder.WonderPoints(player);
    }

    /// <summary>
    /// Adds up the player's victory points from every category, stores the total in
    /// <see cref="PlayerState.VictoryPoints"/> and returns it.
    /// </summary>
    public static int CalculateScore(this PlayerState player)
    {
        int civilian = player.CalculateCivilianScore();
        int commercial = player.CalculateCommercialScore();
        int money = player.CalculateMoneyScore();
        int guilds = player.CalculateGuildScore();
        int science = player.CalculateScientificScore();
        int military = player.CalculateMilitaryScore();
        int wonder = player.CalculateWonderScore();

        int total = civilian + commercial + money + guilds + science + military + wonder;
        player.VictoryPoints = total;
        return player.VictoryPoints;
    }
}
