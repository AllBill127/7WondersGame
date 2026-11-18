using System.Globalization;
using _7WondersGame.Cli.Agents;
using _7WondersGame.Cli.Arguments;
using _7WondersGame.Cli.Experiments;
using _7WondersGame.Cli.Results;

namespace _7WondersGame.Cli.CommandLine;

/// <summary>
/// What 'replay' plays: the line-up, the game's seed, the deal of a dataset file (if any), and, when the game comes
/// from a results CSV, its recorded rows, to check the result against.
/// </summary>
internal sealed class ReplaySetup
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private ReplaySetup(List<AgentSpec> specs, ulong gameSeed, string? datasetName, int? deal,
                        List<Dictionary<string, string>>? recorded)
    {
        Specs = specs;
        GameSeed = gameSeed;
        DatasetName = datasetName;
        Deal = deal;
        Recorded = recorded;
    }

    /// <summary>The agent in each seat.</summary>
    public List<AgentSpec> Specs { get; }

    /// <summary>The game's seed.</summary>
    public ulong GameSeed { get; }

    /// <summary>The dataset file the deal comes from, or null to deal from the seed.</summary>
    public string? DatasetName { get; private set; }

    /// <summary>The deal's number in the dataset file.</summary>
    public int? Deal { get; private set; }

    /// <summary>The game's rows in the results CSV, in seat order, or null when replaying from a seed.</summary>
    public List<Dictionary<string, string>>? Recorded { get; }

    /// <summary>
    /// Game <c>--game</c> of run <c>--run</c> in the results CSV <paramref name="csv"/>: its agents, settings and
    /// seed come from its rows, and its dataset, if any, from the run's manifest.
    /// </summary>
    public static ReplaySetup FromResults(CliArgs cli, string csv, string? datasetName, int? deal)
    {
        if (cli.Has("players") || cli.Has("game-seed"))
        {
            throw new UsageException(
                "Give either --from with --run and --game, or --players with --game-seed.",
                "replay");
        }
        if (!File.Exists(csv))
        {
            throw new UsageException($"Results file not found: {csv}", "replay");
        }
        string runId = cli.Require("run");
        int gameIndex = cli.RequireInt("game", 0);

        List<Dictionary<string, string>> recorded = RecordedRows(csv, runId, gameIndex);
        List<AgentSpec> specs = recorded.Select(RecordedSpec).ToList();
        ulong gameSeed = ulong.Parse(recorded[0]["seed"], InvariantCulture);
        var setup = new ReplaySetup(specs, gameSeed, datasetName, deal, recorded);
        setup.UseRunDataset(csv, runId, recorded);
        return setup;
    }

    /// <summary>The line-up of <c>--players</c> and the seed of <c>--game-seed</c>.</summary>
    public static ReplaySetup FromSeed(CliArgs cli, string? datasetName, int? deal)
    {
        List<AgentSpec> specs = RunPlan.ParsePlayers(cli.Require("players"), "replay");
        if (specs.Any(HasRange))
        {
            throw new UsageException("replay takes single setting values, not ranges a..b.", "replay");
        }
        ulong? gameSeed = cli.GetSeed("game-seed");
        if (gameSeed == null)
        {
            throw new UsageException(
                "Give --game-seed (the seed column of the results CSV), or --from with --run and --game.",
                "replay");
        }
        return new ReplaySetup(specs, gameSeed.Value, datasetName, deal, null);
    }

    /// <summary>The rows of one game in a results CSV, in seat order, or a usage error if there are none.</summary>
    private static List<Dictionary<string, string>> RecordedRows(string csv, string runId, int gameIndex)
    {
        string gameText = gameIndex.ToString(InvariantCulture);
        List<Dictionary<string, string>> recorded = ResultsCsvWriter.ReadRows(csv)
            .Where(row => row.GetValueOrDefault("runId") == runId && row.GetValueOrDefault("gameIndex") == gameText)
            .OrderBy(SeatOf)
            .ToList();
        if (recorded.Count == 0)
        {
            throw new UsageException($"No rows for run {runId}, game {gameIndex} in {csv}.", "replay");
        }
        return recorded;
    }

    private static int SeatOf(Dictionary<string, string> row)
    {
        return int.Parse(row["seat"], InvariantCulture);
    }

    private static AgentSpec RecordedSpec(Dictionary<string, string> row)
    {
        return AgentCatalog.FromResultRow(row["agent"], row["configJson"]);
    }

    private static bool HasRange(AgentSpec spec)
    {
        foreach (KeyValuePair<string, string> setting in spec.Settings)
        {
            if (Sweep.TryParseRange(setting.Value, out _, out _))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// A run that used a dataset recorded it in its manifest: without <c>--dataset</c>, the game is dealt from that
    /// file, and without <c>--deal</c>, its deal is the recorded one.
    /// </summary>
    private void UseRunDataset(string csv, string runId, List<Dictionary<string, string>> recorded)
    {
        string manifestPath = Path.Combine(Path.GetDirectoryName(csv)!, $"{runId}.run.json");
        RunManifest? manifest = RunManifest.Load(manifestPath);
        if (manifest == null)
        {
            return;
        }

        string? runDataset = manifest.Settings.GetValueOrDefault("dataset");
        if (DatasetName == null && !string.IsNullOrEmpty(runDataset))
        {
            DatasetName = runDataset;
            if (Deal == null)
            {
                Deal = int.Parse(recorded[0]["dealIndex"], InvariantCulture);
            }
        }
    }
}
