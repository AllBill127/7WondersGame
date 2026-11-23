namespace _7Wonders.Cli.ResultsAnalysis;

/// <summary>Which game a result row is from: its run and its number in the run. Equal when both are.</summary>
internal sealed class RecordedGameKey : IEquatable<RecordedGameKey>
{
    /// <summary>The game of <paramref name="row"/>.</summary>
    public RecordedGameKey(ResultRow row)
    {
        RunId = row.RunId;
        GameIndex = row.GameIndex;
    }

    /// <summary>The run the game is from.</summary>
    public string RunId { get; }

    /// <summary>The game's number in its run.</summary>
    public long GameIndex { get; }

    /// <summary>Whether both are the same game of the same run.</summary>
    public bool Equals(RecordedGameKey? other)
    {
        if (other is null)
        {
            return false;
        }
        return RunId == other.RunId && GameIndex == other.GameIndex;
    }

    /// <inheritdoc/>
    public override bool Equals(object? otherObject)
    {
        return Equals(otherObject as RecordedGameKey);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Equal keys must give the same hash: HashCode.Combine mixes the two fields Equals compares into one int.
        // The analysis groups rows by this key in a dictionary.
        return HashCode.Combine(RunId, GameIndex);
    }
}
