namespace _7Wonders.Cli.Experiments;

/// <summary>Which deal, seating and sweep value game <see cref="Index"/> of a run plays.</summary>
public readonly struct GameSlot
{
    /// <summary>
    /// Game <paramref name="index"/>: deal <paramref name="deal"/> in seating <paramref name="rotation"/>, at sweep
    /// step <paramref name="step"/>.
    /// </summary>
    public GameSlot(int index, int step, int deal, int rotation, double? value)
    {
        Index = index;
        Step = step;
        Deal = deal;
        Rotation = rotation;
        Value = value;
    }

    /// <summary>The game's number in the run.</summary>
    public int Index { get; }

    /// <summary>Which value of the range setting the game uses (0 without one).</summary>
    public int Step { get; }

    /// <summary>Which deal the game plays.</summary>
    public int Deal { get; }

    /// <summary>The line-up moved this many seats on (0 = as listed).</summary>
    public int Rotation { get; }

    /// <summary>The value of the range setting, if there is one.</summary>
    public double? Value { get; }
}
