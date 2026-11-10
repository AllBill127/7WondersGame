namespace _7WondersGame.src.models.MCTS
{
    /// <summary>The order players decide in the sequential MCTS trees (one player's move per tree level).</summary>
    internal static class SequentialTree
    {
        /// <summary>
        /// Who decides next: the searching player whenever they have something to decide, otherwise the lowest pending
        /// seat. -1 once the game is over.
        /// </summary>
        public static int NextPlayer(Game game, int searcher)
        {
            IReadOnlyList<int> pending = game.PendingPlayers();
            if (pending.Count == 0)
                return -1;
            return pending.Contains(searcher) ? searcher : pending[0];
        }
    }
}
