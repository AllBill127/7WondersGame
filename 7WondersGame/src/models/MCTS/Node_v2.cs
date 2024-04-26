using _7WondersGame.src.models.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace _7WondersGame.src.models.MCTS
{
    // Node with pruned discard and build_wonder options
    internal class Node_v2
    {
        public Game GameState { get; }
        public int CurrentPlayerId { get; }
        public int VisitCount { get; set; } = 0;
        public double TotalScore { get; set; } = 0;
        public double MCTPlayerScore { get; set; } = 0;
        public double TotalScoreWL { get; set; } = 0;
        public double MCTPlayerScoreWL { get; set; } = 0;
        public Node_v2? Parent { get; private set; }
        public List<Command> LegalMoves { get; }
        public List<Command> ExploredMoves { get; set; }
        public List<Node_v2> Children { get; }
        public Command? LastMove { get; private set; }

        public Node_v2(Game gameState, int currentPlayerId, Node_v2? parent, Command? lastMove)
        {
            GameState = gameState;
            CurrentPlayerId = currentPlayerId;
            Parent = parent;
            Children = new List<Node_v2>();
            LastMove = lastMove;

            // get legal moves for nodes current player based on current game state
            // if currentPlayerId DiscardFree is true, then he has to expand on building any card from discarded cards 
            if (GameState.Players[CurrentPlayerId].DiscardFree)
                LegalMoves = GameState.GetLegalMovesPrunedDiscards(CurrentPlayerId, discardFree: true);
            // if currentPlayerId PlaySeveth is true and gameState.turn == 6 13 20, then he has to expand on last card all actions
            else if (GameState.Players[CurrentPlayerId].PlaySeventh &&
                    (GameState.Turn == 6 ||
                    GameState.Turn == 13 ||
                    GameState.Turn == 20))
                LegalMoves = GameState.GetLegalMovesPrunedDiscards(CurrentPlayerId, playSeventh: true);
            // if currentPlayerId FreeCardOnce is true, then he can also expand on building all hand cards free
            // if regular move, then currentPlayerId has to expand on build action with playable cards or discard, build wonder actions with any hand cards
            else
                LegalMoves = GameState.GetLegalMovesPrunedDiscards(CurrentPlayerId);

            // initiate empty explored moves list
            ExploredMoves = new List<Command>();
        }

        public void ReleaseMemory()
        {
            foreach (var childNode in Children)
            {
                if (Children.Count > 0)
                {
                    childNode.ReleaseMemory();

                    GameState.ReleaseGameMemory();
                    Parent = null;
                    LegalMoves.Clear();
                    ExploredMoves.Clear();
                    Children.Clear();
                    LastMove = null;
                }
                else
                {
                    GameState.ReleaseGameMemory();
                    Parent = null;
                    LegalMoves.Clear();
                    ExploredMoves.Clear();
                    Children.Clear();
                    LastMove = null;
                }
            }
        }
    }
}
