using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class EphesosB : Wonder
    {
        public EphesosB() : base(WonderId.EphesosB, 
            "Ephesos B", 
            Resource.Papyrus, 
            new Dictionary<Resource, int> { { Resource.Stone, 2 } },
            new Dictionary<Resource, int>
            {
                { Resource.Stone, 2 },
                { Resource.Wood, 2 },
                { Resource.Glass, 1 },
                { Resource.Loom, 1 },
                { Resource.Papyrus, 1 },
            })
        {
        }

        public override object DeepCopy()
        {
            EphesosB copiedWonder = new();
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
                        WonderPoints += 2;
                        player.Resources[Resource.Coins] += 4;
                        Cost[Resource.Wood] = 2;
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Wood, Cost[Resource.Wood]))
                    {
                        Stage++;
                        player.Resources[Resource.Coins] += 4;
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
                        WonderPoints += 5;
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
                    if (player.ProduceResource(Resource.Wood, Cost[Resource.Wood]))
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
