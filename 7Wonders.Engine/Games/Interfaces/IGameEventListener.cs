using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Games.Events;

namespace _7Wonders.Engine.Games.Interfaces;

/// <summary>
/// Follows what the rules do in a game (see <see cref="Game.EventListener"/>): it gets each event as it happens.
/// </summary>
public interface IGameEventListener
{
    /// <summary>
    /// <paramref name="gameEvent"/> has just happened. It must not change the game: it only reads the event.
    /// </summary>
    void EventRaised(GameEvent gameEvent);
}
