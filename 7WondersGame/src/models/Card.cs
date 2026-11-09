using System.Text;

namespace _7WondersGame.src.models
{
    public class Card
    {
        public CardId Id { get; }
        public string Name { get; }
        public CardType Type { get; }
        public int Era { get; }
        public CardId FreeWithId { get; }
        public IReadOnlyDictionary<Resource, int> Cost { get; }

        /// <summary>Copies in the deck for 3, 4, 5, 6 and 7 players.</summary>
        private readonly int[] _copies;

        public Card(CardId id, string name, CardType type, int era, CardId freeWithId, int[] cost, int[] copies)
        {
            Id = id;
            Name = name;
            Type = type;
            Era = era;
            FreeWithId = freeWithId;
            _copies = copies.ToArray();
            Cost = new Dictionary<Resource, int>()
            {
                { Resource.Wood,       cost[0] },
                { Resource.Ore,        cost[1] },
                { Resource.Clay,       cost[2] },
                { Resource.Stone,      cost[3] },
                { Resource.Loom,       cost[4] },
                { Resource.Glass,      cost[5] },
                { Resource.Papyrus,    cost[6] },
                { Resource.Coins,      cost[7] }
            };
        }

        /// <summary>How many copies of this card go into the deck with <paramref name="players"/> players (3 to 7).</summary>
        public int CopiesFor(int players)
        {
            if (players < 3 || players > 7)
                throw new ArgumentOutOfRangeException(nameof(players), players, "7 Wonders is played by 3 to 7 players.");
            return _copies[players - 3];
        }

        /// <summary>
        /// As defined in the rule book - cards with the same name
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object? obj)
        {
            if (obj == null || !obj.GetType().Equals(this.GetType()))
                return false;

            Card other = (Card)obj;
            return this.Name.Equals(other.Name);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Type, Era, FreeWithId, Cost);
        }

        public override string ToString()
        {
            StringBuilder cardStr = new StringBuilder();
            cardStr.AppendLine($"Id: {Id}   Name: {Name}    Era: {Era}  Copies (3-7 players): {string.Join(' ', _copies)}");
            cardStr.AppendLine($"Type: {Type}   FreeWith: {FreeWithId}");
            return cardStr.ToString();
        }
    }

    public enum CardId
    {
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
        AnyTradingPost = -1,
    };

    public enum CardType
    {
        Materials,
        Manufactured,
        Civilian,
        Commercial,
        Military,
        Scientific,
        Guild,
    };

    public enum Resource
    {
        Wood,
        Ore,
        Clay,
        Stone,

        Loom,
        Glass,
        Papyrus,

        Gear,
        Compass,
        Tablet,

        Coins,
        Shields,
    };
}
