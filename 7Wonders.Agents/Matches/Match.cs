using _7Wonders.Agents.Common.Entities;
using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Agents.Common.Settings;
using _7Wonders.Agents.Matches.Records;
using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Games.Enums;
using _7Wonders.Engine.Games.Events;
using _7Wonders.Engine.Games.Interfaces;
using _7Wonders.Engine.Moves.Dtos;
using _7Wonders.Engine.Moves.Exceptions;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;

namespace _7Wonders.Agents.Matches;

/// <summary>
/// Agents seated at a set-up <see cref="Game"/>: <see cref="Play"/> asks each seat's agent for its decisions until
/// the game is over, then scores it. With <see cref="MatchDetail.Full"/> the match records what the agents were
/// shown and chose and, as the game's event listener, what the rules did, all in one list in the order it happened.
/// It passes each entry to its listener as it is added.
/// </summary>
public class Match : IGameEventListener
{
    private readonly List<MatchEntry> _entries = new();
    private readonly IMatchListener? _listener;

    /// <summary>
    /// Seats <paramref name="agents"/> at <paramref name="game"/>, one per seat in seat order. The game must be set
    /// up already (<see cref="Game.SetUpRandomGame"/> or <see cref="Game.SetUpGameFromDataset"/>). The record keeps
    /// only the results.
    /// </summary>
    public Match(Game game, IReadOnlyList<IAgent> agents)
        : this(game, agents, MatchDetail.ResultsOnly)
    {
    }

    /// <summary>
    /// Like <see cref="Match(Game, IReadOnlyList{IAgent})"/>, with a record that keeps <paramref name="detail"/>.
    /// </summary>
    public Match(Game game, IReadOnlyList<IAgent> agents, MatchDetail detail)
    {
        if (agents.Count != game.Players.Count)
        {
            throw new InvalidOperationException($"{game.Players.Count} players but {agents.Count} agents.");
        }
        Game = game;
        Agents = agents;
        Detail = detail;
    }

    /// <summary>
    /// Like <see cref="Match(Game, IReadOnlyList{IAgent})"/>, with a full record whose entries also go to
    /// <paramref name="listener"/> as they are added.
    /// </summary>
    public Match(Game game, IReadOnlyList<IAgent> agents, IMatchListener listener)
        : this(game, agents, MatchDetail.Full)
    {
        _listener = listener;
    }

    /// <summary>The game being played.</summary>
    public Game Game { get; }

    /// <summary>Who decides each seat's moves, by seat.</summary>
    public IReadOnlyList<IAgent> Agents { get; }

    /// <summary>How much of the match its record keeps.</summary>
    public MatchDetail Detail { get; }

    private bool KeepsEntries
    {
        get
        {
            return Detail == MatchDetail.Full;
        }
    }

    /// <summary>
    /// Plays the game to the end and scores it.
    /// </summary>
    /// <returns>Each seat's final result in seat order, and the entries if the match keeps them.</returns>
    public MatchRecord Play()
    {
        if (KeepsEntries)
        {
            Game.EventListener = this;
        }
        if (Game.Turn == 0 && KeepsEntries)
        {
            AddSetUp();
        }

        IReadOnlyList<int> pending = Game.PendingPlayers();
        while (pending.Count > 0)
        {
            AskAndSubmit(pending);
            pending = Game.PendingPlayers();
        }

        Game.ScoreAllPlayers();
        if (KeepsEntries)
        {
            AddFinalScores();
        }
        List<SeatResult> results = GameResult.ForPlayers(Game.Players, Agents);
        return new MatchRecord(_entries, results);
    }

