using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGame.src.models
{
    public class Card
    {
        public CardId Id { get; set; }
        public string Name { get; set; }
        public CardType Type { get; set; }
        public int Era { get; set; }
        public CardId FreeWithId { get; set; }
        public Dictionary<Resource, int> Cost { get; set; }
        public int NumCardsInGame { get; set; }

        public Card(CardId id, string name, CardType type, int era, CardId freeWithId, int[] cost, int[] numCardsInGame, int numPlayers)
        {
            Id = id;
            Name = name;
            Type = type;
            Era = era;
            FreeWithId = freeWithId;
            NumCardsInGame = numCardsInGame[numPlayers - 3];
            Cost = new()
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
            return HashCode.Combine(Id, Name, Type, Era, FreeWithId, Cost, NumCardsInGame);
        }

        public override string ToString()
        {
            StringBuilder cardStr = new StringBuilder();
            cardStr.AppendLine($"Id: {Id}   Name: {Name}    Era: {Era}  CardsInGame: {NumCardsInGame}");
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
