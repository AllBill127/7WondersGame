using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public class EphesosA : Wonder
    {
        public EphesosA() : base(WonderId.EphesosA, 
            "Ephesos A", 
            Resource.Papyrus, 
            new Dictionary<Resource, int> { { Resource.Stone, 2 } },
            new Dictionary<Resource, int>
            {
                { Resource.Stone, 2 },
                { Resource.Wood, 2 },
                { Resource.Papyrus, 2 },
            })
        {
        }

        public override object DeepCopy()
        {
            EphesosA copiedWonder = new();
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
                        Cost[Resource.Wood] = 2;
                        stage_built = true;
                    }
                    break;

                // Building stage 2
                case 1:
                    if (player.ProduceResource(Resource.Wood, Cost[Resource.Wood]))
                    {
                        Stage++;
                        player.Resources[Resource.Coins] += 9;
                        Cost[Resource.Papyrus] = 2;
                        stage_built = true;
                    }
                    break;

                // Building stage 3
                case 2:
                    if (player.ProduceResource(Resource.Papyrus, Cost[Resource.Papyrus]))
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
                    if (player.ProduceResource(Resource.Papyrus, Cost[Resource.Papyrus]))
                    {
                        stage_built = true;
                    }
                    break;
            }

            return stage_built;
        }
    }
}
