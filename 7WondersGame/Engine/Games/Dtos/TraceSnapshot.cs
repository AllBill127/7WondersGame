using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Engine.Games.Dtos;

internal sealed record TraceSnapshot(int Coins, int Stage, List<string> Flags)
{
    public static TraceSnapshot Of(PlayerState p)
    {
        var flags = Enum.GetValues<Ability>().Where(a => a != Ability.None && p.Has(a)).Select(a => a.ToString()).ToList();
        if (p.DiscardFree) flags.Add(nameof(p.DiscardFree));
        if (p.FreeCardOnce) flags.Add(nameof(p.FreeCardOnce));
        return new TraceSnapshot(p.Resources[Resource.Coins], p.Board.Stage, flags);
    }
}
