using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class HalikarnassosA : Wonder
    {
        public HalikarnassosA() : base(WonderId.HalikarnassosA,
            "Halikarnassos A", 
            Resource.Loom, 
            new Dictionary<Resource, int> { { Resource.Clay, 2 } },
            new Dictionary<Resource, int>
            {
                { Resource.Clay, 2 },
                { Resource.Ore, 3 },
                { Resource.Loom, 2 },
            })
        {
        }

        public override object DeepCopy()
        {
            HalikarnassosA copiedWonder = new();
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
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]))
                    {
                        Stage++;
                        WonderPoints += 3;
                        Cost[Resource.Ore] = 3;
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Ore, Cost[Resource.Ore]))
                    {
                        Stage++;
                        player.DiscardFree = true;
                        Cost[Resource.Loom] = 2;
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Loom, Cost[Resource.Loom]))
                    {
                        Stage++;
                        WonderPoints += 7;
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
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]))
                    {
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Ore, Cost[Resource.Ore]))
                    {
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Loom, Cost[Resource.Loom]))
                    {
                        stage_built = true;
                    }
                    break;
            }

            return stage_built;
        }
    }
}
