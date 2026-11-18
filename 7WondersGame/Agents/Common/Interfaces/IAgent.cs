using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Games.Enums;
using _7WondersGame.Engine.Moves.Dtos;

namespace _7WondersGame.Agents.Common.Interfaces;

/// <summary>
/// Decides one seat's moves. The game asks it whenever the seat must decide (see
/// <see cref="Game.PendingPlayers"/>); it only sees a <see cref="GameView"/>, a copy, so it can't change the game.
/// </summary>
public interface IAgent
{
    /// <summary>The name in results and traces, and on the command line.</summary>
    string Name
    {
        get
        {
            return GetType().Name;
        }
    }

    /// <summary>
    /// The move for the view's phase: in <see cref="Phase.ChooseMoves"/> and <see cref="Phase.SeventhCard"/> a
    /// move with a hand card; in <see cref="Phase.DiscardPileBuild"/> a Build of a discard-pile card, or Skip.
    /// </summary>
    Move Choose(GameView view);

    /// <summary>
    /// What the agent measured over its game (e.g. search time and iterations), as JSON for the results'
    /// agentStatistics column. An agent is made for one game. "{}" for none.
    /// </summary>
    string StatisticsJson()
    {
        return "{}";
    }

    /// <summary>
    /// Why it made its last choice, in a line (e.g. a search's top moves), for the game trace; null for nothing.
    /// Read by <see cref="Game"/> after <see cref="Choose"/> only when tracing.
    /// </summary>
    string? LastChoiceNote
    {
        get
        {
            return null;
        }
    }

    /// <summary>
    /// True if its choices depend on the clock (a time budget): then they depend on the machine and on what else
    /// runs on it, so the run warns about --parallel.
    /// </summary>
    bool UsesClock
    {
        get
        {
            return false;
        }
    }
}
