using _7WondersGame.src.models.Effects;
using static _7WondersGame.src.models.Resource;

namespace _7WondersGame.src.models.Wonders
{
    /// <summary>
    /// Every wonder board (E3): its starting resource and, for each stage in building order, the cost and the effects.
    /// Shared by every game and never changed. Checked against the rulebook in WonderDataTests.
    /// </summary>
    public static class WonderBoards
    {
        private static IReadOnlyDictionary<Resource, int> Cost(params (Resource Resource, int Amount)[] parts) =>
            parts.ToDictionary(p => p.Resource, p => p.Amount);

        private static WonderStage Stage(IReadOnlyDictionary<Resource, int> cost, params IEffect[] effects) => new(cost, effects);

        private static WonderBoard Board(WonderId id, string name, Resource production, params WonderStage[] stages) =>
            new(id, name, production, stages);

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
                Stage(Cost((Stone, 2)), new FreeCardEachEra()),
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
                Stage(Cost((Ore, 2)), new Grant(Ability.AnyRaw)),
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
                Stage(Cost((Wood, 2), (Glass, 1)), new Grant(Ability.PlaySeventh)),
                Stage(Cost((Clay, 3), (Papyrus, 1)), new ScienceWildcard())),
            Board(WonderId.OlympiaB, "Olympia B", Wood,
                Stage(Cost((Wood, 2)), new Grant(Ability.WonderRawCheap)),
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
                Stage(Cost((Clay, 2)), new Grant(Ability.AnyRaw)),
                Stage(Cost((Wood, 2)), new Grant(Ability.AnyManufactured)),
                Stage(Cost((Stone, 3)), new Points(7))),
            Board(WonderId.HalikarnassosB, "Halikarnassos B", Loom,
                Stage(Cost((Ore, 2)), new Points(2), new BuildFromDiscardPile()),
                Stage(Cost((Clay, 3)), new Points(1), new BuildFromDiscardPile()),
                Stage(Cost((Glass, 1), (Loom, 1), (Papyrus, 1)), new BuildFromDiscardPile())),
        }.ToDictionary(board => board.Id);
    }
}
