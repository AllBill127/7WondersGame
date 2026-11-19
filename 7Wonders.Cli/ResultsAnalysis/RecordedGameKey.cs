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
    public override bool Equals(object? obj)
    {
        return Equals(obj as RecordedGameKey);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(RunId, GameIndex);
    }
}
