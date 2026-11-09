namespace _7WondersGame.src.models
{
    /// <summary>
    /// The card table (E7): every card in the game, built once and never changed. Copy counts depend on the
    /// player count, so they are looked up with <see cref="Card.CopiesFor"/> when the decks are built.
    /// Card objects are shared by every game: compare and look them up, never change them.
    /// </summary>
    public static class CardsDataset
    {
        public static readonly IReadOnlyDictionary<CardId, Card> Cards = new Dictionary<CardId, Card>()
        {
            // Raw Material
            { CardId.LumberYard,           new Card(CardId.LumberYard,            "Lumber Yard",           CardType.Materials,    1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.StonePit,             new Card(CardId.StonePit,              "Stone Pit",             CardType.Materials,    1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.ClayPool,             new Card(CardId.ClayPool,              "Clay Pool",             CardType.Materials,    1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.OreVein,              new Card(CardId.OreVein,               "Ore Vein",              CardType.Materials,    1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.TreeFarm,             new Card(CardId.TreeFarm,              "Tree Farm",             CardType.Materials,    1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 1 }, new int[]{ 0, 0, 0, 1, 1 }) },
            { CardId.Excavation,           new Card(CardId.Excavation,            "Excavation",            CardType.Materials,    1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 1 }, new int[]{ 0, 1, 1, 1, 1 }) },
            { CardId.ClayPit,              new Card(CardId.ClayPit,               "Clay Pit",              CardType.Materials,    1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 1 }, new int[]{ 1, 1, 1, 1, 1 }) },
            { CardId.TimberYard,           new Card(CardId.TimberYard,            "Timber Yard",           CardType.Materials,    1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 1 }, new int[]{ 1, 1, 1, 1, 1 }) },
            { CardId.ForestCave,           new Card(CardId.ForestCave,            "Forest Cave",           CardType.Materials,    1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 1 }, new int[]{ 0, 0, 1, 1, 1 }) },
            { CardId.Mine,                 new Card(CardId.Mine,                  "Mine",                  CardType.Materials,    1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 1 }, new int[]{ 0, 0, 0, 1, 1 }) },
            { CardId.Sawmill,              new Card(CardId.Sawmill,               "Sawmill",               CardType.Materials,    2,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 1 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.Quarry,               new Card(CardId.Quarry,                "Quarry",                CardType.Materials,    2,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 1 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.Brickyard,            new Card(CardId.Brickyard,             "Brickyard",             CardType.Materials,    2,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 1 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.Foundry,              new Card(CardId.Foundry,               "Foundry",               CardType.Materials,    2,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 1 }, new int[]{ 1, 2, 2, 2, 2 }) },
            // Manufactured Good
            { CardId.Loom,                 new Card(CardId.Loom,                  "Loom",                  CardType.Manufactured, 1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.Glassworks,           new Card(CardId.Glassworks,            "Glassworks",            CardType.Manufactured, 1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.Press,                new Card(CardId.Press,                 "Press",                 CardType.Manufactured, 1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.Loom2,                new Card(CardId.Loom2,                 "Loom",                  CardType.Manufactured, 2,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.Glassworks2,          new Card(CardId.Glassworks2,           "Glassworks",            CardType.Manufactured, 2,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.Press2,               new Card(CardId.Press2,                "Press",                 CardType.Manufactured, 2,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            // Civilian Structure
            { CardId.Altar,                new Card(CardId.Altar,                 "Altar",                 CardType.Civilian,     1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.Theater,              new Card(CardId.Theater,               "Theater",               CardType.Civilian,     1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.Pawnshop,             new Card(CardId.Pawnshop,              "Pawnshop",              CardType.Civilian,     1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 0, 1, 1, 1, 2 }) },
            { CardId.Baths,                new Card(CardId.Baths,                 "Baths",                 CardType.Civilian,     1,   CardId.None,            new int[]{ 0, 0, 0, 1, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.Temple,               new Card(CardId.Temple,                "Temple",                CardType.Civilian,     2,   CardId.Altar,           new int[]{ 1, 0, 1, 0, 0, 1, 0, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.Courthouse,           new Card(CardId.Courthouse,            "Courthouse",            CardType.Civilian,     2,   CardId.Scriptorium,     new int[]{ 0, 0, 2, 0, 1, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.Statue,               new Card(CardId.Statue,                "Statue",                CardType.Civilian,     2,   CardId.Theater,         new int[]{ 1, 2, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.Aqueduct,             new Card(CardId.Aqueduct,              "Aqueduct",              CardType.Civilian,     2,   CardId.Baths,           new int[]{ 0, 0, 0, 3, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.Gardens,              new Card(CardId.Gardens,               "Gardens",               CardType.Civilian,     3,   CardId.Statue,          new int[]{ 1, 0, 2, 0, 0, 0, 0, 0 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.TownHall,             new Card(CardId.TownHall,              "Town Hall",             CardType.Civilian,     3,   CardId.None,            new int[]{ 0, 1, 0, 2, 0, 1, 0, 0 }, new int[]{ 1, 1, 2, 3, 3 }) },
            { CardId.Senate,               new Card(CardId.Senate,                "Senate",                CardType.Civilian,     3,   CardId.Library,         new int[]{ 2, 1, 0, 1, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.Pantheon,             new Card(CardId.Pantheon,              "Pantheon",              CardType.Civilian,     3,   CardId.Temple,          new int[]{ 0, 1, 2, 0, 1, 1, 1, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.Palace,               new Card(CardId.Palace,                "Palace",                CardType.Civilian,     3,   CardId.None,            new int[]{ 1, 1, 1, 1, 1, 1, 1, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            // Commercial Structure
            { CardId.Tavern,               new Card(CardId.Tavern,                "Tavern",                CardType.Commercial,   1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 0, 1, 2, 2, 3 }) },
            { CardId.EastTradingPost,      new Card(CardId.EastTradingPost,       "East Trading Post",     CardType.Commercial,   1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.WestTradingPost,      new Card(CardId.WestTradingPost,       "West Trading Post",     CardType.Commercial,   1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.Marketplace,          new Card(CardId.Marketplace,           "Marketplace",           CardType.Commercial,   1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            // obs.: Forum can be constructed for free if the player has either the East Trading Post or the West Trading Post.
            // Therefore, freeWithId will be -1 and will be treated on player.cpp, therefore avoiding the need for vectors.
            // It's not the prettiest, but it's a simple and efficient solution, as Forum is the only card that carries this exception.
            { CardId.Forum,                new Card(CardId.Forum,                 "Forum",                 CardType.Commercial,   2,   CardId.AnyTradingPost,  new int[]{ 0, 0, 2, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 2, 3 }) },
            { CardId.Caravansery,          new Card(CardId.Caravansery,           "Caravansery",           CardType.Commercial,   2,   CardId.Marketplace,     new int[]{ 2, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 3, 3 }) },
            { CardId.Vineyard,             new Card(CardId.Vineyard,              "Vineyard",              CardType.Commercial,   2,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.Bazar,                new Card(CardId.Bazar,                 "Bazar",                 CardType.Commercial,   2,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 0, 1, 1, 1, 2 }) },
            { CardId.Haven,                new Card(CardId.Haven,                 "Haven",                 CardType.Commercial,   3,   CardId.Forum,           new int[]{ 1, 1, 0, 0, 1, 0, 0, 0 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.Lighthouse,           new Card(CardId.Lighthouse,            "Lighthouse",            CardType.Commercial,   3,   CardId.Caravansery,     new int[]{ 0, 0, 0, 1, 0, 1, 0, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.ChamberOfCommerce,    new Card(CardId.ChamberOfCommerce,     "Chamber of Commerce",   CardType.Commercial,   3,   CardId.None,            new int[]{ 0, 0, 2, 0, 0, 0, 1, 0 }, new int[]{ 0, 1, 1, 2, 2 }) },
            { CardId.Arena,                new Card(CardId.Arena,                 "Arena",                 CardType.Commercial,   3,   CardId.Dispensary,      new int[]{ 0, 1, 0, 2, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 3 }) },
            // Military Structure
            { CardId.Stockade,             new Card(CardId.Stockade,              "Stockade",              CardType.Military,     1,   CardId.None,            new int[]{ 1, 0, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.Barracks,             new Card(CardId.Barracks,              "Barracks",              CardType.Military,     1,   CardId.None,            new int[]{ 0, 1, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.GuardTower,           new Card(CardId.GuardTower,            "Guard Tower",           CardType.Military,     1,   CardId.None,            new int[]{ 0, 0, 1, 0, 0, 0, 0, 0 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.Walls,                new Card(CardId.Walls,                 "Walls",                 CardType.Military,     2,   CardId.None,            new int[]{ 0, 0, 0, 3, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.TrainingGround,       new Card(CardId.TrainingGround,        "Training Ground",       CardType.Military,     2,   CardId.None,            new int[]{ 1, 2, 0, 0, 0, 0, 0, 0 }, new int[]{ 0, 1, 1, 2, 3 }) },
            { CardId.Stables,              new Card(CardId.Stables,               "Stables",               CardType.Military,     2,   CardId.Apothecary,      new int[]{ 1, 1, 1, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.ArcheryRange,         new Card(CardId.ArcheryRange,          "Archery Range",         CardType.Military,     2,   CardId.Workshop,        new int[]{ 2, 1, 0, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.Fortifications,       new Card(CardId.Fortifications,        "Fortifications",        CardType.Military,     3,   CardId.Walls,           new int[]{ 0, 3, 0, 1, 0, 0, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.Circus,               new Card(CardId.Circus,                "Circus",                CardType.Military,     3,   CardId.TrainingGround,  new int[]{ 0, 1, 0, 3, 0, 0, 0, 0 }, new int[]{ 0, 1, 2, 3, 3 }) },
            { CardId.Arsenal,              new Card(CardId.Arsenal,               "Arsenal",               CardType.Military,     3,   CardId.None,            new int[]{ 2, 1, 0, 0, 1, 0, 0, 0 }, new int[]{ 1, 2, 2, 2, 3 }) },
            { CardId.SiegeWorkshop,        new Card(CardId.SiegeWorkshop,         "Siege Workshop",        CardType.Military,     3,   CardId.Laboratory,      new int[]{ 1, 0, 3, 0, 0, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            // Scientific Structure
            { CardId.Apothecary,           new Card(CardId.Apothecary,            "Apothecary",            CardType.Scientific,   1,   CardId.None,            new int[]{ 0, 0, 0, 0, 1, 0, 0, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.Workshop,             new Card(CardId.Workshop,              "Workshop",              CardType.Scientific,   1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 1, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.Scriptorium,          new Card(CardId.Scriptorium,           "Scriptorium",           CardType.Scientific,   1,   CardId.None,            new int[]{ 0, 0, 0, 0, 0, 0, 1, 0 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.Dispensary,           new Card(CardId.Dispensary,            "Dispensary",            CardType.Scientific,   2,   CardId.Apothecary,      new int[]{ 0, 2, 0, 0, 0, 1, 0, 0 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.Laboratory,           new Card(CardId.Laboratory,            "Laboratory",            CardType.Scientific,   2,   CardId.Workshop,        new int[]{ 0, 0, 2, 0, 0, 0, 1, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
            { CardId.Library,              new Card(CardId.Library,               "Library",               CardType.Scientific,   2,   CardId.Scriptorium,     new int[]{ 0, 0, 0, 2, 1, 0, 0, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.School,               new Card(CardId.School,                "School",                CardType.Scientific,   2,   CardId.None,            new int[]{ 1, 0, 0, 0, 0, 0, 1, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.Lodge,                new Card(CardId.Lodge,                 "Lodge",                 CardType.Scientific,   3,   CardId.Dispensary,      new int[]{ 0, 0, 2, 0, 1, 0, 1, 0 }, new int[]{ 1, 1, 1, 2, 2 }) },
            { CardId.Observatory,          new Card(CardId.Observatory,           "Observatory",           CardType.Scientific,   3,   CardId.Laboratory,      new int[]{ 0, 2, 0, 0, 1, 1, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.University,           new Card(CardId.University,            "University",            CardType.Scientific,   3,   CardId.Library,         new int[]{ 2, 0, 0, 0, 0, 1, 1, 0 }, new int[]{ 1, 2, 2, 2, 2 }) },
            { CardId.Academy,              new Card(CardId.Academy,               "Academy",               CardType.Scientific,   3,   CardId.School,          new int[]{ 0, 0, 0, 3, 0, 1, 0, 0 }, new int[]{ 1, 1, 1, 1, 2 }) },
            { CardId.Study,                new Card(CardId.Study,                 "Study",                 CardType.Scientific,   3,   CardId.School,          new int[]{ 1, 0, 0, 0, 1, 0, 1, 0 }, new int[]{ 1, 1, 2, 2, 2 }) },
        };

        // Guild
        public static readonly IReadOnlyDictionary<CardId, Card> GuildCards = new Dictionary<CardId, Card>()
        {
            { CardId.Workers,              new Card(CardId.Workers,               "Workers Guild",         CardType.Guild,        3,   CardId.None,            new int[]{ 1, 2, 1, 1, 0, 0, 0, 0 }, new int[]{ 0, 0, 0, 0, 0 }) },
            { CardId.Craftsmens,           new Card(CardId.Craftsmens,            "Craftsmens Guild",      CardType.Guild,        3,   CardId.None,            new int[]{ 0, 2, 0, 2, 0, 0, 0, 0 }, new int[]{ 0, 0, 0, 0, 0 }) },
            { CardId.Traders,              new Card(CardId.Traders,               "Traders Guild",         CardType.Guild,        3,   CardId.None,            new int[]{ 0, 0, 0, 0, 1, 1, 1, 0 }, new int[]{ 0, 0, 0, 0, 0 }) },
            { CardId.Philosophers,         new Card(CardId.Philosophers,          "Philosophers Guild",    CardType.Guild,        3,   CardId.None,            new int[]{ 0, 0, 3, 0, 1, 0, 1, 0 }, new int[]{ 0, 0, 0, 0, 0 }) },
            { CardId.Spies,                new Card(CardId.Spies,                 "Spies Guild",           CardType.Guild,        3,   CardId.None,            new int[]{ 0, 0, 3, 0, 0, 1, 0, 0 }, new int[]{ 0, 0, 0, 0, 0 }) },
            { CardId.Magistrates,          new Card(CardId.Magistrates,           "Magistrates Guild",     CardType.Guild,        3,   CardId.None,            new int[]{ 3, 0, 0, 1, 1, 0, 0, 0 }, new int[]{ 0, 0, 0, 0, 0 }) },
            { CardId.Shipowners,           new Card(CardId.Shipowners,            "Shipowners Guild",      CardType.Guild,        3,   CardId.None,            new int[]{ 3, 0, 0, 0, 0, 1, 1, 0 }, new int[]{ 0, 0, 0, 0, 0 }) },
            { CardId.Strategists,          new Card(CardId.Strategists,           "Strategists Guild",     CardType.Guild,        3,   CardId.None,            new int[]{ 0, 2, 0, 1, 1, 0, 0, 0 }, new int[]{ 0, 0, 0, 0, 0 }) },
            { CardId.Scientists,           new Card(CardId.Scientists,            "Scientists Guild",      CardType.Guild,        3,   CardId.None,            new int[]{ 2, 2, 0, 0, 0, 0, 1, 0 }, new int[]{ 0, 0, 0, 0, 0 }) },
            { CardId.Builders,             new Card(CardId.Builders,              "Builders Guild",        CardType.Guild,        3,   CardId.None,            new int[]{ 0, 0, 2, 2, 0, 1, 0, 0 }, new int[]{ 0, 0, 0, 0, 0 }) },
        };

        public static readonly IReadOnlyDictionary<CardId, Card> GameCards = Cards.Concat(GuildCards)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
