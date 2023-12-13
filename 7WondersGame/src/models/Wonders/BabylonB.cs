using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class BabylonB : Wonder
    {
        public BabylonB() : base(WonderId.BabylonB, 
            "Babylon B", 
            Resource.Clay, 
            new Dictionary<Resource, int> { { Resource.Clay, 1 }, { Resource.Loom, 1 }, },
            new Dictionary<Resource, int>
            {
                { Resource.Loom, 1 },
                { Resource.Wood, 2 },
                { Resource.Glass, 1 },
                { Resource.Clay, 3 },
                { Resource.Papyrus, 1 },
            })
        {
        }

        public override object DeepCopy()
        {
            BabylonB copiedWonder = new();
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
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]) &&
                        player.ProduceResource(Resource.Loom, Cost[Resource.Loom]))
                    {
                        Stage++;
                        WonderPoints += 3;
                        Cost[Resource.Wood] = 2;
                        Cost[Resource.Glass] = 1;
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Wood, Cost[Resource.Wood]) &&
                        player.ProduceResource(Resource.Glass, Cost[Resource.Glass]))
                    {
                        Stage++;
                        player.PlaySeventh = true;
                        Cost[Resource.Clay] = 3;
                        Cost[Resource.Papyrus] = 1;
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]) &&
                        player.ProduceResource(Resource.Papyrus, Cost[Resource.Papyrus]))
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
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]) &&
                        player.ProduceResource(Resource.Loom, Cost[Resource.Loom]))
                    {
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Wood, Cost[Resource.Wood]) &&
                        player.ProduceResource(Resource.Glass, Cost[Resource.Glass]))
                    {
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]) &&
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
