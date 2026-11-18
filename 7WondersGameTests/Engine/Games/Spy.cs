using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGameTests.Engine.Games;

/// <summary>
/// Plays randomly and records whether, when asked, any move of this step was already in the game, or its view
/// was the live game rather than a copy.
/// </summary>
internal sealed class Spy : IAgent
{
    private readonly RandomChoiceAI _random = new();

    /// <summary>The live game the spy plays in, to compare its views with.</summary>
    public Game? Game { get; set; }

    /// <summary>Whether another player's move of the same step was already submitted when the spy was asked.</summary>
    public bool SawAnotherChoice { get; private set; }

    /// <summary>Whether a view held the live game's players or discard pile instead of copies.</summary>
    public bool SawLiveState { get; private set; }

    /// <summary>Records what the view shows, then chooses a random move.</summary>
    public Move Choose(GameView view)
    {
        Game game = Game!;
        if (game.SomeMovesSubmitted)
        {
            SawAnotherChoice = true;
        }

        if (ViewSharesLiveState(view, game))
        {
            SawLiveState = true;
        }

        return _random.Choose(view);
    }

    private static bool ViewSharesLiveState(GameView view, Game game)
    {
        foreach (PlayerState player in view.Players)
        {
            if (game.Players.Contains(player))
            {
                return true;
            }
        }

        return ReferenceEquals(view.DiscardPile, game.DiscardPile);
    }
}
