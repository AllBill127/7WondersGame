using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class HalikarnassosB : Wonder
    {
        public HalikarnassosB() : base(WonderId.HalikarnassosB,
            "Halikarnassos B", 
            Resource.Loom, 
            new Dictionary<Resource, int> { { Resource.Ore, 2 } },
            new Dictionary<Resource, int>
            {
                { Resource.Ore, 2 },
                { Resource.Clay, 3 },
                { Resource.Glass, 1 },
                { Resource.Loom, 1 },
                { Resource.Papyrus, 1 },
            })
        {
        }

        public override object DeepCopy()
        {
            HalikarnassosB copiedWonder = new();
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
                    if (player.ProduceResource(Resource.Ore, Cost[Resource.Ore]))
                    {
                        Stage++;
                        WonderPoints += 2;
                        player.DiscardFree = true;
                        Cost[Resource.Clay] = 3;
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]))
                    {
                        Stage++;
                        WonderPoints += 1;
                        player.DiscardFree = true;
                        Cost[Resource.Glass] = 1;
                        Cost[Resource.Loom] = 1;
                        Cost[Resource.Papyrus] = 1;
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Glass, Cost[Resource.Glass]) &&
                        player.ProduceResource(Resource.Loom, Cost[Resource.Loom]) &&
                        player.ProduceResource(Resource.Papyrus, Cost[Resource.Papyrus]))
                    {
                        Stage++;
                        player.DiscardFree = true;
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
                    if (player.ProduceResource(Resource.Ore, Cost[Resource.Ore]))
                    {
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]))
                    {
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Glass, Cost[Resource.Glass]) &&
                        player.ProduceResource(Resource.Loom, Cost[Resource.Loom]) &&
                        player.ProduceResource(Resource.Papyrus, Cost[Resource.Papyrus]))
                    {
                        stage_built = true;
                    }
                    break;
            }

            return stage_built;
        }
    }
}
