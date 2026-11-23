namespace _7Wonders.Engine.Cards.Enums;

/// <summary>Every card of the game, one id per card (the era 1 and era 2 Loom are different cards).</summary>
public enum CardId
{
    /// <summary>No card: a card that doesn't chain from anything has this as its <c>FreeIfBuiltCardId</c>.</summary>
    None,

    // Materials
    LumberYard,
    StonePit,
    ClayPool,
    OreVein,
    TreeFarm,
    Excavation,
    ClayPit,
    TimberYard,
    ForestCave,
    Mine,
    Sawmill,
    Quarry,
    Brickyard,
    Foundry,

    //Manufactured
    Loom,
    Glassworks,
    Press,
    Loom2,
    Glassworks2,
    Press2,

    //Civilian
    Altar,
    Theater,
    Pawnshop,
    Baths,
    Temple,
    Courthouse,
    Statue,
    Aqueduct,
    Gardens,
    TownHall,
    Senate,
    Pantheon,
    Palace,

    //Comercial
    Tavern,
    EastTradingPost,
    WestTradingPost,
    Marketplace,
    Forum,
    Caravansery,
    Vineyard,
    Bazar,
    Haven,
    Lighthouse,
    ChamberOfCommerce,
    Arena,

    //Military
    Stockade,
    Barracks,
    GuardTower,
    Walls,
    TrainingGround,
    Stables,
    ArcheryRange,
    Fortifications,
    Circus,
    Arsenal,
    SiegeWorkshop,

    //Scientific
    Apothecary,
    Workshop,
    Scriptorium,
    Dispensary,
    Laboratory,
    Library,
    School,
    Lodge,
    Observatory,
    University,
    Academy,
    Study,

    //Guild
    Workers,
    Craftsmens,
    Traders,
    Philosophers,
    Spies,
    Magistrates,
    Shipowners,
    Strategists,
    Scientists,
    Builders,

    //Extra commercial
    /// <summary>Not a card: Forum's <c>FreeIfBuiltCardId</c>, meaning either trading post.</summary>
    AnyTradingPost = -1,
};
