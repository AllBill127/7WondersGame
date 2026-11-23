using System.Globalization;
using _7Wonders.Cli.Experiments;
using _7Wonders.Cli.Results;

namespace _7Wonders.Cli.ResultsAnalysis;

/// <summary>
/// Where a sweep run's swept agent sits, from its manifest: listed in position <see cref="Listed"/>, each deal
/// played <see cref="Rotations"/> times (moving the line-up one seat on each time), <see cref="Games"/> deals
/// per value.
/// </summary>
internal sealed class SweptSeat
{
    /// <summary>The swept agent listed in position <paramref name="listed"/>.</summary>
    public SweptSeat(int listed, int games, int rotations)
    {
        Listed = listed;
        Games = games;
        Rotations = rotations;
    }

    /// <summary>The swept agent's position in the line-up as listed.</summary>
    public int Listed { get; }

    /// <summary>Deals per sweep value.</summary>
    public int Games { get; }

    /// <summary>How many seatings each deal is played in.</summary>
    public int Rotations { get; }

    /// <summary>
    /// The seat of the swept agent in game <paramref name="gameIndex"/> (the numbering of <see cref="RunPlan.At"/>).
    /// </summary>
    public int SeatIn(long gameIndex, int players)
    {
        int rotation = (int)(gameIndex % ((long)Games * Rotations) % Rotations);
        return (Listed + rotation) % players;
    }

    /// <summary>
    /// The swept seat a manifest records, or null if the run had no range or is older than the record.
    /// </summary>
    public static SweptSeat? From(RunManifest? manifest)
    {
        if (manifest == null)
        {
            return null;
        }
        int? listed = WholeNumberSetting(manifest, "sweepSeat");
        int? games = WholeNumberSetting(manifest, "games");
        int? rotations = WholeNumberSetting(manifest, "rotations");
        if (listed == null || games == null || rotations == null)
        {
            return null;
        }
        return new SweptSeat(listed.Value, games.Value, rotations.Value);
    }

    /// <summary>A setting of the manifest that is a whole number, or null if it is missing or isn't one.</summary>
    private static int? WholeNumberSetting(RunManifest manifest, string name)
    {
        string? text = manifest.Settings.GetValueOrDefault(name);
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            return null;
        }
        return value;
    }
}
