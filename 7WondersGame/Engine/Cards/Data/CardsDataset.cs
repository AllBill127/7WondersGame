using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Effects;
using _7WondersGame.Engine.Effects.Enums;

namespace _7WondersGame.Engine.Cards.Data;

/// <summary>
/// The card table: every card in the game, built once and never changed. Copy counts depend on the
/// player count, so they are looked up with <see cref="Card.CopiesFor"/> when the decks are built.
/// Card objects are shared by every game: compare and look them up, never change them.
/// </summary>
public static class CardsDataset
{
    // Each card: id, name, colour, era, the card it chains from; its cost in the order Wood, Ore, Clay, Stone, Loom,
    // Glass, Papyrus, Coins; its copies for 3, 4, 5, 6 and 7 players; then its effects.

    /// <summary>Every card of eras 1-3 except the guilds, by id.</summary>
    public static readonly IReadOnlyDictionary<CardId, Card> Cards = ById(new Card[]
    {
        // Raw Material
        new Card(CardId.LumberYard, "Lumber Yard", CardType.Materials, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Gain(Resource.Wood)),
        new Card(CardId.StonePit, "Stone Pit", CardType.Materials, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Stone)),
        new Card(CardId.ClayPool, "Clay Pool", CardType.Materials, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Clay)),
        new Card(CardId.OreVein, "Ore Vein", CardType.Materials, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Gain(Resource.Ore)),
        new Card(CardId.TreeFarm, "Tree Farm", CardType.Materials, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 1 }, copies: new int[] { 0, 0, 0, 1, 1 },
            new OneOf(true, Resource.Wood, Resource.Clay)),
        new Card(CardId.Excavation, "Excavation", CardType.Materials, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 1 }, copies: new int[] { 0, 1, 1, 1, 1 },
            new OneOf(true, Resource.Clay, Resource.Stone)),
        new Card(CardId.ClayPit, "Clay Pit", CardType.Materials, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 1 }, copies: new int[] { 1, 1, 1, 1, 1 },
            new OneOf(true, Resource.Clay, Resource.Ore)),
        new Card(CardId.TimberYard, "Timber Yard", CardType.Materials, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 1 }, copies: new int[] { 1, 1, 1, 1, 1 },
            new OneOf(true, Resource.Wood, Resource.Stone)),
        new Card(CardId.ForestCave, "Forest Cave", CardType.Materials, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 1 }, copies: new int[] { 0, 0, 1, 1, 1 },
            new OneOf(true, Resource.Wood, Resource.Ore)),
        new Card(CardId.Mine, "Mine", CardType.Materials, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 1 }, copies: new int[] { 0, 0, 0, 1, 1 },
            new OneOf(true, Resource.Ore, Resource.Stone)),
        new Card(CardId.Sawmill, "Sawmill", CardType.Materials, era: 2, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 1 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Gain(Resource.Wood, 2)),
        new Card(CardId.Quarry, "Quarry", CardType.Materials, era: 2, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 1 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Gain(Resource.Stone, 2)),
        new Card(CardId.Brickyard, "Brickyard", CardType.Materials, era: 2, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 1 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Gain(Resource.Clay, 2)),
        new Card(CardId.Foundry, "Foundry", CardType.Materials, era: 2, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 1 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Gain(Resource.Ore, 2)),
        // Manufactured Good
        new Card(CardId.Loom, "Loom", CardType.Manufactured, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new Gain(Resource.Loom)),
        new Card(CardId.Glassworks, "Glassworks", CardType.Manufactured, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new Gain(Resource.Glass)),
        new Card(CardId.Press, "Press", CardType.Manufactured, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new Gain(Resource.Papyrus)),
        new Card(CardId.Loom2, "Loom", CardType.Manufactured, era: 2, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Loom)),
        new Card(CardId.Glassworks2, "Glassworks", CardType.Manufactured, era: 2, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Glass)),
        new Card(CardId.Press2, "Press", CardType.Manufactured, era: 2, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Papyrus)),
        // Civilian Structure
        new Card(CardId.Altar, "Altar", CardType.Civilian, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Points(2)),
        new Card(CardId.Theater, "Theater", CardType.Civilian, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new Points(2)),
        new Card(CardId.Pawnshop, "Pawnshop", CardType.Civilian, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 0, 1, 1, 1, 2 },
            new Points(3)),
        new Card(CardId.Baths, "Baths", CardType.Civilian, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 1, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Points(3)),
        new Card(CardId.Temple, "Temple", CardType.Civilian, era: 2, freeWithId: CardId.Altar,
            cost: new int[] { 1, 0, 1, 0, 0, 1, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new Points(3)),
        new Card(CardId.Courthouse, "Courthouse", CardType.Civilian, era: 2, freeWithId: CardId.Scriptorium,
            cost: new int[] { 0, 0, 2, 0, 1, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Points(4)),
        new Card(CardId.Statue, "Statue", CardType.Civilian, era: 2, freeWithId: CardId.Theater,
            cost: new int[] { 1, 2, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Points(4)),
        new Card(CardId.Aqueduct, "Aqueduct", CardType.Civilian, era: 2, freeWithId: CardId.Baths,
            cost: new int[] { 0, 0, 0, 3, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Points(5)),
        new Card(CardId.Gardens, "Gardens", CardType.Civilian, era: 3, freeWithId: CardId.Statue,
            cost: new int[] { 1, 0, 2, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Points(5)),
        new Card(CardId.TownHall, "Town Hall", CardType.Civilian, era: 3, freeWithId: CardId.None,
            cost: new int[] { 0, 1, 0, 2, 0, 1, 0, 0 }, copies: new int[] { 1, 1, 2, 3, 3 },
            new Points(6)),
        new Card(CardId.Senate, "Senate", CardType.Civilian, era: 3, freeWithId: CardId.Library,
            cost: new int[] { 2, 1, 0, 1, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Points(6)),
        new Card(CardId.Pantheon, "Pantheon", CardType.Civilian, era: 3, freeWithId: CardId.Temple,
            cost: new int[] { 0, 1, 2, 0, 1, 1, 1, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new Points(7)),
        new Card(CardId.Palace, "Palace", CardType.Civilian, era: 3, freeWithId: CardId.None,
            cost: new int[] { 1, 1, 1, 1, 1, 1, 1, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Points(8)),
        // Commercial Structure
        new Card(CardId.Tavern, "Tavern", CardType.Commercial, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 0, 1, 2, 2, 3 },
            new Coins(5)),
        new Card(CardId.EastTradingPost, "East Trading Post", CardType.Commercial, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Grant(Ability.RawCheapEast)),
        new Card(CardId.WestTradingPost, "West Trading Post", CardType.Commercial, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Grant(Ability.RawCheapWest)),
        new Card(CardId.Marketplace, "Marketplace", CardType.Commercial, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new Grant(Ability.ManufCheap)),
        // Forum is free with either trading post: its FreeWithId is AnyTradingPost (see Rules.CanChainBuildCard).
        new Card(CardId.Forum, "Forum", CardType.Commercial, era: 2, freeWithId: CardId.AnyTradingPost,
            cost: new int[] { 0, 0, 2, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 3 },
            new OneOf(false, Resource.Loom, Resource.Glass, Resource.Papyrus)),
        new Card(CardId.Caravansery, "Caravansery", CardType.Commercial, era: 2, freeWithId: CardId.Marketplace,
            cost: new int[] { 2, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 3, 3 },
            new OneOf(false, Resource.Wood, Resource.Ore, Resource.Clay, Resource.Stone)),
        new Card(CardId.Vineyard, "Vineyard", CardType.Commercial, era: 2, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new CoinsPerCard(CardType.Materials, 1, Who.All)),
        new Card(CardId.Bazar, "Bazar", CardType.Commercial, era: 2, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 0, 1, 1, 1, 2 },
            new CoinsPerCard(CardType.Manufactured, 2, Who.All)),
        new Card(CardId.Haven, "Haven", CardType.Commercial, era: 3, freeWithId: CardId.Forum,
            cost: new int[] { 1, 1, 0, 0, 1, 0, 0, 0 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new CoinsPerCard(CardType.Materials, 1, Who.Self), new PointsPerCard(1, Who.Self, CardType.Materials)),
        new Card(CardId.Lighthouse, "Lighthouse", CardType.Commercial, era: 3, freeWithId: CardId.Caravansery,
            cost: new int[] { 0, 0, 0, 1, 0, 1, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new CoinsPerCard(CardType.Commercial, 1, Who.Self), new PointsPerCard(1, Who.Self, CardType.Commercial)),
        new Card(CardId.ChamberOfCommerce, "Chamber of Commerce", CardType.Commercial, era: 3, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 2, 0, 0, 0, 1, 0 }, copies: new int[] { 0, 1, 1, 2, 2 },
            new CoinsPerCard(CardType.Manufactured, 2, Who.Self),
            new PointsPerCard(2, Who.Self, CardType.Manufactured)),
        new Card(CardId.Arena, "Arena", CardType.Commercial, era: 3, freeWithId: CardId.Dispensary,
            cost: new int[] { 0, 1, 0, 2, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 3 },
            new CoinsPerWonderStage(3), new PointsPerWonderStage(1, Who.Self)),
        // Military Structure
        new Card(CardId.Stockade, "Stockade", CardType.Military, era: 1, freeWithId: CardId.None,
            cost: new int[] { 1, 0, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Gain(Resource.Shields)),
        new Card(CardId.Barracks, "Barracks", CardType.Military, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 1, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Shields)),
        new Card(CardId.GuardTower, "Guard Tower", CardType.Military, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 1, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Gain(Resource.Shields)),
        new Card(CardId.Walls, "Walls", CardType.Military, era: 2, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 3, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Gain(Resource.Shields, 2)),
        new Card(CardId.TrainingGround, "Training Ground", CardType.Military, era: 2, freeWithId: CardId.None,
            cost: new int[] { 1, 2, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 0, 1, 1, 2, 3 },
            new Gain(Resource.Shields, 2)),
        new Card(CardId.Stables, "Stables", CardType.Military, era: 2, freeWithId: CardId.Apothecary,
            cost: new int[] { 1, 1, 1, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Shields, 2)),
        new Card(CardId.ArcheryRange, "Archery Range", CardType.Military, era: 2, freeWithId: CardId.Workshop,
            cost: new int[] { 2, 1, 0, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new Gain(Resource.Shields, 2)),
        new Card(CardId.Fortifications, "Fortifications", CardType.Military, era: 3, freeWithId: CardId.Walls,
            cost: new int[] { 0, 3, 0, 1, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Gain(Resource.Shields, 3)),
        new Card(CardId.Circus, "Circus", CardType.Military, era: 3, freeWithId: CardId.TrainingGround,
            cost: new int[] { 0, 1, 0, 3, 0, 0, 0, 0 }, copies: new int[] { 0, 1, 2, 3, 3 },
            new Gain(Resource.Shields, 3)),
        new Card(CardId.Arsenal, "Arsenal", CardType.Military, era: 3, freeWithId: CardId.None,
            cost: new int[] { 2, 1, 0, 0, 1, 0, 0, 0 }, copies: new int[] { 1, 2, 2, 2, 3 },
            new Gain(Resource.Shields, 3)),
        new Card(CardId.SiegeWorkshop, "Siege Workshop", CardType.Military, era: 3, freeWithId: CardId.Laboratory,
            cost: new int[] { 1, 0, 3, 0, 0, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Shields, 3)),
        // Scientific Structure
        new Card(CardId.Apothecary, "Apothecary", CardType.Scientific, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 1, 0, 0, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Compass)),
        new Card(CardId.Workshop, "Workshop", CardType.Scientific, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 1, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Gain(Resource.Gear)),
        new Card(CardId.Scriptorium, "Scriptorium", CardType.Scientific, era: 1, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 0, 0, 1, 0 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Gain(Resource.Tablet)),
        new Card(CardId.Dispensary, "Dispensary", CardType.Scientific, era: 2, freeWithId: CardId.Apothecary,
            cost: new int[] { 0, 2, 0, 0, 0, 1, 0, 0 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Gain(Resource.Compass)),
        new Card(CardId.Laboratory, "Laboratory", CardType.Scientific, era: 2, freeWithId: CardId.Workshop,
            cost: new int[] { 0, 0, 2, 0, 0, 0, 1, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Gear)),
        new Card(CardId.Library, "Library", CardType.Scientific, era: 2, freeWithId: CardId.Scriptorium,
            cost: new int[] { 0, 0, 0, 2, 1, 0, 0, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new Gain(Resource.Tablet)),
        new Card(CardId.School, "School", CardType.Scientific, era: 2, freeWithId: CardId.None,
            cost: new int[] { 1, 0, 0, 0, 0, 0, 1, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Gain(Resource.Tablet)),
        new Card(CardId.Lodge, "Lodge", CardType.Scientific, era: 3, freeWithId: CardId.Dispensary,
            cost: new int[] { 0, 0, 2, 0, 1, 0, 1, 0 }, copies: new int[] { 1, 1, 1, 2, 2 },
            new Gain(Resource.Compass)),
        new Card(CardId.Observatory, "Observatory", CardType.Scientific, era: 3, freeWithId: CardId.Laboratory,
            cost: new int[] { 0, 2, 0, 0, 1, 1, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Gain(Resource.Gear)),
        new Card(CardId.University, "University", CardType.Scientific, era: 3, freeWithId: CardId.Library,
            cost: new int[] { 2, 0, 0, 0, 0, 1, 1, 0 }, copies: new int[] { 1, 2, 2, 2, 2 },
            new Gain(Resource.Tablet)),
        new Card(CardId.Academy, "Academy", CardType.Scientific, era: 3, freeWithId: CardId.School,
            cost: new int[] { 0, 0, 0, 3, 0, 1, 0, 0 }, copies: new int[] { 1, 1, 1, 1, 2 },
            new Gain(Resource.Compass)),
        new Card(CardId.Study, "Study", CardType.Scientific, era: 3, freeWithId: CardId.School,
            cost: new int[] { 1, 0, 0, 0, 1, 0, 1, 0 }, copies: new int[] { 1, 1, 2, 2, 2 },
            new Gain(Resource.Gear)),
    });

    /// <summary>The guilds (era 3, purple), by id.</summary>
    public static readonly IReadOnlyDictionary<CardId, Card> GuildCards = ById(new Card[]
    {
        new Card(CardId.Workers, "Workers Guild", CardType.Guild, era: 3, freeWithId: CardId.None,
            cost: new int[] { 1, 2, 1, 1, 0, 0, 0, 0 }, copies: new int[] { 0, 0, 0, 0, 0 },
            new PointsPerCard(1, Who.Neighbours, CardType.Materials)),
        new Card(CardId.Craftsmens, "Craftsmens Guild", CardType.Guild, era: 3, freeWithId: CardId.None,
            cost: new int[] { 0, 2, 0, 2, 0, 0, 0, 0 }, copies: new int[] { 0, 0, 0, 0, 0 },
            new PointsPerCard(2, Who.Neighbours, CardType.Manufactured)),
        new Card(CardId.Traders, "Traders Guild", CardType.Guild, era: 3, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 0, 0, 1, 1, 1, 0 }, copies: new int[] { 0, 0, 0, 0, 0 },
            new PointsPerCard(1, Who.Neighbours, CardType.Commercial)),
        new Card(CardId.Philosophers, "Philosophers Guild", CardType.Guild, era: 3, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 3, 0, 1, 0, 1, 0 }, copies: new int[] { 0, 0, 0, 0, 0 },
            new PointsPerCard(1, Who.Neighbours, CardType.Scientific)),
        new Card(CardId.Spies, "Spies Guild", CardType.Guild, era: 3, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 3, 0, 0, 1, 0, 0 }, copies: new int[] { 0, 0, 0, 0, 0 },
            new PointsPerCard(1, Who.Neighbours, CardType.Military)),
        new Card(CardId.Magistrates, "Magistrates Guild", CardType.Guild, era: 3, freeWithId: CardId.None,
            cost: new int[] { 3, 0, 0, 1, 1, 0, 0, 0 }, copies: new int[] { 0, 0, 0, 0, 0 },
            new PointsPerCard(1, Who.Neighbours, CardType.Civilian)),
        new Card(CardId.Shipowners, "Shipowners Guild", CardType.Guild, era: 3, freeWithId: CardId.None,
            cost: new int[] { 3, 0, 0, 0, 0, 1, 1, 0 }, copies: new int[] { 0, 0, 0, 0, 0 },
            new PointsPerCard(1, Who.Self, CardType.Materials, CardType.Manufactured, CardType.Guild)),
        new Card(CardId.Strategists, "Strategists Guild", CardType.Guild, era: 3, freeWithId: CardId.None,
            cost: new int[] { 0, 2, 0, 1, 1, 0, 0, 0 }, copies: new int[] { 0, 0, 0, 0, 0 },
            new PointsPerNeighbourDefeat(1)),
        new Card(CardId.Scientists, "Scientists Guild", CardType.Guild, era: 3, freeWithId: CardId.None,
            cost: new int[] { 2, 2, 0, 0, 0, 0, 1, 0 }, copies: new int[] { 0, 0, 0, 0, 0 },
            new ScienceWildcard()),
        new Card(CardId.Builders, "Builders Guild", CardType.Guild, era: 3, freeWithId: CardId.None,
            cost: new int[] { 0, 0, 2, 2, 0, 1, 0, 0 }, copies: new int[] { 0, 0, 0, 0, 0 },
            new PointsPerWonderStage(1, Who.All)),
    });

    /// <summary>Every card, guilds included, by id.</summary>
    public static readonly IReadOnlyDictionary<CardId, Card> GameCards = Combine(Cards, GuildCards);

    /// <summary>The cards keyed by their id, in the order given.</summary>
    private static Dictionary<CardId, Card> ById(Card[] cards)
    {
        Dictionary<CardId, Card> table = new Dictionary<CardId, Card>();
        foreach (Card card in cards)
        {
            table.Add(card.Id, card);
        }
        return table;
    }

    /// <summary>The cards of both tables in one, the first table's first.</summary>
    private static Dictionary<CardId, Card> Combine(IReadOnlyDictionary<CardId, Card> first,
                                                    IReadOnlyDictionary<CardId, Card> second)
    {
        Dictionary<CardId, Card> combined = new Dictionary<CardId, Card>();
        foreach (Card card in first.Values)
        {
            combined.Add(card.Id, card);
        }
        foreach (Card card in second.Values)
        {
            combined.Add(card.Id, card);
        }
        return combined;
    }
}
