using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Rules.Services;

/// <summary>Olympia B stage 3: copying a neighbour's guild at the end of the game.</summary>
public static class GuildCopying
{
    /// <summary>
    /// Olympia B stage 3: at the end of the game, copy one guild built by a neighbour, as if built. <br/>
    /// - each candidate is scored with the real <see cref="Scoring.CalculateScore"/>; the best one is kept (ties go
    ///   to the first candidate, west then east) <br/>
    /// - the copy stays in BuiltCards, so Shipowners counts it and a copied Scientists Guild gives its wildcard <br/>
    /// - a guild the player already has is skipped (no two identical structures) <br/>
    /// - should be called before calculating scores <br/>
    /// Sources: https://rules.dized.com/7-wonders/rule/wonder-boards/the-statue-of-zeus-in-olympia,
    ///          https://rules.dized.com/7-wonders/rule/symbols/guilds
    /// </summary>
    /// <returns>true if a guild was copied; otherwise false</returns>
    public static bool CopyBestNeighbourGuild(PlayerState player)
    {
        if (!PlayerAbilities.Has(player, Ability.CopyGuild))
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
        return card.Type == CardType.Guild && !BuildRules.HasBuiltCardNamed(player, card);
    }

    /// <summary>
    /// Scores the player with <paramref name="guild"/> added, and keeps it as the best guild if it scores more than
    /// <paramref name="bestScore"/> (the first of equal ones stays).
    /// </summary>
    private static void KeepGuildIfBetter(PlayerState player, Card guild, ref Card? bestGuild, ref int bestScore)
    {
        player.BuiltCards.Add(guild);
        int score = Scoring.CalculateScore(player);
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
}
