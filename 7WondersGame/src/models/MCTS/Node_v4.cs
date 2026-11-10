namespace _7WondersGame.src.models.MCTS
{
    internal class Node_v4
    {
        public Game GameState { get; }
        public int VisitCount { get; set; } = 0;
        public double TotalScore { get; set; } = 0;
        public double MCTPlayerScore { get; set; } = 0;
        public List<double> TotalScoreVector { get; set; } = new List<double>();
        public List<double> TotalWLVector { get; set; } = new List<double>();
        public Node_v4? Parent { get; private set; }
        public List<List<Command>> LegalJointMoves { get; }
        public List<List<Command>> ExploredJointMoves { get; set; }
        public List<Node_v4> Children { get; }
        public List<Command>? LastJointMove { get; private set; }
        //public Command? LastMove { get; }

        public Node_v4(Game gameState, Node_v4? parent, List<Command>? lastJointMove)
        {
            GameState = gameState;
            Parent = parent;
            Children = new List<Node_v4>();
            LastJointMove = lastJointMove;

            for (int i = 0; i < gameState.Players.Count; i++)
            {
                TotalScoreVector.Add(0);
                TotalWLVector.Add(0);
            }

            LegalJointMoves = GameState.JointMovesPruned();

            // initiate empty explored moves list
            ExploredJointMoves = new List<List<Command>>();
        }

        public void ReleaseMemory()
        {
            foreach (var childNode in Children)
            {
                if (Children.Count > 0)
                {
                    childNode.ReleaseMemory();

                    GameState.ReleaseGameMemory();
                    TotalScoreVector.Clear();
                    TotalWLVector.Clear();
                    Parent = null;
                    LegalJointMoves.Clear();
                    ExploredJointMoves.Clear();
                    LastJointMove = null;
                }
                else
                {
                    GameState.ReleaseGameMemory();
                    TotalScoreVector.Clear();
                    TotalWLVector.Clear();
                    Parent = null;
                    LegalJointMoves.Clear();
                    ExploredJointMoves.Clear();
                    Children.Clear();
                    LastJointMove = null;
                }
            }
        }
    }
}
