using _7WondersGame.src.models.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace _7WondersGame.src.models.MCTS
{
    internal class Node
    {
        public Game GameState { get; }
        public Player CurrentPlayer { get; }
        public int VisitCount { get; set; } = 0;
        public double TotalScore { get; set; } = 0;
        public double MCTPlayerScore { get; set; } = 0;
        public Node? Parent { get; }
        public List<Command> LegalMoves { get; }
        public List<Command> ExploredMoves { get; set; }
        public List<Node> Children { get; }
        //public List<Command>? LastJointMove { get; }
        public Command? LastMove { get; }

        //public Node(Game gameState, Node? parent = null, List<Command>? lastJointMove = null)
        public Node(Game gameState, Player currentPlayer, Node? parent, Command? lastMove)
        {
            GameState = gameState;
            CurrentPlayer = currentPlayer;
            Parent = parent;
            Children = new List<Node>();
            //LastJointMove = lastJointMove;
            LastMove = lastMove;

            // get legal moves for nodes current player based on current game state
            // if currentPlayer DiscardFree is true, then he has to expand on building any card from discarded cards 
            if (CurrentPlayer.DiscardFree)
                LegalMoves = GameState.GetLegalMoves(CurrentPlayer.Id, discardFree: true);
            // if currentPlayer PlaySeveth is true and gameState.turn == 6 13 20, then he has to expand on last card all actions
            else if (CurrentPlayer.PlaySeventh &&
                    (GameState.Turn == 6 ||
                    GameState.Turn == 13 ||
                    GameState.Turn == 20))
                LegalMoves = GameState.GetLegalMoves(CurrentPlayer.Id, playSeventh: true);
            // if currentPlayer FreeCardOnce is true, then he can also expand on building all hand cards free
            // if regular move, then currentPlayer has to expand on build action with playable cards or discard, build wonder actions with any hand cards
            else
                LegalMoves = GameState.GetLegalMoves(CurrentPlayer.Id);

            // initiate empty explored moves list
            ExploredMoves = new List<Command>();
        }
    }
}
