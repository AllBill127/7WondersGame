namespace _7WondersGame.src.models.MCTS
{
    // Node with pruned discard and build_wonder options
    internal class Node_v6
    {
        public Game GameState { get; }
        public int CurrentPlayerId { get; private set; }
        public int VisitCount { get; set; } = 0;
        public double TotalScore { get; set; } = 0;
        public double TotalScoreWL { get; set; } = 0;
        public List<double> TotalScoreVector { get; set; } = new List<double>();
        public List<double> TotalWLVector { get; set; } = new List<double>();
        public Node_v6? Parent { get; private set; }
        public List<Command> LegalMoves { get; }
        public List<Command> ExploredMoves { get; set; }
        public List<Node_v6> Children { get; }
        public Command? LastMove { get; private set; }

        public Node_v6(Game gameState, int currentPlayerId, Node_v6? parent, Command? lastMove)
        {
            GameState = gameState;
            CurrentPlayerId = currentPlayerId;
            Parent = parent;
            Children = new List<Node_v6>();
            LastMove = lastMove;

            for (int i = 0; i < gameState.Players.Count; i++)
            {
                TotalScoreVector.Add(0);
                TotalWLVector.Add(0);
            }

            // the moves the current player may make now (E1)
            LegalMoves = GameState.LegalMovesPruned(CurrentPlayerId);

            // initiate empty explored moves list
            ExploredMoves = new List<Command>();
        }

        public void ReleaseMemory()
        {
            foreach(var childNode in Children)
            {
                if (Children.Count > 0)
                {
                    childNode.ReleaseMemory();

                    GameState.ReleaseGameMemory();
                    TotalScoreVector.Clear();
                    TotalWLVector.Clear();
                    Parent = null;
                    LegalMoves.Clear();
                    ExploredMoves.Clear();
                    LastMove = null;
                }
                else
                {
                    GameState.ReleaseGameMemory();
                    TotalScoreVector.Clear();
                    TotalWLVector.Clear();
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
