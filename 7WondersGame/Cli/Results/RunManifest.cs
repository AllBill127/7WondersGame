using System.Reflection;
using System.Text.Json;

namespace _7WondersGame.Cli.Results;

/// <summary>
/// The run manifest, saved next to the results CSV: what was run, from which code, and when.
/// Written when the run starts and again when it ends.
/// </summary>
public sealed class RunManifest
{
    private const string UnknownCommit = "unknown";

    /// <summary>The run's id (see <see cref="NewRunId"/>).</summary>
    public string RunId { get; init; } = "";

    /// <summary>The run's seed.</summary>
    public ulong RunSeed { get; init; }

    /// <summary>The command line as given.</summary>
    public string[] Args { get; init; } = Array.Empty<string>();

    /// <summary>The run's options as the program understood them, by name.</summary>
    public Dictionary<string, string> Settings { get; init; } = new();

    /// <summary>The commit the program was built from (see <see cref="GetGitCommit"/>).</summary>
    public string GitCommit { get; init; } = GetGitCommit();

    /// <summary>Where it ran: search times (and anything with a time budget) depend on it.</summary>
    public MachineInfo Machine { get; init; } = MachineInfo.Current();

    /// <summary>The results CSV (full path).</summary>
    public string ResultsCsv { get; init; } = "";

    /// <summary>When the run started.</summary>
    public DateTime StartedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>When the run ended, or null while it runs.</summary>
    public DateTime? FinishedUtc { get; set; }

    /// <summary>How many games finished and were written.</summary>
    public int GamesCompleted { get; set; }

    /// <summary>How many games failed.</summary>
    public int GamesFailed { get; set; }

    /// <summary>Writes the manifest as indented JSON.</summary>
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>The manifest saved at <paramref name="path"/>, or null if there is none.</summary>
    public static RunManifest? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        return JsonSerializer.Deserialize<RunManifest>(File.ReadAllText(path));
    }

    /// <summary>
    /// The commit the program was built from (stamped in at build time, with "-dirty" if there were
    /// uncommitted changes), or "unknown".
    /// </summary>
    public static string GetGitCommit()
    {
        var attribute = typeof(RunManifest).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute == null)
        {
            return UnknownCommit;
        }

        // the version is "<version>+<commit>"
        string version = attribute.InformationalVersion;
        int plus = version.IndexOf('+');
        if (plus < 0)
        {
            return UnknownCommit;
        }
        int commitStart = plus + 1;
        return version.Substring(commitStart);
    }

    /// <summary>A run id that sorts by start time and shows the seed.</summary>
    public static string NewRunId(DateTime startedUtc, ulong runSeed)
    {
        return $"{startedUtc:yyyyMMdd-HHmmss}-{runSeed:x16}";
    }
}
