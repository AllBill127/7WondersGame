using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class RhodosB : Wonder
    {
        public RhodosB() : base(WonderId.RhodosB, 
            "Rhodos B", 
            Resource.Ore, 
            new Dictionary<Resource, int> { { Resource.Stone, 3 } },
            new Dictionary<Resource, int>
            {
                { Resource.Stone, 3 },
                { Resource.Ore, 4 },
            })
        {
        }

        public override object DeepCopy()
        {
            RhodosB copiedWonder = new();
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
                    if (player.ProduceResource(Resource.Stone, Cost[Resource.Stone]))
                    {
                        Stage++;
                        WonderPoints += 3;
                        player.Resources[Resource.Shields] += 1;
                        player.Resources[Resource.Coins] += 3;
                        Cost[Resource.Ore] = 4;
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Ore, Cost[Resource.Ore]))
                    {
                        Stage++;
                        WonderPoints += 4;
                        player.Resources[Resource.Shields] += 1;
                        player.Resources[Resource.Coins] += 4;
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
                    if (player.ProduceResource(Resource.Stone, Cost[Resource.Stone]))
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
            }

            return stage_built;
        }
    }
}