    /// <summary>
    /// Asks every pending seat's agent for its decision, then submits them all. Everyone chooses from a view of the
    /// same state before any choice is submitted, so no agent sees another's choice.
    /// </summary>
    private void AskAndSubmit(IReadOnlyList<int> pending)
    {
        List<GameView> views = new(pending.Count);
        foreach (int seat in pending)
        {
            views.Add(new GameView(Game, seat));
        }
        if (KeepsEntries)
        {
            AddShown(views);
        }

        List<Move> choices = new(views.Count);
        foreach (GameView view in views)
        {
            choices.Add(Agents[view.Seat].Choose(view));
        }
        if (KeepsEntries)
        {
            for (int choiceIndex = 0; choiceIndex < views.Count; choiceIndex++)
            {
                AddChoice(views[choiceIndex].Seat, choices[choiceIndex]);
            }
        }

        for (int choiceIndex = 0; choiceIndex < views.Count; choiceIndex++)
        {
            Submit(views[choiceIndex].Seat, choices[choiceIndex]);
        }
    }

    /// <summary>
    /// Submits the seat's choice. A move the rules don't allow is reported with the name of the agent that chose it.
    /// </summary>
    private void Submit(int seat, Move move)
    {
        try
        {
            Game.Submit(seat, move);
        }
        catch (MoveNotAllowedException exception)
        {
            string agentName = Agents[exception.Seat].Name;
            throw new InvalidOperationException($"Seat {exception.Seat} ({agentName}) {exception.Failure}", exception);
        }
    }

    // ================= Record entries (MatchDetail.Full) =================
    // Read-only: these only read state that is already computed, and copy the lists the game changes later.

    private void Add(MatchEntry entry)
    {
        _entries.Add(entry);
        if (_listener != null)
        {
            _listener.EntryAdded(entry);
        }
    }

    /// <summary>The game's rules did something: it goes into the record among the match's own entries.</summary>
    void IGameEventListener.EventRaised(GameEvent gameEvent)
    {
        Add(new GameEventEntry(gameEvent));
    }

    private void AddSetUp()
    {
        List<SeatSetUp> seats = new(Game.Players.Count);
        foreach (PlayerState player in Game.Players)
        {
            IAgent agent = Agents[player.Seat];
            seats.Add(new SeatSetUp(player.Seat, agent.Name, agent.ConfigurationJson(), player.Wonder.Id));
        }
        List<IReadOnlyList<Card>> decks = new(Game.Decks.Length);
        foreach (List<Card> deck in Game.Decks)
        {
            decks.Add(new List<Card>(deck));
        }
        Add(new MatchSetUp(Game.Seed, seats, decks));
    }

    private void AddShown(IReadOnlyList<GameView> views)
    {
        Add(new PhaseStarted(Game.Phase, Game.Era, Game.Turn));
        foreach (GameView view in views)
        {
            PlayerState player = Game.Players[view.Seat];
            // the discard pile only matters to a build from it
            IReadOnlyList<Card> discardPile = Array.Empty<Card>();
            if (Game.Phase == Phase.DiscardPileBuild)
            {
                discardPile = new List<Card>(Game.DiscardPile);
            }
            // what the agent was shown: worked out for its view, so recording it changes nothing
            Add(new SeatShown(Game.Phase,
                player.Seat,
                player.Resources[Resource.Coins],
                player.Resources[Resource.Shields],
                player.Wonder.Id,
                player.Wonder.BuiltStageCount,
                new List<Card>(player.Hand),
                discardPile,
                view.BuildableCards,
                view.CanBuildWonder));
        }
    }

    private void AddChoice(int seat, Move move)
    {
        // e.g. a search's top moves; the agent made the note when it chose, so reading it changes nothing
        IAgent agent = Agents[seat];
        Add(new SeatChose(Game.Phase, seat, agent.Name, move, agent.LastChoiceNote));
    }

    private void AddFinalScores()
    {
        List<SeatScore> seats = new(Game.Players.Count);
        foreach (PlayerState player in Game.Players)
        {
            seats.Add(new SeatScore(player.Seat,
                Agents[player.Seat].Name,
                player.VictoryPoints,
                Scoring.CalculateMilitaryScore(player),
                Scoring.CalculateMoneyScore(player),
                Scoring.CalculateWonderScore(player),
                Scoring.CalculateCivilianScore(player),
                Scoring.CalculateCommercialScore(player),
                Scoring.CalculateGuildScore(player),
                Scoring.CalculateScientificScore(player),
                player.Resources[Resource.Coins]));
        }
        Add(new FinalScores(seats));
    }
}
