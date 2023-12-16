using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGame.src.models.MCTS
{
    public class MCTSNewAI : Player
    {
        private class NodeNew
        {
            public int Visits { get; set; }
            public double Score { get; set; }
            public Command Move { get; set; }
            public List<NodeNew> Children { get; set; }
            public NodeNew Parent { get; set; }

            public NodeNew(Command move, NodeNew parent)
            {
                Move = move;
                Parent = parent;
                Children = new List<NodeNew>();
                Visits = 0;
                Score = 0;
            }
        }

        private const int MaxIterations = 5000;
        private const double ExplorationWeight = 1.4;

        public MCTSNewAI(int id) : base(id)
        {
        }

        public override object DeepCopy()
        {
            MCTSNewAI copy = new MCTSNewAI(this.Id)
            {
                Board = (Wonder)this.Board.DeepCopy(),
                HandCards = this.HandCards.Select(e => e).ToList(),
                PlayedCards = this.PlayedCards.Select(e => e).ToList(),
                PlayableCards = this.PlayableCards.Select(e => e).ToList(),

                VictoryTokens = this.VictoryTokens,
                DefeatTokens = this.DefeatTokens,
                VictoryPoints = this.VictoryPoints,
                // Babylon B stage 2
                PlaySeventh = this.PlaySeventh,
                // Olympia B stage 1
                WonderRawCheap = this.WonderRawCheap,
                RawCheapEast = this.RawCheapEast,
                RawCheapWest = this.RawCheapWest,
                // Marketplace card effect
                ManufCheap = this.ManufCheap,
                // Olympia A stage 2
                FreeCardOnce = this.FreeCardOnce,
                // Halikarnassos A stage 2 & B stage 1-3
                DiscardFree = this.DiscardFree,
                // Science guild effect
                ExtraScience = this.ExtraScience,
                CanBuildWonder = this.CanBuildWonder,

                // flags used to check multichoice materials only once
                UsedOnDemandResource = this.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Resources = this.Resources.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            return copy;
        }

        override public void ChooseMoveCommand(Game game)
        {
            Command bestCommand = FindBestMove(game, this.Id);

            // set the command and ready status in game
            if (bestCommand.Argument != null)
                game.PlayerCommands[this.Id] = bestCommand;
            else
                game.PlayerCommands[this.Id] = new Command("skip_move", null);
            game.PlayersReady[this.Id] = true;
        }

        /// <summary>
        /// chooses both move and card in MCTS case
        /// </summary>
        /// <param name="game"></param>
        /// <param name="commandStr"></param>
        /// <param name="cardsList"></param>
        /// <returns></returns>
        public override Card? ChooseMoveCard(Game game, string commandStr, List<Card> cardsList)
        {
            Command bestCommand = FindBestMove(game, this.Id);
            // extract move card
            Card? bestCard = bestCommand.Argument;
            return bestCard;
        }

        // ================================= MCTS implementation ==========================================
        public Command FindBestMove(Game gameState, int playerId)
        {
            NodeNew rootNode = new NodeNew(null, null);

            for (int i = 0; i < MaxIterations; i++)
            {
                // Selection
                NodeNew selectedNode = Select(rootNode, gameState.DeepCopy());

                // Expansion
                if (!selectedNode.Children.Any() && gameState.Era < 4)
                {
                    Expand(selectedNode, gameState.DeepCopy());
                }

                // Simulation
                double simulationResult = Simulate(selectedNode, gameState.DeepCopy(), playerId);

                // Backpropagation
                Backpropagate(selectedNode, simulationResult);
            }

            // Choose the best move based on the most visited child
            return GetBestMove(rootNode);
        }

        private NodeNew Select(NodeNew node, Game gameState)
        {
            while (node.Children.Any())
            {
                node = UCTSelect(node);
                gameState.ApplyMoveNew(node.Move, this.Id);
            }

            return node;
        }

        private NodeNew UCTSelect(NodeNew node)
        {
            // Select child node based on UCT (Upper Confidence Bound applied to Trees) formula
            double explorationFactor = 1.4;
            NodeNew res = node.Children.OrderByDescending(c =>
                c.Score / c.Visits + explorationFactor * Math.Sqrt(Math.Log(node.Visits) / c.Visits)
            ).First();

            return res;
        }

        private void Expand(NodeNew node, Game gameState)
        {
            //// Expand by adding a child for each possible move
            //List<Command> possibleMoves = gameState.GetPossibleMoves(node.Move);
            //foreach (Command move in possibleMoves)
            //{
            //    NodeNew childNode = new NodeNew(move, node);
            //    node.Children.Add(childNode);
            //}

            // Expand by adding a child for each possible move for all players
            List<Command> possibleMovesAllPlayers = gameState.GetPossibleMovesForAllPlayers(node.Move, this.Id);
            foreach (Command move in possibleMovesAllPlayers)
            {
                NodeNew childNode = new NodeNew(move, node);
                node.Children.Add(childNode);
            }
        }

        private double Simulate(NodeNew node, Game gameState, int playerId)
        {
            //// Simulate the game from the current state until the end and return the result
            //while (gameState.Era < 4)
            //{
            //    Command randomMove = gameState.GetRandomMove();
            //    gameState.ApplyMoveNew(randomMove);
            //}

            //// Return the score for the specified player
            //return gameState.GetPlayerScore(playerId);

            // cast any mcts players to deterministicAI player
            for (int i = 0; i < gameState.Players.Count; i++)
            {
                Player player = gameState.Players[i];

                if (player.GetType() == typeof(MCTSAI))
                {
                    DeterministicAI simPlayer = new DeterministicAI(player.Id)
                    {
                        Board = (Wonder)player.Board.DeepCopy(),
                        HandCards = player.HandCards.Select(e => e).ToList(),
                        PlayedCards = player.PlayedCards.Select(e => e).ToList(),
                        PlayableCards = player.PlayableCards.Select(e => e).ToList(),

                        VictoryTokens = player.VictoryTokens,
                        DefeatTokens = player.DefeatTokens,
                        VictoryPoints = player.VictoryPoints,
                        // Babylon B stage 2
                        PlaySeventh = player.PlaySeventh,
                        // Olympia B stage 1
                        RawCheapEast = player.RawCheapEast,
                        RawCheapWest = player.RawCheapWest,
                        // Marketplace card effect
                        ManufCheap = player.ManufCheap,
                        // Olympia A stage 2
                        FreeCardOnce = player.FreeCardOnce,
                        // Halikarnassos A stage 2 & B stage 1-3
                        DiscardFree = player.DiscardFree,
                        // Science guild effect
                        ExtraScience = player.ExtraScience,
                        CanBuildWonder = player.CanBuildWonder,

                        // flags used to check multichoice materials only once
                        UsedOnDemandResource = player.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        Resources = player.Resources.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    };
                    gameState.Players[player.Id] = simPlayer;
                }
            }

            // set neighbors for recasted players
            gameState.SetPlayerNeighbors();

            // simulate game until the end
            Dictionary<int, int> scores = gameState.Loop();

            return scores[playerId];
        }

        private void Backpropagate(NodeNew node, double result)
        {
            // Backpropagate the simulation result up the tree
            while (node != null)
            {
                node.Visits++;
                node.Score += result;
                node = node.Parent;
            }
        }

        private Command GetBestMove(NodeNew rootNode)
        {
            // Choose the move with the highest number of visits
            return rootNode.Children.OrderByDescending(c => c.Visits).First().Move;
        }
    }
}
