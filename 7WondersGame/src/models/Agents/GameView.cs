using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Agents
{
    /// <summary>
    /// What an agent sees when it must decide (E5): a copy of the table taken when it is asked, so changing it
    /// changes nothing in the game. Every agent asked in the same step gets a view of the same state, so none sees
    /// another's choice (M2).
    /// The view still shows everything, including the other hands (<see cref="Players"/>, <see cref="CopyGame"/>). An
    /// agent that plays fair uses only the public state, its own hand and <see cref="Known"/> (M1); the MCTS agent
    /// does this through <see cref="Determinization"/>.
    /// </summary>
    public sealed class GameView
    {
        private readonly Game _game;

        public GameView(Game game, int seat)
        {
            _game = game;
            Seat = seat;
            Phase = game.Phase;
            Era = game.Era;
            Turn = game.Turn;
            Players = PlayerState.CloneTable(game.Players);
            DiscardPile = game.DiscardedCards.ToList();
            Me = Players[seat];
            PlayableCards = Phase == Phase.DiscardBuild
                // the discard-pile build is free, so only the no-duplicates rule limits it
                ? Me.GetPlayableCards(DiscardPile, playAllFree: true)
                : Me.GetPlayableCards(Me.HandCards);
            CanBuildWonder = Me.CanBuildWonder();
            Known = game.KnowledgeOf(seat);
        }

        /// <summary>The seat deciding.</summary>
        public int Seat { get; }
        public Phase Phase { get; }
        public int Era { get; }
        public int Turn { get; }

        /// <summary>Every player, by seat (a copy).</summary>
        public IReadOnlyList<PlayerState> Players { get; }

        /// <summary>The deciding player (a copy).</summary>
        public PlayerState Me { get; }

        public List<Card> DiscardPile { get; }

        /// <summary>
        /// The cards the player may build now: hand cards it can pay for or chain, or in
        /// <see cref="Phase.DiscardBuild"/> the discard-pile cards it hasn't built yet.
        /// </summary>
        public List<Card> PlayableCards { get; }

        /// <summary>Whether the player can pay for its next wonder stage.</summary>
        public bool CanBuildWonder { get; }

        /// <summary>This seat's random stream: every random choice of the agent comes from it (B1).</summary>
        public Random Random => _game.AgentRandom(Seat);

        /// <summary>Every move the rules allow the seat now (see <see cref="Game.LegalMoves"/>).</summary>
        public List<Command> LegalMoves() => _game.LegalMoves(Seat);

        /// <summary>
        /// What the seat knows about the cards it can't see (taken when the view was made); null if the game doesn't
        /// keep track (a search copy).
        /// </summary>
        public SeatKnowledge? Known { get; }

        /// <summary>
        /// A copy of the whole game to search in. It holds everything, hidden cards included: a fair search
        /// re-deals what the seat can't know (<see cref="Determinization.Sample"/>) before using it.
        /// </summary>
        public Game CopyGame() => _game.DeepCopy();
    }
}
