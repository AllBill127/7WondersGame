using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Dtos;

namespace _7WondersGameTests.Engine.Games;

/// <summary>
/// Plays randomly and records whether, when asked, any move of this step was already in the game, or its view
/// was the live game rather than a copy.
/// </summary>
internal sealed class Spy : IAgent
{
    private readonly RandomChoiceAI _random = new();
    public Game? Game;
    public bool SawAnotherChoice;
    public bool SawLiveState;

    public Command Choose(GameView view)
    {
        SawAnotherChoice |= Game!.MovesWaiting;
        SawLiveState |= view.Players.Any(p => Game.Players.Contains(p)) || ReferenceEquals(view.DiscardPile, Game.DiscardedCards);
        return _random.Choose(view);
    }
}
