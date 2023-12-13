using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class RhodosA : Wonder
    {
        public RhodosA() : base(WonderId.RhodosA, 
            "Rhodos A", 
            Resource.Ore, 
            new Dictionary<Resource, int> { { Resource.Wood, 2 } },
            new Dictionary<Resource, int>
            {
                { Resource.Wood, 2 },
                { Resource.Clay, 3 },
                { Resource.Ore, 4 },
            })
        {
        }

        public override object DeepCopy()
        {
            RhodosA copiedWonder = new();
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
                        Cost[Resource.Clay] = 3;
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]))
                    {
                        Stage++;
                        player.Resources[Resource.Shields] += 2;
                        Cost[Resource.Ore] = 4;
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Ore, Cost[Resource.Ore]))
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
                    if (player.ProduceResource(Resource.Clay, Cost[Resource.Clay]))
                    {
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Ore, Cost[Resource.Ore]))
                    {
                        stage_built = true;
                    }
                    break;
            }

            return stage_built;
        }
    }
}
