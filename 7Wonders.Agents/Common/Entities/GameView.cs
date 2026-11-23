using _7Wonders.Agents.HiddenInformation;
using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Games.Enums;
using _7Wonders.Engine.Games.Knowledge;
using _7Wonders.Engine.Moves.Dtos;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;

namespace _7Wonders.Agents.Common.Entities;

/// <summary>
/// What an agent sees when it must decide: a copy of the table taken when it is asked, so changing it changes
/// nothing in the game. Every agent asked in the same step gets a view of the same state, so none sees another's
/// choice.
/// The view still shows everything, including the other hands (<see cref="Players"/>, <see cref="CopyGame"/>). An
/// agent that plays fair uses only the public state, its own hand and <see cref="Knowledge"/>; the MCTS agent does
/// this through <see cref="Determinization"/>.
/// </summary>
public sealed class GameView
{
    private readonly Game _game;

    /// <summary>The view of <paramref name="game"/> as it is now, for <paramref name="seat"/>.</summary>
    public GameView(Game game, int seat)
    {
        _game = game;
        Seat = seat;
        Phase = game.Phase;
        Era = game.Era;
        Turn = game.Turn;
        Players = PlayerState.CloneTable(game.Players);
        DiscardPile = new List<Card>(game.DiscardPile);
        DecidingPlayer = Players[seat];
        BuildableCards = BuildableCardsNow();
        CanBuildWonder = BuildRules.CanBuildWonderStage(DecidingPlayer);
        Knowledge = game.KnowledgeOf(seat);
    }

    /// <summary>The seat deciding.</summary>
    public int Seat { get; }

    /// <summary>What the seat must decide now.</summary>
    public Phase Phase { get; }

    /// <summary>The era being played, 1 to 3.</summary>
    public int Era { get; }

    /// <summary>The turn, 0 to 20 (see <see cref="Game.Turn"/>).</summary>
    public int Turn { get; }

    /// <summary>Every player, by seat (a copy).</summary>
    public IReadOnlyList<PlayerState> Players { get; }

    /// <summary>The deciding player (a copy).</summary>
    public PlayerState DecidingPlayer { get; }

    /// <summary>The discarded cards (a copy of the list).</summary>
    public List<Card> DiscardPile { get; }

    /// <summary>
    /// The cards the player may build now: hand cards it can pay for or chain, or in
    /// <see cref="Phase.DiscardPileBuild"/> the discard-pile cards it hasn't built yet.
    /// </summary>
    public List<Card> BuildableCards { get; }

    /// <summary>Whether the player can pay for its next wonder stage.</summary>
    public bool CanBuildWonder { get; }

    /// <summary>This seat's random stream: every random choice of the agent comes from it.</summary>
    public Random Random
    {
        get
        {
            return _game.SeatRandom(Seat);
        }
    }

    /// <summary>
    /// What the seat knows about the cards it can't see (taken when the view was made); null if the game doesn't
    /// keep track (a search copy).
    /// </summary>
    public SeatKnowledge? Knowledge { get; }

    /// <summary>Every move the rules allow the seat now (see <see cref="Game.LegalMoves"/>).</summary>
    public List<Move> LegalMoves()
    {
        return _game.LegalMoves(Seat);
    }

    /// <summary>
    /// A copy of the whole game to search in. It holds everything, hidden cards included: a fair search
    /// re-deals what the seat can't know (<see cref="Determinization.Sample"/>) before using it.
    /// </summary>
    public Game CopyGame()
    {
        return _game.DeepCopy();
    }

    private List<Card> BuildableCardsNow()
    {
        if (Phase == Phase.DiscardPileBuild)
        {
            // the discard-pile build is free, so only the no-duplicates rule limits it
            return BuildRules.BuildableCards(DecidingPlayer, DiscardPile, everyCardFree: true);
        }
        return BuildRules.BuildableCards(DecidingPlayer, DecidingPlayer.Hand);
    }
}
