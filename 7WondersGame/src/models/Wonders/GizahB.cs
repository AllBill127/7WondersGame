using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class GizahB : Wonder
    {
        public GizahB() : base(WonderId.GizahB, 
            "Gizah B", 
            Resource.Stone, 
            new Dictionary<Resource, int> { { Resource.Wood, 2 } },
            new Dictionary<Resource, int>
            {
                { Resource.Wood, 2 },
                { Resource.Clay, 3 },
                { Resource.Stone, 4 },
                { Resource.Papyrus, 1 },
            })
        {
        }

        public override object DeepCopy()
        {
            GizahB copiedWonder = new();
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
                        WonderPoints += 3;
                        Cost[Resource.Stone] = 3;
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Stone, Cost[Resource.Stone]))
                    {
                        Stage++;
                        WonderPoints += 5;
                        Cost[Resource.Clay] = 3;
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]))
                    {
                        Stage++;
                        WonderPoints += 5;
                        Cost[Resource.Stone] = 4;
                        Cost[Resource.Papyrus] = 1;
                        stage_built = true;
                    }
                    break;

                // Building stage 4
                case 3:
                    if (player.ProduceResource(Resource.Stone, Cost[Resource.Stone]) &&
                        player.ProduceResource(Resource.Papyrus, Cost[Resource.Papyrus]))
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
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]))
                    {
                        stage_built = true;
                    }
                    break;

                // Building stage 4
                case 3:
                    if (player.ProduceResource(Resource.Stone, Cost[Resource.Stone]) &&
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
