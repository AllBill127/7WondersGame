namespace _7Wonders.Cli.ResultsAnalysis;

/// <summary>
/// Where a result row comes in the order the runs play their games: by run, then game, then seat.
/// </summary>
public sealed class PlayOrder : IComparable<PlayOrder>
{
    /// <summary>The place of <paramref name="row"/>.</summary>
    public PlayOrder(ResultRow row)
    {
        RunId = row.RunId;
        GameIndex = row.GameIndex;
        Seat = row.Seat;
    }

    /// <summary>The run the row is from.</summary>
    public string RunId { get; }

    /// <summary>The game's number in its run.</summary>
    public long GameIndex { get; }

    /// <summary>The row's seat.</summary>
    public int Seat { get; }

    /// <summary>Compares by run, then game, then seat.</summary>
    public int CompareTo(PlayOrder? other)
    {
        if (other is null)
        {
            return 1;
        }
        int byRun = Comparer<string>.Default.Compare(RunId, other.RunId);
        if (byRun != 0)
        {
            return byRun;
        }
        int byGame = GameIndex.CompareTo(other.GameIndex);
        if (byGame != 0)
        {
            return byGame;
        }
        return Seat.CompareTo(other.Seat);
    }
}
