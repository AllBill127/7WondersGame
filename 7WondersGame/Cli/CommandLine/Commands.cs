using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Agents;
using _7WondersGame.Cli.Arguments;
using _7WondersGame.Cli.Datasets;
using _7WondersGame.Cli.Experiments;
using _7WondersGame.Cli.Logs;
using _7WondersGame.Cli.Results;
using _7WondersGame.Cli.ResultsAnalysis;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Randomness.Services;
using Serilog;
using Serilog.Events;

namespace _7WondersGame.Cli.CommandLine;

/// <summary>The program's commands: run, replay, analyze, dataset, agents. Each returns the exit code.</summary>
public static class Commands
{
    /// <summary>Exit code: everything was done.</summary>
    public const int Ok = 0;

    /// <summary>Exit code: some games failed (each is logged with the command that replays it).</summary>
    public const int GamesFailed = 1;

    /// <summary>Exit code: the command line was wrong.</summary>
    public const int BadUsage = 2;

    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private static string ProgramDirectory
    {
        get
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }

    private static string ResultsDirectory
    {
        get
        {
            return Path.Combine(ProgramDirectory, "results");
        }
    }

    // ============ run ============

    /// <summary>
    /// 'run': plays the games of the plan and appends their rows to the results CSV, with a manifest next to it.
    /// </summary>
    public static async Task<int> Run(CliArgs cli, string[] args)
    {
        RunPlan plan = RunPlan.FromCli(cli);
        var (level, levelSource) = Logging.Resolve(plan.Log);
        Logging.Configure(level);

        List<List<int>[]>? deals = null;
        if (plan.Dataset != null)
        {
            deals = await LoadDataset(plan.Dataset, plan.Players.Count, plan.Games);
        }

        string csvPath = Path.GetFullPath(OutputPath(plan.Out));
        DateTime started = DateTime.UtcNow;
        string runId = RunManifest.NewRunId(started, plan.Seed);
        string runDirectory = Path.GetDirectoryName(csvPath)!;
        string manifestPath = Path.Combine(runDirectory, $"{runId}.run.json");
        string? experimentCopy = CopyExperimentFile(plan, runDirectory, runId);

        var manifest = new RunManifest
        {
            RunId = runId,
            RunSeed = plan.Seed,
            Args = args,
            Settings = ManifestSettings(plan, experimentCopy, $"{level} ({levelSource})"),
            ResultsCsv = csvPath,
            StartedUtc = started,
        };
        manifest.Save(manifestPath);
        LogRunStart(plan, manifest, csvPath, level, levelSource);
        WarnIfClockBudget(plan.Players, plan.Sweep, plan.Parallel);

        var runClock = Stopwatch.StartNew();
        GameRunner runner;
        using (var writer = new ResultsCsvWriter(csvPath))
        {
            runner = new GameRunner(plan, runId, deals, writer, runClock);
            runner.PlayAll();
        }

        manifest.FinishedUtc = DateTime.UtcNow;
        manifest.GamesCompleted = runner.Completed;
        manifest.GamesFailed = runner.Failed;
        manifest.Save(manifestPath);

        Log.Information("Run {RunId} finished in {Elapsed}: {Completed} games, {Failed} failed. Manifest: {Manifest}",
            runId, Clock(runClock.Elapsed), runner.Completed, runner.Failed, manifestPath);
        if (runner.Failed == 0)
        {
            return Ok;
        }
        return GamesFailed;
    }

    /// <summary>
    /// Copies the experiment file (if the plan came from one) next to the results, so the run can be repeated
    /// from them. Returns the copy's path.
    /// </summary>
    private static string? CopyExperimentFile(RunPlan plan, string runDirectory, string runId)
    {
        if (plan.Experiment == null)
        {
            return null;
        }
        Directory.CreateDirectory(runDirectory);
        string experimentCopy = Path.Combine(runDirectory, $"{runId}.experiment.json");
        File.Copy(plan.Experiment, experimentCopy, overwrite: true);
        return experimentCopy;
    }

