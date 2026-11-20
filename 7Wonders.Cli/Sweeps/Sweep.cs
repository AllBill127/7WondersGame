using System.Globalization;

namespace _7Wonders.Cli.Sweeps;

/// <summary>
/// A setting given as a range <c>a..b</c>: the run plays every deal once for each of <c>Steps</c> evenly spaced values
/// from a to b (both included). Only one setting in a run may be a range.
/// </summary>
internal sealed class Sweep : IEquatable<Sweep>
{
    private const string RangeDots = "..";

    /// <summary>The range setting <paramref name="setting"/> of seat <paramref name="seat"/>.</summary>
    public Sweep(int seat, string setting, double from, double to, int steps)
    {
        Seat = seat;
        Setting = setting;
        From = from;
        To = to;
        Steps = steps;
    }

    /// <summary>The seat (in the line-up as listed) whose setting is a range.</summary>
    public int Seat { get; }

    /// <summary>The setting's name as written.</summary>
    public string Setting { get; }

    /// <summary>The first value.</summary>
    public double From { get; }

    /// <summary>The last value.</summary>
    public double To { get; }

    /// <summary>How many values, both ends included.</summary>
    public int Steps { get; }

    /// <summary>Whether both are the same range of the same setting (e.g. from a file and a command line).</summary>
    public bool Equals(Sweep? other)
    {
        if (other is null)
        {
            return false;
        }
        return Seat == other.Seat
            && Setting == other.Setting
            && From.Equals(other.From)
            && To.Equals(other.To)
            && Steps == other.Steps;
    }

    /// <inheritdoc/>
    public override bool Equals(object? otherObject)
    {
        return Equals(otherObject as Sweep);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Seat, Setting, From, To, Steps);
    }

    /// <summary>
    /// The value at <paramref name="step"/>: <see cref="From"/> at step 0, <see cref="To"/> at the last.
    /// </summary>
    public double ValueAt(int step)
    {
        // same arithmetic as the old C sweep (minC + k * ((maxC - minC) / intervals)), so its values are reproduced
        // exactly
        return From + step * ((To - From) / (Steps - 1));
    }

    /// <summary>Whether <paramref name="value"/> is a range <c>a..b</c> of two numbers, and its ends.</summary>
    public static bool TryParseRange(string value, out double from, out double to)
    {
        from = 0;
        to = 0;
        int dots = value.IndexOf(RangeDots, StringComparison.Ordinal);
        if (dots <= 0)
        {
            return false;
        }

        string fromText = value.Substring(0, dots);
        int toStart = dots + RangeDots.Length;
        string toText = value.Substring(toStart);
        return double.TryParse(fromText, NumberStyles.Float, CultureInfo.InvariantCulture, out from)
            && double.TryParse(toText, NumberStyles.Float, CultureInfo.InvariantCulture, out to);
    }
}
