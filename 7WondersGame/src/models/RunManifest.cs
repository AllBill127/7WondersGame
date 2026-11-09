using System.Reflection;
using System.Text.Json;

namespace _7WondersGame.src.models
{
    /// <summary>
    /// The run manifest, saved next to the results CSV (B2): what was run, from which code, and when.
    /// Written when the run starts and again when it ends.
    /// </summary>
    public sealed class RunManifest
    {
        public string RunId { get; init; } = "";
        public ulong RunSeed { get; init; }
        public string[] Args { get; init; } = Array.Empty<string>();
        public Dictionary<string, string> Settings { get; init; } = new();
        public string GitCommit { get; init; } = GetGitCommit();
        public string ResultsCsv { get; init; } = "";
        public DateTime StartedUtc { get; init; } = DateTime.UtcNow;
        public DateTime? FinishedUtc { get; set; }
        public int GamesCompleted { get; set; }
        public int GamesFailed { get; set; }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static RunManifest? Load(string path) =>
            File.Exists(path) ? JsonSerializer.Deserialize<RunManifest>(File.ReadAllText(path)) : null;

        /// <summary>
        /// The commit the program was built from (stamped in at build time, with "-dirty" if there were
        /// uncommitted changes), or "unknown".
        /// </summary>
        public static string GetGitCommit()
        {
            string? version = typeof(RunManifest).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            int plus = version?.IndexOf('+') ?? -1;
            return plus >= 0 ? version![(plus + 1)..] : "unknown";
        }

        /// <summary>A run id that sorts by start time and shows the seed.</summary>
        public static string NewRunId(DateTime startedUtc, ulong runSeed) =>
            $"{startedUtc:yyyyMMdd-HHmmss}-{runSeed:x16}";
    }
}
