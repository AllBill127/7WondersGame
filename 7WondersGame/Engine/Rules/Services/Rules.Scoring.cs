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
    public static void Battle(this PlayerState p, int era)
    {
        int tokensForWin = VictoryTokensForWinningBattle(era);
        BattleWithNeighbour(p, p.PlayerEast, tokensForWin);
        BattleWithNeighbour(p, p.PlayerWest, tokensForWin);
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
    private static void BattleWithNeighbour(PlayerState p, PlayerState neighbour, int tokensForWin)
    {
        int shields = p.Resources[Resource.Shields];
        int neighbourShields = neighbour.Resources[Resource.Shields];
        if (shields > neighbourShields)
        {
            p.VictoryTokens += tokensForWin;
        }
        else if (shields < neighbourShields)
        {
            p.DefeatTokens += 1;
        }
    }

    /// <summary>
    /// Olympia B stage 3: at the end of the game, copy one guild built by a neighbour, as if built. <br/>
    /// - each candidate is scored with the real CalculateScore(); the best one is kept (ties go to the first
    ///   candidate, west then east) <br/>
    /// - the copy stays in PlayedCards, so Shipowners counts it and a copied Scientists Guild gives its wildcard <br/>
    /// - a guild the player already has is skipped (no two identical structures) <br/>
    /// - should be called before calculating scores <br/>
    /// Sources: https://rules.dized.com/7-wonders/rule/wonder-boards/the-statue-of-zeus-in-olympia,
    ///          https://rules.dized.com/7-wonders/rule/symbols/guilds
    /// </summary>
    /// <returns>true if a guild was copied; otherwise false</returns>
    public static bool CopyGuild(this PlayerState p)
    {
        if (!p.Has(Ability.CopyGuild))
        {
            return false;
        }

        Card? bestGuild = BestGuildToCopy(p);
        if (bestGuild == null)
        {
            return false;
        }
        p.PlayedCards.Add(bestGuild);
        return true;
    }

    /// <summary>
    /// The neighbours' guild that scores the player the most, or null if there is none to copy. Leaves the player
    /// as it was.
    /// </summary>
    private static Card? BestGuildToCopy(PlayerState p)
    {
        // guilds only score at the end (none has an effect when built), so trying one is adding and removing it
        int victoryPointsBeforeTrying = p.VictoryPoints;
        Card? bestGuild = null;
        int bestScore = int.MinValue;

        foreach (Card guild in p.PlayerWest.PlayedCards)
        {
            if (CanCopyGuild(p, guild))
            {
                KeepGuildIfBetter(p, guild, ref bestGuild, ref bestScore);
            }
        }
        foreach (Card guild in p.PlayerEast.PlayedCards)
        {
            // a guild both neighbours have is only tried once, from the west
            bool alreadyTried = p.PlayerWest.PlayedCards.Contains(guild);
            if (CanCopyGuild(p, guild) && !alreadyTried)
            {
                KeepGuildIfBetter(p, guild, ref bestGuild, ref bestScore);
            }
        }

        p.VictoryPoints = victoryPointsBeforeTrying;
        return bestGuild;
    }

    /// <summary>Is the card a guild the player doesn't have yet?</summary>
    private static bool CanCopyGuild(PlayerState p, Card card)
    {
        return card.Type == CardType.Guild && !p.HasPlayedCard(card);
    }

    /// <summary>
    /// Scores the player with <paramref name="guild"/> added, and keeps it as the best guild if it scores more than
    /// <paramref name="bestScore"/> (the first of equal ones stays).
    /// </summary>
    private static void KeepGuildIfBetter(PlayerState p, Card guild, ref Card? bestGuild, ref int bestScore)
    {
        p.PlayedCards.Add(guild);
        int score = p.CalculateScore();
        RemoveLastBuiltCard(p);
        if (score > bestScore)
        {
            bestScore = score;
            bestGuild = guild;
        }
    }

    /// <summary>Takes back the card added last to the player's city.</summary>
    private static void RemoveLastBuiltCard(PlayerState p)
    {
        int lastIndex = p.PlayedCards.Count - 1;
        p.PlayedCards.RemoveAt(lastIndex);
    }

    // =========== End of game scoring related ===========
    /// <summary>The player's victory points from civilian (blue) cards.</summary>
    public static int CalculateCivilianScore(this PlayerState p)
    {
        return p.CardPoints(CardType.Civilian);
    }

    /// <summary>The player's victory points from commercial (yellow) cards.</summary>
    public static int CalculateCommercialScore(this PlayerState p)
    {
        return p.CardPoints(CardType.Commercial);
    }

    /// <summary>The player's victory points from coins: 1 for every 3 coins.</summary>
    public static int CalculateMoneyScore(this PlayerState p)
    {
        return p.Resources[Resource.Coins] / CoinsPerVictoryPoint;
    }

    /// <summary>
    /// The player's victory points from guild (purple) cards (the Scientists Guild's wildcard is in the science
    /// score).
    /// </summary>
    public static int CalculateGuildScore(this PlayerState p)
    {
        return p.CardPoints(CardType.Guild);
    }

    /// <summary>The end-game VP of the player's cards of one type.</summary>
    private static int CardPoints(this PlayerState p, CardType type)
    {
        int vp = 0;
        foreach (Card card in p.PlayedCards)
        {
            if (card.Type != type)
            {
                continue;
            }
            for (int i = 0; i < card.Effects.Count; i++)
            {
                vp += card.Effects[i].EndGameVp(p);
            }
        }
        return vp;
    }

    /// <summary>The player's victory points from battles: victory tokens minus defeat tokens.</summary>
    public static int CalculateMilitaryScore(this PlayerState p)
    {
        return p.VictoryTokens - p.DefeatTokens;
    }

    /// <summary>The player's victory points from science symbols, with the wildcards placed best.</summary>
    public static int CalculateScientificScore(this PlayerState p)
    {
        int gear = p.Resources[Resource.Gear];
        int tablet = p.Resources[Resource.Tablet];
        int compass = p.Resources[Resource.Compass];
        return BestScienceScore(gear, compass, tablet, p.ScienceWildcards());
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
    public static int CalculateWonderScore(this PlayerState p)
    {
        return p.Board.WonderPoints(p);
    }

    /// <summary>
    /// Adds up the player's victory points from every category, stores the total in
    /// <see cref="PlayerState.VictoryPoints"/> and returns it.
    /// </summary>
    public static int CalculateScore(this PlayerState p)
    {
        int civilian = p.CalculateCivilianScore();
        int commercial = p.CalculateCommercialScore();
        int money = p.CalculateMoneyScore();
        int guilds = p.CalculateGuildScore();
        int science = p.CalculateScientificScore();
        int military = p.CalculateMilitaryScore();
        int wonder = p.CalculateWonderScore();

        int total = civilian + commercial + money + guilds + science + military + wonder;
        p.VictoryPoints = total;
        return p.VictoryPoints;
    }
}
