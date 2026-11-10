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

            // the moves the current player may make now (E1)
            LegalMoves = GameState.LegalMovesPruned(CurrentPlayerId);

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
