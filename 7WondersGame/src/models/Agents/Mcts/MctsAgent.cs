using System.Diagnostics;
using System.Text.Json;

namespace _7WondersGame.src.models.Agents.Mcts
{
    /// <summary>
    /// The one configurable MCTS agent (fix plan step 6, E + D1), which replaces the old v1-v12 and JM agents. Every
    /// choice the old versions made differently is a setting in <see cref="Config"/>, logged with every result row.
    /// By default it doesn't see hidden cards: each iteration searches a new guess at them (SO-ISMCTS, M1).
    /// A decision with only one legal move is made without a search.
    /// </summary>
    public sealed class MctsAgent : IAgent
    {
        /// <summary>Its settings.</summary>
        public MctsConfig Config { get; init; } = new();

        // what it measured over its game (an agent is made for one game); in an object, so these aren't settings
        private readonly SearchStats _stats = new();

        private sealed class SearchStats
        {
            public int Searches;
            public long Iterations;
            public double SearchMs;
            public double LongestMs;
        }

        public Command Choose(GameView view)
        {
            List<Command> legal = view.LegalMoves();
            if (legal.Count == 1)
                return legal[0];

            var clock = Stopwatch.StartNew();
            var search = new MctsSearch(Config, view.Seat, view.CopyGame(), view.Known, view.Random);
            Command move = search.Run(clock);
            double ms = clock.Elapsed.TotalMilliseconds;

            _stats.Searches++;
            _stats.Iterations += search.Iterations;
            _stats.SearchMs += ms;
            _stats.LongestMs = Math.Max(_stats.LongestMs, ms);
            return move;
        }

        /// <summary>Searches made, iterations in all, and search time in milliseconds (in all and the longest).</summary>
        public string StatsJson() => JsonSerializer.Serialize(new
        {
            searches = _stats.Searches,
            iterations = _stats.Iterations,
            searchMs = Math.Round(_stats.SearchMs),
            longestSearchMs = Math.Round(_stats.LongestMs),
        });
    }
}
