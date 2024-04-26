using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGame.src.models
{
    internal class PlayerResults
    {
        public int Id { get; set; }

        public Wonder Board { get; set; }
        public List<Card> PlayedCards { get; set; }

        public int VictoryTokens = 0;
        public int DefeatTokens = 0;
        public int VictoryPoints = 0;

        public Dictionary<Resource, int> Resources = new Dictionary<Resource, int>()
        {
            { Resource.Wood, 0 },
            { Resource.Stone, 0 },
            { Resource.Ore, 0 },
            { Resource.Clay, 0 },
            { Resource.Glass, 0 },
            { Resource.Loom, 0 },
            { Resource.Papyrus, 0 },
            { Resource.Gear, 0 },
            { Resource.Compass, 0 },
            { Resource.Tablet, 0 },
            { Resource.Coins, 3 },
            { Resource.Shields, 0 },
        };

        public PlayerResults() { }
    }
}
