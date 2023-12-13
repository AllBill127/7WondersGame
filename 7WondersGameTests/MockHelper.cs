using _7WondersGame.src.models.Wonders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _7WondersGame.src.models.Players;

namespace _7WondersGameTests
{
    internal class MockHelper
    {
        /// <summary>
        /// List of Gizah, Babylon, Olympia, Rhodos, Ephesos, Alexandria, Halikarnassos
        /// </summary>
        /// <returns>List of A and B Wonders</returns>
        public static List<Wonder> MockWonders()
        {
            List<Wonder> wonders = new List<Wonder>
            {
                new GizahA(),
                new BabylonA(),
                new OlympiaA(),
                new RhodosA(),
                new EphesosA(),
                new AlexandriaA(),
                new HalikarnassosA(),
                new GizahB(),
                new BabylonB(),
                new OlympiaB(),
                new RhodosB(),
                new EphesosB(),
                new AlexandriaB(),
                new HalikarnassosB()
            };

            return wonders;
        }

        public static List<Player> MockRandomChoiceAIPlayers(List<Wonder> wonders)
        {
            List<Player> playerList = new List<Player>();
            int numPlayers = wonders.Count;
            Player player;

            // create players
            for (int i = 0; i < numPlayers; i++)
            {
                player = new RandomChoiceAI(i);
                player.Board = wonders[i];
                playerList.Add(player);
            }

            // set player neighbors
            for (int i = 0; i < numPlayers; i++)
            {
                if (i == 0)
                {
                    playerList[i].PlayerWest = playerList[numPlayers - 1];
                    playerList[i].PlayerEast = playerList[i + 1];
                }
                else if (i == numPlayers - 1)
                {
                    playerList[i].PlayerWest = playerList[i - 1];
                    playerList[i].PlayerEast = playerList[0];
                }
                else
                {
                    playerList[i].PlayerWest = playerList[i - 1];
                    playerList[i].PlayerEast = playerList[i + 1];
                }
            }

            return playerList;
        }

        public static List<Player> MockPriorityAIPlayers(List<Wonder> wonders)
        {
            List<Player> playerList = new List<Player>();
            int numPlayers = wonders.Count;
            Player player;

            // create players
            for (int i = 0; i < numPlayers; i++)
            {
                player = new PriorityRandomChoiceAI(i);
                player.Board = wonders[i];
                playerList.Add(player);
            }

            // set player neighbors
            for (int i = 0; i < numPlayers; i++)
            {
                if (i == 0)
                {
                    playerList[i].PlayerWest = playerList[numPlayers - 1];
                    playerList[i].PlayerEast = playerList[i + 1];
                }
                else if (i == numPlayers - 1)
                {
                    playerList[i].PlayerWest = playerList[i - 1];
                    playerList[i].PlayerEast = playerList[0];
                }
                else
                {
                    playerList[i].PlayerWest = playerList[i - 1];
                    playerList[i].PlayerEast = playerList[i + 1];
                }
            }

            return playerList;
        }

        public static List<Player> MockDeterministicAIPlayers(List<Wonder> wonders)
        {
            List<Player> playerList = new List<Player>();
            int numPlayers = wonders.Count;
            Player player;

            // create players
            for (int i = 0; i < numPlayers; i++)
            {
                player = new DeterministicAI(i);
                player.Board = wonders[i];
                playerList.Add(player);
            }

            // set player neighbors
            for (int i = 0; i < numPlayers; i++)
            {
                if (i == 0)
                {
                    playerList[i].PlayerWest = playerList[numPlayers - 1];
                    playerList[i].PlayerEast = playerList[i + 1];
                }
                else if (i == numPlayers - 1)
                {
                    playerList[i].PlayerWest = playerList[i - 1];
                    playerList[i].PlayerEast = playerList[0];
                }
                else
                {
                    playerList[i].PlayerWest = playerList[i - 1];
                    playerList[i].PlayerEast = playerList[i + 1];
                }
            }

            return playerList;
        }
    }
}
