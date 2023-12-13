using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class OlympiaB : Wonder
    {
        public OlympiaB() : base(WonderId.OlympiaB, 
            "Olympia B", 
            Resource.Wood, 
            new Dictionary<Resource, int> { { Resource.Wood, 2 } },
            new Dictionary<Resource, int>
            {
                { Resource.Wood, 2 },
                { Resource.Stone, 2 },
                { Resource.Ore, 2 },
                { Resource.Loom, 1 },
            })
        {
        }

        public override object DeepCopy()
        {
            OlympiaB copiedWonder = new();
            copiedWonder.Stage = Stage;
            copiedWonder.WonderPoints = WonderPoints;
            copiedWonder.Cost = this.Cost.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return copiedWonder;
        }

        public override bool AddStage(Player player)
        {
            bool stage_built = false;

            switch (Stage)
            {
                // Building stage 1
                case 0:
                    if (player.ProduceResource(Resource.Wood, Cost[Resource.Wood]))
                    {
                        Stage++;
                        player.WonderRawCheap = true;
                        Cost[Resource.Stone] = 2;
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Stone, Cost[Resource.Stone]))
                    {
                        Stage++;
                        WonderPoints += 5;
                        Cost[Resource.Ore] = 2;
                        Cost[Resource.Loom] = 1;
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Ore, Cost[Resource.Ore]) &&
                        player.ProduceResource(Resource.Loom, Cost[Resource.Loom]))
                    {
                        Stage++;
                        stage_built = true;
                    }
                    break;
            }

            return stage_built;
        }

        public override bool CanAddStage(Player player)
        {
            bool stage_built = false;

            switch (Stage)
            {
                // Building stage 1
                case 0:
                    if (player.ProduceResource(Resource.Wood, Cost[Resource.Wood]))
                    {
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Stone, Cost[Resource.Stone]))
                    {
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Ore, Cost[Resource.Ore]) &&
                        player.ProduceResource(Resource.Loom, Cost[Resource.Loom]))
                    {
                        stage_built = true;
                    }
                    break;
            }

            return stage_built;
        }
    }
}
