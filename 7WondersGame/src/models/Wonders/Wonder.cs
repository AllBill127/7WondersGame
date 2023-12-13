using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public abstract class Wonder
    {
        public WonderId Id { get; private set; }
        public string Name { get; private set; }
        public int Stage { get; set; } = 0;
        public int WonderPoints { get; set; } = 0;
        public Resource Production { get; private set; }
        public Dictionary<Resource, int> Cost = new Dictionary<Resource, int>()
        {
            { Resource.Wood, 0 },
            { Resource.Stone, 0 },
            { Resource.Ore, 0 },
            { Resource.Clay, 0 },
            { Resource.Glass, 0 },
            { Resource.Loom, 0 },
            { Resource.Papyrus, 0 },
        };
        public Dictionary<Resource, int> HighestCosts = new Dictionary<Resource, int>()
        {
            { Resource.Wood, 0 },
            { Resource.Stone, 0 },
            { Resource.Ore, 0 },
            { Resource.Clay, 0 },
            { Resource.Glass, 0 },
            { Resource.Loom, 0 },
            { Resource.Papyrus, 0 },
        };

        public Wonder(WonderId id, string name, Resource production, Dictionary<Resource, int> cost, Dictionary<Resource, int> highestCosts)
        {
            Id = id;
            Name = name;
            Production = production;
            foreach (var kvp in cost)
                Cost[kvp.Key] = kvp.Value;
            foreach (var kvp in highestCosts)
                HighestCosts[kvp.Key] = kvp.Value;
        }

        public abstract object DeepCopy();

        /// <summary>
        /// Checks if a player can build the next stage of a wonder. <br/>
        /// - Calling function is responsible to backup and reset player and his neighbors resource and usedOnDemandResource dictionaries
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public abstract bool CanAddStage(Player player);

        /// <summary>
        /// Build the next stage of a wonder. <br/>
        /// - Calling function is responsible to backup and reset player and his neighbors resource and usedOnDemandResource dictionaries if unsuccessfull
        /// </summary>
        /// <param name="player"></param>
        /// <returns>
        /// true if successfull; otherwise false
        /// </returns>
        public abstract bool AddStage(Player player);
    }

    public enum WonderId{
        // 0-6 side A
        GizahA,
        BabylonA,
        OlympiaA,
        RhodosA,
        EphesosA,
        AlexandriaA,
        HalikarnassosA,
        // 7-13 side B
        GizahB,
        BabylonB,
        OlympiaB,
        RhodosB,
        EphesosB,
        AlexandriaB,
        HalikarnassosB,
    };
}
