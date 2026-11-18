using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Engine.Games.Dtos;

/// <summary>What the trace compares before and after a move: a player's coins, wonder stage and effect flags.</summary>
internal sealed class TraceSnapshot
{
    /// <summary>A snapshot with these values.</summary>
    public TraceSnapshot(int coins, int builtStageCount, List<string> flags)
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
    public static TraceSnapshot Of(PlayerState player)
    {
        var flags = new List<string>();
        foreach (Ability ability in Enum.GetValues<Ability>())
        {
            if (ability != Ability.None && player.Has(ability))
            {
                flags.Add(ability.ToString());
            }
        }
        if (player.HalikarnassosDiscardBuildPending)
        {
            flags.Add(nameof(player.HalikarnassosDiscardBuildPending));
        }
        if (player.OlympiaFreeBuildAvailable)
        {
            flags.Add(nameof(player.OlympiaFreeBuildAvailable));
        }
        return new TraceSnapshot(player.Resources[Resource.Coins], player.Wonder.BuiltStageCount, flags);
    }
}
