using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGame.src.models.MCTS
{
    public class MCTSAI : Player
    {
        private const int MaxIterations = 5000;
        private const double EXPLORATION_WEIGHT = 3.6;
        private const double DISCARD_DISCOURAGE = 1;
        private const double BEST_MOVE_EXPLORATION_WEIGHT = 1;
        // NOTE:
        //      GizahB MCTS won with EXPLORATION_WEIGHT = 3.6 | DISCARD_DISCOURAGE = 1 | BEST_MOVE_EXPLORATION_WEIGHT = 1
        //      

        public MCTSAI(int id) : base(id)
        {
        }

        public override object DeepCopy()
        {
            MCTSAI copy = new MCTSAI(this.Id)
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
            Command bestCommand = FindBestMove(game);

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
            Command bestCommand = FindBestMove(game);
            // extract move card
            Card? bestCard = bestCommand.Argument;
            return bestCard;
        }


        // =============== MCTS ===============
        
        public Command FindBestMove(Game game)
        {
            Node rootNode = new Node(game.DeepCopy(), (Player)game.Players[this.Id].DeepCopy(), null, null); // Create a clone of the game state as the root node and copy root player

            for (int iteration = 0; iteration < MaxIterations;)
            {
                // TEST:
                if (iteration == MaxIterations - 1)
                { }

                Node selectedNode = Selection(rootNode);
                Node expandedNode = Expansion(selectedNode);

                // call simulation and backpropagation if a turn was completed by all players or extra turn was completed
                //      (Should be indicated by any change in players hand cards or played cards)
                //      if discardFree extra turn then change in played cards (increased by 1 for a player wit that effect)
                //      if game turn or playeSeventh extra turn then change in hand cards (decreased by 1 for at least one player)
                //
                //  should check parent node game state and check if game state in current node changed
                //  and current node is not extra turn expansion (current player has playSeventh or discardFree flag AND turn is 6 or 7 respectfully)
                if (TurnEnded(expandedNode)) 
                {
                    double simulationResult = Simulation(expandedNode);

                    // discourage discarding cards
                    if (expandedNode.GameState.PlayerCommands[this.Id].Subcommand.Equals("discard"))
                    {
                        simulationResult = simulationResult * DISCARD_DISCOURAGE;
                    }
                    Backpropagation(expandedNode, simulationResult);

                    iteration++;
                }
            }

            // Choose the best child of the root node as the final move
            Command? bestMove = BestUCTChild(rootNode).LastMove;

            if (bestMove != null)
                return bestMove;
            else
                return new Command("skip_move", null);
        }

        private bool TurnEnded(Node expandedNode)
        {
            bool turnEnded = false;

            if (expandedNode.Parent is null)
                return turnEnded;
            if (expandedNode.Parent.GameState.CompletedTurn(expandedNode.GameState))
                turnEnded = true;
            if (expandedNode.CurrentPlayer.PlaySeventh == true &&
                (expandedNode.GameState.Turn + 1) % 7 == 0 &&
                expandedNode.GameState.PlayersReady[expandedNode.CurrentPlayer.Id] == false)
                turnEnded = false;
            // if current nodes player is a player with extra turn effect, the turn number is considered possible extra turn and player is not ready
            // then turn is not complete as extra move has to be done before simulation
            if (expandedNode.GameState.Players[expandedNode.CurrentPlayer.Id].DiscardFree == true && 
                (expandedNode.GameState.Turn % 7 == 0 &&
                expandedNode.GameState.PlayersReady[expandedNode.CurrentPlayer.Id] == false))
                turnEnded = false;

            return turnEnded;

        }

        private Node Selection(Node node)
        {
            while (!(node.GameState.Era >= 4) && node.Children.Count > 0)
            {
                if (node.Children.Count < node.LegalMoves.Count)
                {
                    // If there are unexplored children, choose one and return
                    return Expand(node);
                }
                else
                {
                    // selection strategy
                    node = SelectChild(node);
                }
            }

            return node;
        }

        private Node SelectChild(Node node)
        {
            // simply choose child with best UCT score (basic approach)
            double bestUCT = double.MinValue;
            Node bestChild = null;

            foreach (Node child in node.Children)
            {
                double uctValue = UCTValue(child);
                if (uctValue > bestUCT)
                {
                    bestUCT = uctValue;
                    bestChild = child;
                }
            }

            if (bestChild == null)
            { }

            return bestChild;
        }

        private double UCTValue(Node node)
        {
            // Upper Confidence Bound applied to Trees (UCT) formula
            double exploitation = node.TotalScore / (node.VisitCount + 1);
            double exploration = Math.Sqrt(Math.Log((node.Parent?.VisitCount ?? 0) + 1) / (node.VisitCount + 1));
            return exploitation + EXPLORATION_WEIGHT * exploration;
        }

        /// <summary>
        /// select an unexplored move and add a child with it to the tree
        /// </summary>
        /// <param name="node"></param>
        /// <returns>newly created child node</returns>
        private Node Expand(Node node)
        {
            // get unexplored moves
            //List<Command> unexploredMoves = node.LegalMoves.Select(e => e).Where(e => !node.ExploredMoves.Contains(e)).ToList();
            List<Command> unexploredMoves = node.LegalMoves
                .Where(move => !node.ExploredMoves.Contains(move))
                .ToList();

            // select move
            Command selectedMove = unexploredMoves.First();
            // add move to explored list
            node.ExploredMoves.Add(selectedMove);

            // create child node with new game state after selected move
            // apply move to new state
            Game newState = node.GameState.DeepCopy();
            Player? nextPlayer = newState.ApplyMove(node.CurrentPlayer, selectedMove);

            // TEST:
            if (newState.Players[0].HandCards.Count == 0 && newState.Era < 4 && newState.Turn % 7 != 0)
            { }

            // NOTE:    can add a check if nextplayer is Players[0] && node.GameState.Turn != newState.Turn
            //          meaning that a turn has been applied and nextPlayer - Player[0] was returned as a ahrd coded choice.
            //          In this situation nextPlayer can be set to MCST main player - this.Id
            
            // if nextPlayer is null, then game has ended
            if (nextPlayer is null)
            {
                // NOTE:    unsure if current player as next player is the correct assumption in end of game situation
                Node newChild = new Node(newState, node.CurrentPlayer, node, selectedMove);
                node.Children.Add(newChild);
            }
            else
            {
                Node newChild = new Node(newState, nextPlayer, node, selectedMove);
                node.Children.Add(newChild);
            }

            // return newly created child node
            return node.Children.Last();
        }

        private Node Expansion(Node node)
        {
            // Choose an unexplored child if available; otherwise, return the same node
            if (node.GameState.Era < 4 && node.Children.Count < node.LegalMoves.Count)
                return Expand(node);
            else
                return node;
        }

        private double Simulation(Node node)
        {
            double simulationResults = 0;
            Game simGame = node.GameState.DeepCopy();

            // TEST:
            if (simGame.Turn % 7 == 0 && simGame.Turn != 0 &&
                simGame.Players[0].HandCards.Count == 0 && simGame.Era < 4)
            { }

            
            // cast any mcts players to deterministicAI player
            for (int i = 0; i < simGame.Players.Count; i++)
            {
                Player player = simGame.Players[i];

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
                    simGame.Players[player.Id] = simPlayer;
                }
            }

            /* // cast this MCTS player to Deterministic and all other players to RandomChoice
            for (int i = 0; i < simGame.Players.Count; i++)
            {
                Player player = simGame.Players[i];

                if (player.Id == this.Id)
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
                    simGame.Players[player.Id] = simPlayer;
                }
                else
                {
                    RandomChoiceAI simPlayer = new RandomChoiceAI(player.Id)
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
                    simGame.Players[player.Id] = simPlayer;
                }
            }
            */

            // set neighbors for recasted players
            simGame.SetPlayerNeighbors();

            // simulate game until the end
            Dictionary<int, int> scores = simGame.Loop();

            // get this players score in the end of game
            int simScore = scores[this.Id];

            // determine what place this player scored in the game
            int simRank = scores
                .OrderByDescending(x => x.Value)
                .Select((entry, i) => new { entry.Key, Index = i })
                .FirstOrDefault(x => x.Key == this.Id)?.Index ?? -1;

            // calculate simulation results - player score normalized by his overal rank
            //simulationResults = simScore / simRank;
            //simulationResults = simScore * (simRank == 1 ? 1.2 : 1);
            simulationResults = simScore;

            // Return the result of the simulation
            return simulationResults;
        }

        private void Backpropagation(Node node, double result)
        {
            Node currentNode = node;
            double childMCTSPlayerScore = 0;

            // traverse the tree updating values until root 
            while (currentNode.Parent != null)
            {
                // update leaf node with simulation results only for the main mcts player
                if (currentNode.CurrentPlayer.Id == this.Id)
                {
                    currentNode.TotalScore += result;
                    currentNode.MCTPlayerScore = currentNode.TotalScore;
                    childMCTSPlayerScore = currentNode.MCTPlayerScore;
                }
                else
                {
                    //currentNode.MCTPlayerScore = childMCTSPlayerScore;
                    if (currentNode.MCTPlayerScore < result)
                    {
                        currentNode.MCTPlayerScore = result;
                        currentNode.TotalScore = result;
                    }
                }
                
                // update visit count for each visited node in path
                currentNode.VisitCount++;

                // move up the tree
                currentNode = currentNode.Parent;
            }

            // update root node with simulation results only for the main mcts player
            if (currentNode.CurrentPlayer.Id == this.Id)
            {
                currentNode.TotalScore += result;
                currentNode.MCTPlayerScore = result;
            }
            // update visit count 
            currentNode.VisitCount++;
        }

        private Node BestUCTChild(Node node)
        {
            double bestUCT = double.MinValue;
            double exploitation, exploration, uctValue;
            Node bestChild = null;

            foreach (Node child in node.Children)
            {
                // calculate uct based on passed through mcts player score
                //exploitation = child.MCTPlayerScore / (child.VisitCount + 1);
                //exploration = Math.Sqrt(Math.Log((child.Parent?.VisitCount ?? 0) + 1) / (child.VisitCount + 1));
                //uctValue = exploitation + BEST_MOVE_EXPLORATION_WEIGHT * exploration;
                uctValue = child.MCTPlayerScore;

                if (uctValue > bestUCT)
                {
                    bestUCT = uctValue;
                    bestChild = child;
                }
            }

            // TEST:
            if (bestChild == null)
            { }

            return bestChild;
        }
    }
}
