using System.Globalization;

namespace _7WondersGame.Cli.Agents;

/// <summary>
/// A setting given as a range <c>a..b</c>: the run plays every deal once for each of <c>Steps</c> evenly spaced values
/// from a to b (both included). Only one setting in a run may be a range.
/// </summary>
public sealed record Sweep(int Seat, string Setting, double From, double To, int Steps)
{
    // same arithmetic as the old C sweep (minC + k * ((maxC - minC) / intervals)), so its values are reproduced exactly
    public double ValueAt(int step) => From + step * ((To - From) / (Steps - 1));

    public static bool TryParseRange(string value, out double from, out double to)
    {
        from = to = 0;
        int dots = value.IndexOf("..", StringComparison.Ordinal);
        return dots > 0 &&
               double.TryParse(value[..dots], NumberStyles.Float, CultureInfo.InvariantCulture, out from) &&
               double.TryParse(value[(dots + 2)..], NumberStyles.Float, CultureInfo.InvariantCulture, out to);
    }
}