    /// <summary>
    /// The run's options as the manifest records them, in a fixed order ("" where one doesn't apply).
    /// </summary>
    private static Dictionary<string, string> ManifestSettings(RunPlan plan, string? experimentCopy, string logText)
    {
        var settings = new Dictionary<string, string>
        {
            ["command"] = "run",
            ["players"] = string.Join(",", plan.Players),
            ["games"] = plan.Games.ToString(InvariantCulture),
            ["steps"] = "",
            ["sweep"] = "",
            ["rotate"] = "no",
            // for 'analyze': which row of a game is the swept agent (listed position, moved on by the rotation)
            ["rotations"] = plan.Rotations.ToString(InvariantCulture),
            ["sweepSeat"] = "",
            ["parallel"] = plan.Parallel.ToString(InvariantCulture),
            ["dataset"] = "",
            ["experiment"] = "",
            ["experimentCopy"] = "",
            ["description"] = "",
            ["log"] = logText,
        };

        // setting a key again keeps its place in the order
        Sweep? sweep = plan.Sweep;
        if (sweep != null)
        {
            settings["steps"] = sweep.Steps.ToString(InvariantCulture);
            settings["sweep"] = SweepDescription(sweep, plan.Players);
            settings["sweepSeat"] = sweep.Seat.ToString(InvariantCulture);
        }
        if (plan.Rotate)
        {
            settings["rotate"] = $"yes ({plan.Rotations} seatings per deal)";
        }
        if (plan.Dataset != null)
        {
            settings["dataset"] = ResolveInput(plan.Dataset, ProgramDirectory);
        }
        if (plan.Experiment != null)
        {
            settings["experiment"] = plan.Experiment;
        }
        if (experimentCopy != null)
        {
            settings["experimentCopy"] = experimentCopy;
        }
        if (plan.Description != null)
        {
            settings["description"] = plan.Description;
        }
        return settings;
    }

    /// <summary>
    /// The range setting, whose it is and its ends, e.g. "C of MctsAgent:C=0.3..2 (listed 1.) from 0.3 to 2".
    /// </summary>
    private static string SweepDescription(Sweep sweep, List<AgentSpec> specs)
    {
        int listedNumber = sweep.Seat + 1;
        string from = sweep.From.ToString(InvariantCulture);
        string to = sweep.To.ToString(InvariantCulture);
        return $"{sweep.Setting} of {specs[sweep.Seat]} (listed {listedNumber}.) from {from} to {to}";
    }

    private static void LogRunStart(RunPlan plan, RunManifest manifest, string csvPath, LogEventLevel level,
                                    string levelSource)
    {
        Log.Information("Run {RunId} | seed {RunSeed} (0x{SeedHex}) | commit {Commit} | log level {Level} ({Source})",
            manifest.RunId, plan.Seed, plan.Seed.ToString("x16"), manifest.GitCommit, level, levelSource);
        if (plan.Experiment != null)
        {
            Log.Information("Experiment: {File}{Description}", plan.Experiment, DescriptionNote(plan.Description));
        }
        Log.Information("Players: {Players}", string.Join(", ", plan.Players));
        Log.Information("{Total} games ({Deals} deals{Rotations}{Sweep}), {Parallel} at a time", plan.Total,
            plan.Games, RotationsNote(plan), SweepNote(plan.Sweep), plan.Parallel);
        Log.Information("Results: {Csv}", csvPath);
    }

    private static string DescriptionNote(string? description)
    {
        if (description == null)
        {
            return "";
        }
        return $" ({description})";
    }

    private static string RotationsNote(RunPlan plan)
    {
        if (!plan.Rotate)
        {
            return "";
        }
        return $" x {plan.Rotations} seatings";
    }

    private static string SweepNote(Sweep? sweep)
    {
        if (sweep == null)
        {
            return "";
        }
        return $" x {sweep.Steps} values of {sweep.Setting}";
    }

