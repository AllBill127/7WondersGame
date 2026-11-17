using System.Globalization;
using _7WondersGame.Cli.Experiments;
using _7WondersGame.Cli.Results;

namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>
/// Where a sweep run's swept agent sits, from its manifest: listed in position <paramref name="Listed"/>, each deal
/// played <paramref name="Rotations"/> times (moving the line-up one seat on each time), <paramref name="Games"/> deals per value.
/// </summary>
public sealed record SweptSeat(int Listed, int Games, int Rotations)
{
    /// <summary>The seat of the swept agent in game <paramref name="gameIndex"/> (the numbering of <see cref="RunPlan.At"/>).</summary>
    public int SeatIn(long gameIndex, int players)
    {
        int rotation = (int)(gameIndex % ((long)Games * Rotations) % Rotations);
        return (Listed + rotation) % players;
    }

    /// <summary>The swept seat a manifest records, or null if the run had no range or is older than the record.</summary>
    public static SweptSeat? From(RunManifest? manifest)
    {
        var inv = CultureInfo.InvariantCulture;
        if (manifest == null ||
            !int.TryParse(manifest.Settings.GetValueOrDefault("sweepSeat"), NumberStyles.Integer, inv, out int listed) ||
            !int.TryParse(manifest.Settings.GetValueOrDefault("games"), NumberStyles.Integer, inv, out int games) ||
            !int.TryParse(manifest.Settings.GetValueOrDefault("rotations"), NumberStyles.Integer, inv, out int rotations))
            return null;
        return new SweptSeat(listed, games, rotations);
    }
}
