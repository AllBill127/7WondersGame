using _7Wonders.Agents.Common.Entities;
using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Agents.Common.Settings;
using _7Wonders.Agents.Matches;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Cards.Services;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Games.Enums;
using _7Wonders.Engine.Moves.Dtos;
using _7Wonders.Engine.Moves.Exceptions;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;
using Serilog;
using Serilog.Events;

namespace _7Wonders.Agents.Matches;

/// <summary>
/// Agents seated at a set-up <see cref="Game"/>: <see cref="Play"/> asks each seat's agent for its decisions until
/// the game is over, then scores it. The match traces what the agents were shown and chose; the game traces what
/// the rules did.
/// </summary>
public class Match
{
    /// <summary>
    /// Seats <paramref name="agents"/> at <paramref name="game"/>, one per seat in seat order. The game must be set
    /// up already (<see cref="Game.SetUpRandomGame"/> or <see cref="Game.SetUpGameFromDataset"/>).
    /// </summary>
    public Match(Game game, IReadOnlyList<IAgent> agents)
    {
        if (agents.Count != game.Players.Count)
        {
            throw new InvalidOperationException($"{game.Players.Count} players but {agents.Count} agents.");
        }
        Game = game;
        Agents = agents;
    }

    /// <summary>The game being played.</summary>
    public Game Game { get; }

    /// <summary>Who decides each seat's moves, by seat.</summary>
    public IReadOnlyList<IAgent> Agents { get; }

    private ILogger Trace
    {
        get
        {
            return Game.Trace;
        }
    }

    private bool Tracing
    {
        get
        {
            return Trace.IsEnabled(LogEventLevel.Debug);
        }
    }

    /// <summary>
    /// Plays the game to the end and scores it.
    /// </summary>
    /// <returns>Each seat's final result, in seat order.</returns>
    public List<SeatResult> Play()
    {
        if (Game.Turn == 0 && Tracing)
        {
            TraceSetup();
        }

        IReadOnlyList<int> pending = Game.PendingPlayers();
        while (pending.Count > 0)
        {
            AskAndSubmit(pending);
            pending = Game.PendingPlayers();
        }

        Game.ScoreAllPlayers();
        if (Tracing)
        {
            TraceScores();
        }
        return GameResult.ForPlayers(Game.Players, Agents);
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
        if (Tracing)
        {
            TracePhaseStart(views);
        }

        List<Move> choices = new(views.Count);
        foreach (GameView view in views)
        {
            choices.Add(Agents[view.Seat].Choose(view));
        }
        if (Tracing && Game.Phase != Phase.DiscardPileBuild)
        {
            for (int choiceIndex = 0; choiceIndex < views.Count; choiceIndex++)
            {
                TraceChoice(views[choiceIndex].Seat, choices[choiceIndex]);
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

    // ================= Trace (Debug / Verbose) =================
    // Read-only: these only read state that is already computed.

    private void TraceSetup()
    {
        Trace.Debug("Game seed {Seed} (0x{SeedHex}), {Count} players",
            Game.Seed,
            Game.Seed.ToString("x16"),
            Game.Players.Count);
        foreach (PlayerState player in Game.Players)
        {
            IAgent agent = Agents[player.Seat];
            Trace.Debug("Seat {Seat}: {Agent} {Configuration} on {Wonder}",
                player.Seat,
                agent.Name,
                agent.ConfigurationJson(),
                player.Wonder.Id);
        }
        for (int era = 0; era < Game.Decks.Length; era++)
        {
            Trace.Verbose("Era {Era} deck: {Cards}", era + 1, CardNames.Join(Game.Decks[era]));
        }
        Trace.Debug("=== Era 1 ===");
    }

    private void TracePhaseStart(IReadOnlyList<GameView> views)
    {
        if (Game.Phase == Phase.ChooseMoves)
        {
            TraceTurnStart(views);
            return;
        }
        foreach (GameView view in views)
        {
            PlayerState player = Game.Players[view.Seat];
            if (Game.Phase == Phase.SeventhCard)
            {
                Trace.Debug("Seat {Seat} plays the last card of the hand (Babylon B): {Hand}",
                    player.Seat,
                    CardNames.Join(player.Hand));
            }
            else
            {
                Trace.Debug("Seat {Seat} may build from the discard pile for free (Halikarnassos)", player.Seat);
                Trace.Verbose("Discard pile: {Cards}", CardNames.Join(Game.DiscardPile));
            }
            TracePlayable(view);
        }
    }

    private void TraceTurnStart(IReadOnlyList<GameView> views)
    {
        Trace.Debug("--- Era {Era}, turn {Turn} ---", Game.Era, Game.Turn);
        foreach (GameView view in views)
        {
            PlayerState player = Game.Players[view.Seat];
            Trace.Debug("Seat {Seat}: {Coins} coins, {Shields} shields, {Wonder} stage {Stage}; hand: {Hand}",
                player.Seat,
                player.Resources[Resource.Coins],
                player.Resources[Resource.Shields],
                player.Wonder.Id,
                player.Wonder.BuiltStageCount,
                CardNames.Join(player.Hand));
            TracePlayable(view);
        }
    }

    // what the agent was shown (worked out for its view, so tracing it changes nothing)
    private void TracePlayable(GameView view)
    {
        Trace.Verbose("Seat {Seat} can build: {Playable}; can build a wonder stage: {CanBuildWonder}",
            view.Seat,
            CardNames.Join(view.BuildableCards),
            view.CanBuildWonder);
    }

    private void TraceChoice(int seat, Move move)
    {
        string cardName = "-";
        if (move.Card != null)
        {
            cardName = move.Card.Name;
        }
        Trace.Debug("Seat {Seat} chooses {Action} {Card}", seat, move.Action, cardName);

        // e.g. a search's top moves; the agent made the note when it chose, so reading it changes nothing
        IAgent agent = Agents[seat];
        string? note = agent.LastChoiceNote;
        if (note != null)
        {
            Trace.Debug("Seat {Seat} {Agent}: {Note}", seat, agent.Name, note);
        }
    }

    private void TraceScores()
    {
        foreach (PlayerState player in Game.Players)
        {
            Trace.Debug("Seat {Seat} {Agent}: {Total} VP = military {Military} + coins {Money} + wonder {Wonder} + " +
                        "civilian {Civilian} + commercial {Commercial} + guilds {Guilds} + science {Science}; " +
                        "{Coins} coins left",
                player.Seat,
                Agents[player.Seat].Name,
                player.VictoryPoints,
                Scoring.CalculateMilitaryScore(player),
                Scoring.CalculateMoneyScore(player),
                Scoring.CalculateWonderScore(player),
                Scoring.CalculateCivilianScore(player),
                Scoring.CalculateCommercialScore(player),
                Scoring.CalculateGuildScore(player),
                Scoring.CalculateScientificScore(player),
                player.Resources[Resource.Coins]);
        }
    }
}
