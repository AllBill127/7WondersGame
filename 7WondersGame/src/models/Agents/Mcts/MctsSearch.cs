using System.Diagnostics;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Agents.Mcts
{
    /// <summary>One decision of one player at a node: its statistics.</summary>
    internal sealed class MoveStats
    {
        public MoveStats(int players) => Total = new double[players];

        public int Visits;
        /// <summary>How many times the move was legal when its node was visited (ISMCTS: it may not be in every guess).</summary>
        public int Available;
        /// <summary>The rewards summed, per seat.</summary>
        public readonly double[] Total;
        /// <summary>The searching player's win shares summed.</summary>
        public double Wins;
    }

    internal sealed class Node
    {
        public readonly Dictionary<(int Seat, Command Move), MoveStats> Stats = new();
        /// <summary>The keys of <see cref="Stats"/> in the order they were added (for ties).</summary>
        public readonly List<(int Seat, Command Move)> Order = new();
        public readonly Dictionary<MoveKey, Node> Children = new();
    }

    /// <summary>The moves made at a node (one in a sequential tree, everyone pending in DUCT): the key of the child.</summary>
    internal readonly struct MoveKey : IEquatable<MoveKey>
    {
        private readonly (int Seat, Command Move)[] _moves;

        public MoveKey(IEnumerable<(int Seat, Command Move)> moves) => _moves = moves.ToArray();

        public bool Equals(MoveKey other) => _moves.SequenceEqual(other._moves);
        public override bool Equals(object? obj) => obj is MoveKey k && Equals(k);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var m in _moves)
                hash.Add(m);
            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// One search, for one decision of the player in seat <c>me</c>. Each iteration takes a new copy of the game (a
    /// guess at the hidden cards, unless the search may see them), walks down the tree from the root choosing moves
    /// by the tree policy, adds one new node, plays a rollout from there and adds its reward to every move on the way.
    /// The tree is keyed by moves only, so the guesses share it (single-observer ISMCTS).
    /// </summary>
    internal sealed class MctsSearch
    {
        private static readonly Command Skip = new(ActionType.Skip, null);

        private readonly MctsConfig _c;
        private readonly int _me;
        private readonly int _n;
        private readonly Game _root;
        private readonly SeatKnowledge? _known;
        private readonly Random _rng;
        private readonly HeuristicAI _policy = new();
        private readonly Node _tree = new();

        /// <param name="root">A copy of the game, which the search doesn't change.</param>
        /// <param name="known">What the player knows about the hidden cards (for <see cref="MctsInformation.Determinized"/>).</param>
        public MctsSearch(MctsConfig config, int me, Game root, SeatKnowledge? known, Random rng)
        {
            _c = config;
            _me = me;
            _n = root.Players.Count;
            _root = root;
            _known = known;
            _rng = rng;
        }

        public int Iterations { get; private set; }

        /// <summary>Searches until the budget is used up (at least one iteration) and returns the move to play.</summary>
        public Command Run(Stopwatch clock)
        {
            do
            {
                Iterate();
                Iterations++;
            }
            while (_c.Budget == MctsBudget.Iterations ? Iterations < _c.Iterations : clock.ElapsedMilliseconds < _c.TimeMs);
            return FinalMove();
        }

        private void Iterate()
        {
            Game g = _c.Information == MctsInformation.PerfectInformation
                ? _root.DeepCopy()
                : Determinization.Sample(_root, _me, _known, _rng);

            var path = new List<(Node Node, List<(int Seat, Command Move)> Moves)>();
            Node node = _tree;
            while (g.Phase != Phase.GameOver)
            {
                List<(int Seat, Command Move)> moves = _c.Tree == MctsTree.Sequential ? ChooseSequential(node, g) : ChooseDuct(node, g);
                path.Add((node, moves));
                foreach (var (seat, move) in moves)
                    g.Submit(seat, move);

                var key = new MoveKey(moves);
                if (!node.Children.TryGetValue(key, out Node? child))
                {
                    // a new node: roll out from it
                    node.Children[key] = new Node();
                    break;
                }
                node = child;
            }

            (double[] reward, double myWin) = Rollout(g);
            foreach (var (n, moves) in path)
            {
                foreach (var m in moves)
                {
                    MoveStats s = n.Stats[m];
                    s.Visits++;
                    for (int i = 0; i < _n; i++)
                        s.Total[i] += reward[i];
                    s.Wins += myWin;
                }
            }
        }

        // =========== tree policy ===========

        /// <summary>One player decides: the searching player whenever it is pending, otherwise the lowest pending seat.</summary>
        private List<(int Seat, Command Move)> ChooseSequential(Node node, Game g)
        {
            IReadOnlyList<int> pending = g.PendingPlayers();
            int seat = pending.Contains(_me) ? _me : pending[0];
            return new() { (seat, Choose(node, g, seat)) };
        }

        /// <summary>DUCT: every pending player decides, each from its own statistics.</summary>
        private List<(int Seat, Command Move)> ChooseDuct(Node node, Game g) =>
            g.PendingPlayers().Select(seat => (seat, Choose(node, g, seat))).ToList();

        /// <summary>A move not tried yet at this node (a random one), else the best by the tree policy.</summary>
        private Command Choose(Node node, Game g, int seat)
        {
            List<Command> legal = _c.Pruning == MctsPruning.RandomCard ? g.LegalMovesPruned(seat) : g.LegalMoves(seat);
            var untried = new List<Command>();
            foreach (Command m in legal)
            {
                if (node.Stats.TryGetValue((seat, m), out MoveStats? s))
                    s.Available++;
                else
                    untried.Add(m);
            }
            if (untried.Count > 0)
            {
                Command m = untried[_rng.Next(untried.Count)];
                node.Stats[(seat, m)] = new MoveStats(_n) { Available = 1 };
                node.Order.Add((seat, m));
                return m;
            }

            Command best = legal[0];
            double bestValue = double.NegativeInfinity;
            foreach (Command m in legal)
            {
                double v = TreeValue(node.Stats[(seat, m)], seat);
                if (v > bestValue)
                {
                    best = m;
                    bestValue = v;
                }
            }
            return best;
        }

        private double TreeValue(MoveStats s, int seat)
        {
            if (s.Visits == 0)
                return double.PositiveInfinity;
            double total = _c.Backup switch
            {
                MctsBackup.MaxN => s.Total[seat],
                MctsBackup.Paranoid => seat == _me ? s.Total[_me] : -s.Total[_me],
                _ => s.Total[_me],
            };
            double n = s.Visits;
            return _c.Exploration switch
            {
                MctsExploration.Smoothed => total / (n + 1) + _c.C * Math.Sqrt(Math.Log(s.Available + 1) / (n + 1)),
                MctsExploration.Diminishing => total / n + _c.C / (1 + Math.Log(1 + n)) * Math.Sqrt(Math.Log(s.Available) / n),
                _ => total / n + _c.C * Math.Sqrt(Math.Log(s.Available) / n),
            };
        }

        /// <summary>Of the searching player's moves at the root, the one the config says.</summary>
        private Command FinalMove()
        {
            Command? best = null;
            double bestValue = double.NegativeInfinity;
            foreach (var key in _tree.Order.Where(k => k.Seat == _me))
            {
                MoveStats s = _tree.Stats[key];
                double v = _c.FinalMove switch
                {
                    MctsFinalMove.HighestReward => s.Total[_me],
                    MctsFinalMove.MostWins => s.Wins,
                    _ => s.Visits,
                };
                if (best == null || v > bestValue)
                {
                    best = key.Move;
                    bestValue = v;
                }
            }
            return best ?? throw new InvalidOperationException("The search tried no move of its own player.");
        }

        // =========== rollout ===========

        /// <summary>Plays on to the end (of the game, or of the era if the config says so) and gives the rewards.</summary>
        private (double[] Reward, double MyWin) Rollout(Game g)
        {
            int era = g.Era;
            bool eraEnd = _c.RolloutEnd == MctsRolloutEnd.EraEnd;
            while (g.Phase != Phase.GameOver && !(eraEnd && g.Era != era))
            {
                // everyone chooses from the same state, as in a real game
                var moves = g.PendingPlayers().Select(seat => (seat, RolloutMove(g, seat))).ToList();
                foreach (var (seat, move) in moves)
                    g.Submit(seat, move);
            }
            return Score(g);
        }

        private Command RolloutMove(Game g, int seat)
        {
            PlayerState p = g.Players[seat];
            bool random = _c.Rollout == MctsRollout.Random ||
                          _c.Rollout == MctsRollout.EpsilonHeuristic && _rng.NextDouble() < _c.Epsilon;
            return random ? RandomMove(g, p) : _policy.Choose(p, g.Phase, g.Era, g.DiscardedCards);
        }

        /// <summary>A random action the player can take, then a random card for it (as <see cref="RandomChoiceAI"/>).</summary>
        private Command RandomMove(Game g, PlayerState p)
        {
            if (g.Phase == Phase.DiscardBuild)
            {
                List<Card> fromPile = p.GetPlayableCards(g.DiscardedCards, playAllFree: true);
                return fromPile.Count == 0 ? Skip : new Command(ActionType.Build, fromPile[_rng.Next(fromPile.Count)]);
            }
            List<Card> hand = p.HandCards;
            if (hand.Count == 0)
                return Skip;

            List<Card> playable = p.GetPlayableCards(hand);
            List<Card>? free = g.Phase == Phase.ChooseMoves && p.FreeCardOnce ? p.GetPlayableCards(hand, playAllFree: true) : null;
            var options = new List<ActionType>(4);
            if (playable.Count > 0)
                options.Add(ActionType.Build);
            if (free is { Count: > 0 })
                options.Add(ActionType.BuildFreeOnce);
            if (p.CanBuildWonder())
                options.Add(ActionType.BuildWonder);
            options.Add(ActionType.Discard);

            ActionType action = options[_rng.Next(options.Count)];
            List<Card> cards = action switch
            {
                ActionType.Build => playable,
                ActionType.BuildFreeOnce => free!,
                _ => hand,
            };
            return new Command(action, cards[_rng.Next(cards.Count)]);
        }

        /// <summary>Each seat's reward and the searching player's win share, from the scores now.</summary>
        private (double[] Reward, double MyWin) Score(Game g)
        {
            // the game is a throwaway copy at the end; mid-game (era-end cutoff) score copies so it can't change
            List<PlayerState> players = g.Phase == Phase.GameOver ? g.Players : PlayerState.CloneTable(g.Players);
            foreach (PlayerState p in players)
            {
                p.CopyGuild();
                p.CalculateScore();
            }
            List<PlayerStanding> standings = GameResult.Rank(players);

            var reward = new double[_n];
            for (int s = 0; s < _n; s++)
            {
                int vp = players[s].VictoryPoints;
                reward[s] = _c.Reward switch
                {
                    MctsReward.VpMargin => vp - players.Where(o => o.Id != s).Max(o => o.VictoryPoints),
                    MctsReward.RawVp => vp,
                    _ => standings[s].WinShare,
                };
            }
            return (reward, standings[_me].WinShare);
        }
    }
}
