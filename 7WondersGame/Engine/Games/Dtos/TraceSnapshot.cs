using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Engine.Games.Dtos;

/// <summary>What the trace compares before and after a move: a player's coins, wonder stage and effect flags.</summary>
internal sealed class TraceSnapshot
{
    /// <summary>A snapshot with these values.</summary>
    public TraceSnapshot(int coins, int stage, List<string> flags)
    {
        Coins = coins;
        Stage = stage;
        Flags = flags;
    }

    /// <summary>The player's coins.</summary>
    public int Coins { get; }

    /// <summary>The player's built wonder stages.</summary>
    public int Stage { get; }

    /// <summary>The abilities and one-use charges the player has, by name.</summary>
    public List<string> Flags { get; }

    public static TraceSnapshot Of(PlayerState p)
    {
        var flags = Enum.GetValues<Ability>().Where(a => a != Ability.None && p.Has(a)).Select(a => a.ToString()).ToList();
        if (p.DiscardFree) flags.Add(nameof(p.DiscardFree));
        if (p.FreeCardOnce) flags.Add(nameof(p.FreeCardOnce));
        return new TraceSnapshot(p.Resources[Resource.Coins], p.Board.Stage, flags);
    }
}
