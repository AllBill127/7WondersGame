using _7Wonders.Agents.Matches.Records;

namespace _7Wonders.Agents.Matches;

/// <summary>
/// Follows a match as it is played (e.g. a live trace, or a frontend showing the game): it gets each entry of the
/// <see cref="MatchRecord"/> as soon as it is added.
/// </summary>
public interface IMatchListener
{
    /// <summary>
    /// <paramref name="entry"/> has just been added to the record. It must not change the game: it only reads the
    /// entry.
    /// </summary>
    void EntryAdded(MatchEntry entry);
}
