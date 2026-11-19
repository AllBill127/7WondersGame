using _7Wonders.Engine.Games.Events;

namespace _7Wonders.Agents.Matches.Records;

/// <summary>Something the rules did (see <see cref="GameEvent"/>), in its place among the match's entries.</summary>
public sealed class GameEventEntry : MatchEntry
{
    /// <summary>The entry for <paramref name="gameEvent"/>.</summary>
    public GameEventEntry(GameEvent gameEvent)
    {
        Event = gameEvent;
    }

    /// <summary>What the rules did.</summary>
    public GameEvent Event { get; }
}
