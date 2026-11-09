using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGame.src.models.MCTS
{
    public class MCTSAIv2 : Player
    {
        private int MaxIterations = 10000;
        private const double EXPLORATION_WEIGHT = 50;

        public MCTSAIv2(int id) : base(id)
        {
        }

        public override object DeepCopy()
        {
            MCTSAIv2 copy = new MCTSAIv2(this.Id)
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
            if (bestCommand.Card != null)
                game.PlayerCommands[this.Id] = bestCommand;
            else
                game.PlayerCommands[this.Id] = new Command(ActionType.Skip, null);
            game.PlayersReady[this.Id] = true;
        }
        
        /// <summary>
        /// chooses both move and card in MCTS case
        /// </summary>
        /// <param name="game"></param>
        /// <param name="action"></param>
        /// <param name="cardsList"></param>
        /// <returns></returns>
        public override Card? ChooseMoveCard(Game game, ActionType action, List<Card> cardsList)
        {
            Command bestCommand = FindBestMove(game);
            // extract move card
            Card? bestCard = bestCommand.Card;
            return bestCard;
        }


        // =============== MCTS ===============
        // IMPROVEMENTS: 
        //                  * Progressive widening
        //                  // * Discourage 'discard' moves
        //                   * DDK implementation in simulations
        //                  //* rng simulations
        //                  * backpropagating game scores
        //                  // * UCT with deminishing exploration
        //                  * pruned discards

        public Command FindBestMove(Game game)
        {
            Node_v2 rootNode = new Node_v2(game.DeepCopy(), this.Id, null, null); // Create a clone of the game state as the root node and copy root player

            // MCTS IMPROVEMENT: Progressive widening
            double ageMultiplier = game.Era / 2d;

            for (int iteration = 0; iteration < (MaxIterations * ageMultiplier); )
            {
                Node_v2 selectedNode = Selection(rootNode);
                Node_v2 expandedNode = Expansion(selectedNode);

                // call simulation and backpropagation if a turn was completed by all players or extra turn was completed
                //      (Should be indicated by any change in players hand cards or played cards)
                //      if discardFree extra turn then change in played cards (increased by 1 for a player wit that effect)
                //      if game turn or playeSeventh extra turn then change in hand cards (decreased by 1 for at least one player)
                //
                //  should check parent node game state and check if game state in current node changed
                //  and current node is not extra turn expansion (current player has playSeventh or discardFree flag AND turn is 6 or 7 respectfully)
                if (TurnEnded(expandedNode)) 
                {
                    Dictionary<int, int> simulationResult = Simulation(expandedNode);

                    Backpropagation(expandedNode, simulationResult);

                    iteration++;
                }
            }

            // Choose the best child of the root node as the final move
            Command? bestMove = BestUCTChild(rootNode).LastMove;

            if (bestMove != null)
            {
                rootNode.ReleaseMemory();
                return bestMove;
            }
            else
                return new Command(ActionType.Skip, null);
        }

        private bool TurnEnded(Node_v2 expandedNode)
        {
            bool turnEnded = false;

            if (expandedNode.Parent is null)
                return turnEnded;
            if (expandedNode.Parent.GameState.CompletedTurn(expandedNode.GameState))
                turnEnded = true;
            if (expandedNode.GameState.Players[expandedNode.CurrentPlayerId].PlaySeventh == true &&
                (expandedNode.GameState.Turn + 1) % 7 == 0 &&
                expandedNode.GameState.PlayersReady[expandedNode.CurrentPlayerId] == false)
                turnEnded = false;
            // if current nodes player is a player with extra turn effect, the turn number is considered possible extra turn and player is not ready
            // then turn is not complete as extra move has to be done before simulation
            if (expandedNode.GameState.Players[expandedNode.CurrentPlayerId].DiscardFree == true && 
                (expandedNode.GameState.Turn % 7 == 0 &&
                expandedNode.GameState.PlayersReady[expandedNode.CurrentPlayerId] == false))
                turnEnded = false;

            return turnEnded;

        }

        private Node_v2 Selection(Node_v2 node)
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

        private Node_v2 SelectChild(Node_v2 node)
        {
            // simply choose child with best UCT score (basic approach)
            double bestUCT = double.MinValue;
            Node_v2 bestChild = null;

            foreach (Node_v2 child in node.Children)
            {
                double uctValue = UCTValue(child);
                if (uctValue > bestUCT)
                {
                    bestUCT = uctValue;
                    bestChild = child;
                }
            }

            return bestChild;
        }

        private double UCTValue(Node_v2 node)
        {
            // Upper Confidence Bound applied to Trees (UCT) formula
            double exploitation = node.TotalScore / (node.VisitCount + 1);
            double exploration = Math.Sqrt(Math.Log((node.Parent?.VisitCount ?? 0) + 1) / (node.VisitCount + 1));

            //// --- MCTS IMPROVEMENT: deminishing exploration
            //exploration *= (1.0 / (1.0 + node.VisitCount));

            return exploitation + EXPLORATION_WEIGHT * exploration;
        }

        /// <summary>
        /// select an unexplored move and add a child with it to the tree
        /// </summary>
        /// <param name="node"></param>
        /// <returns>newly created child node</returns>
        private Node_v2 Expand(Node_v2 node)
        {
            // get unexplored moves
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
            int nextPlayer = newState.ApplyMove(node.CurrentPlayerId, selectedMove);

            // NOTE:    can add a check if nextplayer is Players[0] && node.GameState.Turn != newState.Turn
            //          meaning that a turn has been applied and nextPlayer - Player[0] was returned as a ahrd coded choice.
            //          In this situation nextPlayer can be set to MCST main player - this.Id
            
            // if nextPlayer is null, then game has ended
            if (nextPlayer == -1)
            {
                // NOTE:    unsure if current player as next player is the correct assumption in end of game situation
                Node_v2 newChild = new Node_v2(newState, node.CurrentPlayerId, node, selectedMove);
                node.Children.Add(newChild);
            }
            else
            {
                Node_v2 newChild = new Node_v2(newState, nextPlayer, node, selectedMove);
                node.Children.Add(newChild);
            }

            // return newly created child node
            return node.Children.Last();
        }

        private Node_v2 Expansion(Node_v2 node)
        {
            // Choose an unexplored child if available; otherwise, return the same node
            if (node.GameState.Era < 4 && node.Children.Count < node.LegalMoves.Count)
                return Expand(node);
            else
                return node;
        }

        private Dictionary<int, int> Simulation(Node_v2 node)
        {
            Game simGame = node.GameState.DeepCopy();

            // MCTS IMPROVEMENT: DDK in simulations
            // cast any players to deterministicAI player for simulation 
            for (int i = 0; i < simGame.Players.Count; i++)
            {
                Player player = simGame.Players[i];

                if (player.GetType() != typeof(DeterministicAI))
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

            // set neighbors for recasted players
            simGame.SetPlayerNeighbors();

            // simulate game until the end
            Dictionary<int, int> scores = simGame.Loop();

            // Return the results of the simulation
            //return simulationResults;
            return scores;
        }

        private void Backpropagation(Node_v2 node, Dictionary<int, int> results)
        {
            Node_v2 currentNode = node;

            // traverse the tree updating values until root 
            while (currentNode.Parent != null)
            {
                currentNode.TotalScore += results[this.Id];
                currentNode.MCTPlayerScore = currentNode.TotalScore;

                // update leaf node with simulation results only for the main mcts player
                if (currentNode.CurrentPlayerId == this.Id)
                {
                    // determine what place this player scored in the game
                    double rank = 1 + results
                        .OrderByDescending(x => x.Value)
                        .Select((entry, i) => new { entry.Key, Index = i })
                        .FirstOrDefault(x => x.Key == this.Id)?.Index ?? -1;

                    if (rank == 1)
                    {
                        // --- MCTS IMPROVEMENT: discourage discard
                        //if (currentNode.LastMove is not null && currentNode.LastMove.Action == ActionType.Discard)
                        //    rank *= DISCOURAGE_DISCARD;
                        currentNode.TotalScoreWL += rank;
                    }
                }
                else
                {
                    currentNode.TotalScore += results[currentNode.CurrentPlayerId];
                    if (currentNode.MCTPlayerScore < results[this.Id])
                    {
                        currentNode.MCTPlayerScore = results[this.Id];
                    }

                    // determine what place current node player scored in the game
                    double rank = 1 + results
                        .OrderByDescending(x => x.Value)
                        .Select((entry, i) => new { entry.Key, Index = i })
                        .FirstOrDefault(x => x.Key == currentNode.CurrentPlayerId)?.Index ?? -1;

                    if (rank == 1)
                    {
                        // --- MCTS IMPROVEMENT: discourage discard
                        //if (currentNode.LastMove is not null && currentNode.LastMove.Action == ActionType.Discard)
                        //    rank *= DISCOURAGE_DISCARD;
                        currentNode.TotalScoreWL += rank;
                    }
                }
                
                // update visit count for each visited node in path
                currentNode.VisitCount++;

                // move up the tree
                currentNode = currentNode.Parent;
            }

            // update root node with simulation results only for the main mcts player
            if (currentNode.CurrentPlayerId == this.Id)
            {
                currentNode.TotalScore += results[this.Id];
                currentNode.MCTPlayerScore = currentNode.TotalScore;

                // determine what place this player scored in the game
                double rank = 1 + results
                    .OrderByDescending(x => x.Value)
                    .Select((entry, i) => new { entry.Key, Index = i })
                    .FirstOrDefault(x => x.Key == currentNode.CurrentPlayerId)?.Index ?? -1;

                if (rank == 1)
                {
                    // --- MCTS IMPROVEMENT: discourage discard
                    //if (currentNode.LastMove is not null && currentNode.LastMove.Action == ActionType.Discard)
                    //    rank *= DISCOURAGE_DISCARD;
                    currentNode.TotalScoreWL += rank;
                }
            }
            // update visit count 
            currentNode.VisitCount++;
        }

        private Node_v2 BestUCTChild(Node_v2 node)
        {
            double bestUCT = double.MinValue;
            double uctValue;
            Node_v2 bestChild = null;

            foreach (Node_v2 child in node.Children)
            {
                // calculate uct based on passed through mcts player score
                // --- MCTS IMPROVEMENT: new best move evaluation function
                //uctValue = child.TotalScore / child.VisitCount / SCORE_NORMALIZATION + child.MCTPlayerScore / 32;
                uctValue = child.TotalScore;

                if (uctValue > bestUCT)
                {
                    bestUCT = uctValue;
                    bestChild = child;
                }
            }

            return bestChild;
        }
    }
}
