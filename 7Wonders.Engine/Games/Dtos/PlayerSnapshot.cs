using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;

namespace _7Wonders.Engine.Games.Dtos;

/// <summary>
/// What a move's event compares before and after it: a player's coins, wonder stage and effect flags.
/// </summary>
internal sealed class PlayerSnapshot
{
    /// <summary>A snapshot with these values.</summary>
    public PlayerSnapshot(int coins, int builtStageCount, List<string> flags)
    {
        Coins = coins;
        BuiltStageCount = builtStageCount;
        Flags = flags;
    }

    /// <summary>The player's coins.</summary>
    public int Coins { get; }

    /// <summary>The player's built wonder stages.</summary>
    public int BuiltStageCount { get; }

    /// <summary>The abilities and one-use charges the player has, by name.</summary>
    public List<string> Flags { get; }

    /// <summary>The player's coins, wonder stage and flags now.</summary>
    public static PlayerSnapshot Of(PlayerState player)
    {
        List<string> flags = new();
        // Enum.GetValues lists every value Ability declares; each one past None is a single bit.
        foreach (Ability ability in Enum.GetValues<Ability>())
        {
            if (ability != Ability.None && PlayerAbilities.Has(player, ability))
            {
                flags.Add(ability.ToString());
            }
        }
        if (player.DiscardPileBuildPending)
        {
            flags.Add(nameof(player.DiscardPileBuildPending));
        }
        if (player.FreeBuildAvailable)
        {
            flags.Add(nameof(player.FreeBuildAvailable));
        }
        return new PlayerSnapshot(player.Resources[Resource.Coins], player.Wonder.BuiltStageCount, flags);
    }
}
