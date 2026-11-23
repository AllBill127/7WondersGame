using _7Wonders.Agents.Matches.Records;

namespace _7Wonders.Agents.Matches;

/// <summary>How much of a match its <see cref="MatchRecord"/> keeps.</summary>
public enum MatchDetail
{
    /// <summary>Only the results: no entry is built, so a match costs no more than the game itself.</summary>
    ResultsOnly,

    /// <summary>Every entry: the setup, what each seat was shown and chose, what the rules did, the scores.</summary>
    Full,
}
