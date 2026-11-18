using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Effects;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Wonders.Dtos;
using _7WondersGame.Engine.Wonders.Enums;
using static _7WondersGame.Engine.Cards.Enums.Resource;

namespace _7WondersGame.Engine.Wonders.Data;

/// <summary>
/// Every wonder board: its starting resource and, for each stage in building order, the cost and the effects.
/// Shared by every game and never changed. Checked against the rulebook in WonderDataTests.
/// </summary>
public static class WonderBoards
{
    /// <summary>A stage's cost from (resource, amount) pairs.</summary>
    private static IReadOnlyDictionary<Resource, int> Cost(params (Resource Resource, int Amount)[] parts)
    {
        return parts.ToDictionary(part => part.Resource, part => part.Amount);
    }

    /// <summary>A stage costing <paramref name="cost"/> with <paramref name="effects"/>.</summary>
    private static WonderStage Stage(IReadOnlyDictionary<Resource, int> cost, params IEffect[] effects)
    {
        return new WonderStage(cost, effects);
    }

    /// <summary>A board with its stages in building order.</summary>
    private static WonderBoard Board(WonderId id, string name, Resource production, params WonderStage[] stages)
    {
        return new WonderBoard(id, name, production, stages);
    }

    /// <summary>Every board, both sides, by id.</summary>
    public static readonly IReadOnlyDictionary<WonderId, WonderBoard> All = new[]
    {
        // ---------- side A ----------
        Board(WonderId.GizahA, "Gizah A", Stone,
            Stage(Cost((Stone, 2)), new Points(3)),
            Stage(Cost((Wood, 3)), new Points(5)),
            Stage(Cost((Stone, 4)), new Points(7))),
        Board(WonderId.BabylonA, "Babylon A", Clay,
            Stage(Cost((Clay, 2)), new Points(3)),
            Stage(Cost((Wood, 3)), new ScienceWildcard()),
            Stage(Cost((Clay, 4)), new Points(7))),
        Board(WonderId.OlympiaA, "Olympia A", Wood,
            Stage(Cost((Wood, 2)), new Points(3)),
            Stage(Cost((Stone, 2)), new OlympiaFreeBuildEachEra()),
            Stage(Cost((Ore, 2)), new Points(7))),
        Board(WonderId.RhodosA, "Rhodos A", Ore,
            Stage(Cost((Wood, 2)), new Points(3)),
            Stage(Cost((Clay, 3)), new Gain(Shields, 2)),
            Stage(Cost((Ore, 4)), new Points(7))),
        Board(WonderId.EphesosA, "Ephesos A", Papyrus,
            Stage(Cost((Stone, 2)), new Points(3)),
            Stage(Cost((Wood, 2)), new Coins(9)),
            Stage(Cost((Papyrus, 2)), new Points(7))),
        Board(WonderId.AlexandriaA, "Alexandria A", Glass,
            Stage(Cost((Stone, 2)), new Points(3)),
            Stage(Cost((Ore, 2)), new Grant(Ability.AnyRawMaterialEachTurn)),
            Stage(Cost((Glass, 2)), new Points(7))),
        Board(WonderId.HalikarnassosA, "Halikarnassos A", Loom,
            Stage(Cost((Clay, 2)), new Points(3)),
            Stage(Cost((Ore, 3)), new BuildFromDiscardPile()),
            Stage(Cost((Loom, 2)), new Points(7))),

        // ---------- side B ----------
        Board(WonderId.GizahB, "Gizah B", Stone,
            Stage(Cost((Wood, 2)), new Points(3)),
            Stage(Cost((Stone, 3)), new Points(5)),
            Stage(Cost((Clay, 3)), new Points(5)),
            Stage(Cost((Stone, 4), (Papyrus, 1)), new Points(7))),
        Board(WonderId.BabylonB, "Babylon B", Clay,
            Stage(Cost((Clay, 1), (Loom, 1)), new Points(3)),
            Stage(Cost((Wood, 2), (Glass, 1)), new Grant(Ability.PlaySeventhCard)),
            Stage(Cost((Clay, 3), (Papyrus, 1)), new ScienceWildcard())),
        Board(WonderId.OlympiaB, "Olympia B", Wood,
            Stage(Cost((Wood, 2)), new Grant(Ability.RawMaterialsCheapFromBothNeighbours)),
            Stage(Cost((Stone, 2)), new Points(5)),
            Stage(Cost((Ore, 2), (Loom, 1)), new Grant(Ability.CopyGuild))),
        Board(WonderId.RhodosB, "Rhodos B", Ore,
            Stage(Cost((Stone, 3)), new Points(3), new Gain(Shields), new Coins(3)),
            Stage(Cost((Ore, 4)), new Points(4), new Gain(Shields), new Coins(4))),
        Board(WonderId.EphesosB, "Ephesos B", Papyrus,
            Stage(Cost((Stone, 2)), new Points(2), new Coins(4)),
            Stage(Cost((Wood, 2)), new Points(3), new Coins(4)),
            Stage(Cost((Glass, 1), (Loom, 1), (Papyrus, 1)), new Points(5), new Coins(4))),
        Board(WonderId.AlexandriaB, "Alexandria B", Glass,
            Stage(Cost((Clay, 2)), new Grant(Ability.AnyRawMaterialEachTurn)),
            Stage(Cost((Wood, 2)), new Grant(Ability.AnyManufacturedGoodEachTurn)),
            Stage(Cost((Stone, 3)), new Points(7))),
        Board(WonderId.HalikarnassosB, "Halikarnassos B", Loom,
            Stage(Cost((Ore, 2)), new Points(2), new BuildFromDiscardPile()),
            Stage(Cost((Clay, 3)), new Points(1), new BuildFromDiscardPile()),
            Stage(Cost((Glass, 1), (Loom, 1), (Papyrus, 1)), new BuildFromDiscardPile())),
    }.ToDictionary(board => board.Id);
}