    /// <summary>A duration as h:mm:ss.</summary>
    internal static string Clock(TimeSpan duration)
    {
        return $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    /// <summary>
    /// A time budget makes the results depend on the machine; games run side by side also slow each other down,
    /// so a searcher gets fewer iterations than it would alone.
    /// </summary>
    private static void WarnIfClockBudget(List<AgentSpec> specs, Sweep? sweep, int parallel)
    {
        List<string> timed = TimedAgents(specs, sweep);
        if (timed.Count == 0)
        {
            return;
        }

        string agents = string.Join(", ", timed);
        if (parallel > 1)
        {
            Log.Warning("{Agents} search for a time, not a number of iterations, and {Parallel} games run at a time: " +
                        "they slow each other down, so each search gets fewer iterations than alone. For results " +
                        "that repeat, give an iteration budget (e.g. n=1000) or use --parallel 1.", agents, parallel);
        }
        else
        {
            Log.Information("{Agents} search for a time: the results depend on this machine.", agents);
        }
    }

    /// <summary>The agents of the line-up that search for a time, each listed once.</summary>
    private static List<string> TimedAgents(List<AgentSpec> specs, Sweep? sweep)
    {
        var timed = new List<string>();
        for (int seat = 0; seat < specs.Count; seat++)
        {
            double? value = null;
            if (sweep != null && sweep.Seat == seat)
            {
                value = sweep.ValueAt(0);
            }
            IAgent agent = AgentCatalog.Create(specs[seat], value);
            string spec = specs[seat].ToString();
            if (agent.UsesClock && !timed.Contains(spec))
            {
                timed.Add(spec);
            }
        }
        return timed;
    }

    // ============ replay ============

    /// <summary>
    /// 'replay': plays one game again with the step-by-step trace, from a results CSV or from a seed.
    /// </summary>
    public static async Task<int> Replay(CliArgs cli)
    {
        cli.AllowOnly("from", "run", "game", "players", "game-seed", "dataset", "deal", "log");
        var (level, _) = Logging.Resolve(cli.Get("log"), LogEventLevel.Debug);
        Logging.Configure(level);

        string? datasetName = cli.Get("dataset");
        int? deal = cli.GetInt("deal", 0);
        ReplaySetup setup;
        if (cli.Has("from"))
        {
            string csv = ResolveInput(cli.Require("from"), ResultsDirectory);
            setup = ReplaySetup.FromResults(cli, csv, datasetName, deal);
        }
        else
        {
            setup = ReplaySetup.FromSeed(cli, datasetName, deal);
        }

        if ((setup.DatasetName == null) != (setup.Deal == null))
        {
            throw new UsageException("--dataset and --deal go together.", "replay");
        }
        AgentCatalog.Validate(setup.Specs, null);

        Game game = await NewReplayGame(setup);
        Log.Information("Replaying game seed {Seed} (0x{SeedHex}){Deal} with {Players}", setup.GameSeed,
            setup.GameSeed.ToString("x16"), DealNote(setup), string.Join(", ", setup.Specs));
        game.Loop(record: true);

        foreach (SeatResult seatResult in game.Results!)
        {
            Log.Information("Seat {Seat} {Agent} {Wonder}{Side}: {VP} VP, {Coins} coins, rank {Rank}",
                seatResult.Seat, seatResult.Agent, seatResult.Wonder, seatResult.Side, seatResult.VictoryPoints,
                seatResult.Coins, seatResult.Rank);
        }
        if (setup.Recorded != null)
        {
            CompareWithRecorded(setup.Recorded, game);
        }
        return Ok;
    }

    private static async Task<Game> NewReplayGame(ReplaySetup setup)
    {
        List<List<int>[]>? deals = null;
        if (setup.DatasetName != null)
        {
            int dealsNeeded = setup.Deal!.Value + 1;
            deals = await LoadDataset(setup.DatasetName, setup.Specs.Count, dealsNeeded);
        }

        var game = new Game(setup.GameSeed)
        {
            Agents = setup.Specs.Select(spec => AgentCatalog.Create(spec)).ToList(),
            Trace = Log.Logger,
        };
        if (deals != null)
        {
            game.SetUpGameFromDataset(deals[setup.Deal!.Value]);
        }
        else
        {
            game.SetUpRandomGame();
        }
        return game;
    }

    private static string DealNote(ReplaySetup setup)
    {
        if (setup.DatasetName == null)
        {
            return "";
        }
        return $", deal {setup.Deal} of {setup.DatasetName}";
    }

    /// <summary>Says whether the replayed game ended as the results CSV recorded it, and why it may not have.</summary>
    private static void CompareWithRecorded(List<Dictionary<string, string>> recorded, Game game)
    {
        if (SameAsRecorded(recorded, game.Results!))
        {
            Log.Information("Same result as recorded in the CSV.");
        }
        else if (game.Agents.Any(agent => agent.UsesClock))
        {
            Log.Information("The result differs from the CSV row, as expected: an agent searches for a time, " +
                            "so its choices depend on how fast the machine was then.");
        }
        else
        {
            Log.Warning("The result differs from the CSV row: the code has changed since that run " +
                        "(its commit is in the run's .run.json), or its dataset file is missing.");
        }
    }

    private static bool SameAsRecorded(List<Dictionary<string, string>> recorded, List<SeatResult> results)
    {
        if (recorded.Count != results.Count)
        {
            return false;
        }
        for (int seat = 0; seat < recorded.Count; seat++)
        {
            if (!SameSeatResult(recorded[seat], results[seat]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool SameSeatResult(Dictionary<string, string> row, SeatResult result)
    {
        return row["vp"] == result.VictoryPoints.ToString(InvariantCulture)
            && row["coins"] == result.Coins.ToString(InvariantCulture)
            && row["wonder"] == result.Wonder
            && row["side"] == result.Side;
    }

    // ============ analyze ============

    /// <summary>
    /// 'analyze': prints each group's win rate and mean VP, and the paired differences between groups, from a
    /// results CSV; with --out also writes them as CSV.
    /// </summary>
    public static int Analyze(CliArgs cli)
    {
        cli.AllowOnly("from", "run", "by", "vs", "out", "log");
        Logging.Configure(Logging.Resolve(cli.Get("log")).Level);

        string csv = ResolveInput(cli.Require("from"), ResultsDirectory);
        if (!File.Exists(csv))
        {
            throw new UsageException($"Results file not found: {csv}", "analyze");
        }
        List<ResultRow> rows = ResultRow.Read(ResultsCsvWriter.ReadRows(csv));
        List<string> runs = RunsToAnalyze(cli.Get("run"), rows, csv);
        rows = rows.Where(row => runs.Contains(row.RunId)).ToList();
        if (rows.Count == 0)
        {
            throw new UsageException($"{csv} has no result rows.", "analyze");
        }
        // the swept agent is its own group even where a value equals an opponent's settings
        rows = Analysis.MarkSwept(rows, SweptSeats(runs, csv));

        GroupBy by = ParseGroupBy(cli.GetOrDefault("by", "player"));
        List<GroupStats> groups = Analysis.Groups(rows, by);
        int? vs = cli.GetInt("vs", 1, groups.Count);
        // all pairs of many groups (e.g. 14 wonders: 91 pairs) is a long list where some differ by chance alone
        bool allPairs = vs != null || groups.Count <= Analysis.MaxAllPairsGroups;
        var pairs = new List<PairStats>();
        if (allPairs)
        {
            pairs = Analysis.Pairs(rows, by, groups, vs);
        }

        PrintAnalysis(csv, rows, runs, by, groups, pairs, allPairs);
        string? outputName = cli.Get("out");
        if (outputName != null)
        {
            WriteAnalysis(outputName, csv, groups, pairs);
        }
        return Ok;
    }

    /// <summary>
    /// The runs to analyze. Several runs in one file may be different experiments, so mixing them is asked for
    /// (<c>--run all</c> or a list), not assumed.
    /// </summary>
    private static List<string> RunsToAnalyze(string? runOption, List<ResultRow> rows, string csv)
    {
        List<string> runsInFile = rows.Select(row => row.RunId).Distinct().ToList();
        if (runOption == null)
        {
            if (runsInFile.Count > 1)
            {
                throw new UsageException($"{csv} has {runsInFile.Count} runs: {string.Join(", ", runsInFile)}. " +
                                         "Give --run <runId>[,<runId>...] or --run all.", "analyze");
            }
            return runsInFile;
        }
        if (runOption.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return runsInFile;
        }

        List<string> runs = runOption
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        string? missing = runs.FirstOrDefault(run => !runsInFile.Contains(run));
        if (missing != null)
        {
            throw new UsageException(
                $"No run {missing} in {csv}. Runs there: {string.Join(", ", runsInFile)}.",
                "analyze");
        }
        return runs;
    }

    /// <summary>Where the swept agent sits in each run that had a range setting, by run id.</summary>
    private static Dictionary<string, SweptSeat> SweptSeats(List<string> runs, string csv)
    {
        var sweeps = new Dictionary<string, SweptSeat>();
        foreach (string run in runs)
        {
            RunManifest? manifest = RunManifest.Load(Path.Combine(Path.GetDirectoryName(csv)!, $"{run}.run.json"));
            SweptSeat? swept = SweptSeat.From(manifest);
            if (swept != null)
            {
                sweeps[run] = swept;
            }
        }
        return sweeps;
    }

    private static GroupBy ParseGroupBy(string byText)
    {
        if (!Enum.TryParse(byText, ignoreCase: true, out GroupBy by) || !Enum.IsDefined(by))
        {
            throw new UsageException($"--by must be player, agent, seat or wonder, not '{byText}'.", "analyze");
        }
        return by;
    }

    private static void PrintAnalysis(string csv, List<ResultRow> rows, List<string> runs, GroupBy by,
                                      List<GroupStats> groups, List<PairStats> pairs, bool allPairs)
    {
        int games = rows.Select(row => (row.RunId, row.GameIndex)).Distinct().Count();
        int deals = rows.Select(row => row.Seed).Distinct().Count();
        Console.WriteLine($"{csv}: {games} games on {deals} deals, {RunOrRuns(runs.Count)} " +
                          $"{string.Join(", ", runs)}, grouped by {by.ToString().ToLowerInvariant()}");
        Console.WriteLine();
        Console.WriteLine(Analysis.GroupsTable(groups));
        if (pairs.Count > 0)
        {
            Console.WriteLine("Paired differences on the same deals (A - B; VP, and win share where a win counts 1):");
            Console.WriteLine(Analysis.PairsTable(pairs));
        }
        else if (!allPairs)
        {
            Console.WriteLine($"{groups.Count} groups: give --vs <n> for paired differences against group n.");
        }
        else if (groups.Count > 1)
        {
            Console.WriteLine("No two groups played the same deals, so there are no paired differences.");
        }
    }

    private static string RunOrRuns(int runCount)
    {
        if (runCount == 1)
        {
            return "run";
        }
        return "runs";
    }

    /// <summary>
    /// Writes the group table to <paramref name="outputName"/> (a relative name goes next to the results CSV) and the
    /// pairs next to it, as <c>&lt;name&gt;.pairs.csv</c>.
    /// </summary>
    private static void WriteAnalysis(string outputName, string csv, List<GroupStats> groups, List<PairStats> pairs)
    {
        string outputPath = outputName;
        if (!Path.IsPathRooted(outputName))
        {
            outputPath = Path.Combine(Path.GetDirectoryName(csv)!, outputName);
        }
        string outputDirectory = Path.GetDirectoryName(outputPath)!;
        string pairsPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(outputPath) + ".pairs.csv");

        Directory.CreateDirectory(outputDirectory);
        File.WriteAllLines(outputPath, Analysis.GroupsCsv(groups));
        File.WriteAllLines(pairsPath, Analysis.PairsCsv(pairs));
        Console.WriteLine($"Wrote {outputPath} and {pairsPath}");
    }

    // ============ dataset ============

    /// <summary>'dataset': writes the deals of a seed (decks and wonders) to a file for 'run --dataset'.</summary>
    public static async Task<int> Dataset(CliArgs cli)
    {
        cli.AllowOnly("seats", "games", "seed", "out", "log");
        int seats = cli.RequireInt("seats", 3, 7);
        int games = cli.RequireInt("games", 1);
        ulong seed = ParseRunSeed(cli.Require("seed"));
        Logging.Configure(Logging.Resolve(cli.Get("log")).Level);

        string outputName = cli.GetOrDefault("out", $"gameDataset_{seats}p_{games}g.txt");
        string outputPath = Path.GetFullPath(Path.Combine(ProgramDirectory, outputName));

        var gameDatasets = new List<List<int>[]>();
        for (int dealIndex = 0; dealIndex < games; dealIndex++)
        {
            gameDatasets.Add(DealOf(seed, dealIndex, seats));
        }

        await Filer.WriteGameDatasetToFile(outputPath, seats, gameDatasets);
        Log.Information("Wrote {Games} deals for {Seats} players (seed {Seed}) to {File}",
                        games, seats, seed, outputPath);
        return Ok;
    }

    /// <summary>
    /// The deal 'run --seed &lt;seed&gt;' plays as game <paramref name="gameIndex"/>: the three decks and the
    /// wonders, as ids.
    /// </summary>
    private static List<int>[] DealOf(ulong seed, int gameIndex, int seats)
    {
        var agents = new List<IAgent>(seats);
        for (int seat = 0; seat < seats; seat++)
        {
            agents.Add(new RandomChoiceAI());
        }
        var game = new Game(Seeding.GameSeed(seed, gameIndex)) { Agents = agents };
        game.SetUpRandomGame();

        int decksAndWonders = Game.LastEra + 1;
        var deal = new List<int>[decksAndWonders];
        for (int era = 0; era < Game.LastEra; era++)
        {
            deal[era] = new List<int>();
            foreach (Card card in game.Decks[era])
            {
                deal[era].Add((int)card.Id);
            }
        }

        // the wonders come after the decks
        var wonders = new List<int>();
        foreach (PlayerState player in game.Players)
        {
            wonders.Add((int)player.Wonder.Id);
        }
        deal[Game.LastEra] = wonders;
        return deal;
    }

    // ============ agents ============

    /// <summary>'agents': lists the agents and their settings.</summary>
    public static int Agents(CliArgs cli)
    {
        cli.AllowOnly();
        Console.WriteLine(AgentsText());
        return Ok;
    }

    /// <summary>The agents, each with its settings, their types and defaults.</summary>
    public static string AgentsText()
    {
        var text = new StringBuilder();
        text.AppendLine("Agents (use in --players as Name[:setting=value...]):");
        text.AppendLine();
        foreach (AgentCatalogEntry entry in AgentCatalog.Entries)
        {
            text.AppendLine($"  {entry.Name,-32} {entry.Description}");
            foreach (var (name, type, value) in AgentCatalog.SettingsOf(entry))
            {
                text.AppendLine($"      {SettingLabel(name),-36} {type.Name,-8} default {ShownValue(value)}");
            }
        }
        text.AppendLine();
        text.AppendLine("Agents without settings listed have none. Setting names are not case-sensitive.");
        return text.ToString();
    }

    /// <summary>The setting's name, after its alias if it has one: "n (Iterations)".</summary>
    private static string SettingLabel(string name)
    {
        foreach (KeyValuePair<string, string> alias in AgentCatalog.Aliases)
        {
            if (alias.Value == name)
            {
                return $"{alias.Key} ({name})";
            }
        }
        return name;
    }

    private static string ShownValue(object? value)
    {
        string? shown = Convert.ToString(value, InvariantCulture);
        if (shown == null)
        {
            return "";
        }
        return shown;
    }

    // ============ shared ============

    /// <summary>A number, or "random" for a fresh one (it is printed and saved in the manifest).</summary>
    internal static ulong ParseRunSeed(string text)
    {
        if (text.Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            return BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
        }
        if (!CliArgs.TryParseSeed(text, out ulong seed))
        {
            throw new UsageException($"--seed must be a number (decimal or 0x hex) or 'random', not '{text}'.");
        }
        return seed;
    }

    /// <summary>
    /// Where a result file goes: as given if absolute, otherwise in the results folder next to the program.
    /// </summary>
    private static string OutputPath(string name)
    {
        if (Path.IsPathRooted(name))
        {
            return name;
        }
        return Path.Combine(ResultsDirectory, name);
    }

    /// <summary>
    /// An input file: as given if it exists from the current folder, otherwise in <paramref name="fallbackDirectory"/>.
    /// </summary>
    private static string ResolveInput(string name, string fallbackDirectory)
    {
        if (Path.IsPathRooted(name) || File.Exists(name))
        {
            return Path.GetFullPath(name);
        }
        return Path.Combine(fallbackDirectory, name);
    }

    /// <summary>The deals of a dataset file, checked against the line-up.</summary>
    private static async Task<List<List<int>[]>> LoadDataset(string name, int seats, int dealsNeeded)
    {
        string path = ResolveInput(name, ProgramDirectory);
        if (!File.Exists(path))
        {
            throw new UsageException($"Dataset file not found: {path}");
        }
        int players;
        List<List<int>[]> deals;
        try
        {
            (players, deals) = await Filer.ReadGameDatasetFromFile(path);
        }
        catch (InvalidDataException exception)
        {
            throw new UsageException(exception.Message);
        }
        if (players != seats)
        {
            throw new UsageException($"{path} deals are for {players} players, but the line-up has {seats}.");
        }
        if (deals.Count < dealsNeeded)
        {
            throw new UsageException($"{path} has {deals.Count} deals; {dealsNeeded} are needed.");
        }
        Log.Information("Dataset {File}: {Deals} deals for {Players} players, all checked", path, deals.Count, players);
        return deals;
    }
}
