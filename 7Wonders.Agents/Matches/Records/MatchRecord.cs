namespace _7Wonders.Agents.Matches.Records;

/// <summary>
/// What a played match produced: each seat's result and, for a match that keeps details, everything that happened
/// in the order it happened.
/// </summary>
public sealed class MatchRecord
{
    /// <summary>A record of these entries and results.</summary>
    public MatchRecord(IReadOnlyList<MatchEntry> entries, List<SeatResult> results)
    {
        Entries = entries;
        Results = results;
    }

    /// <summary>
    /// The entries in the order they happened; empty for a match with <see cref="MatchDetail.ResultsOnly"/>.
    /// </summary>
    public IReadOnlyList<MatchEntry> Entries { get; }

    /// <summary>Each seat's final result, in seat order.</summary>
    public List<SeatResult> Results { get; }
}
