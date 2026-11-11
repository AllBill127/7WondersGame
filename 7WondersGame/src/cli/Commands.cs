using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using _7WondersGame.src.models;
using _7WondersGame.src.models.Agents;
using Serilog;
using Serilog.Events;

namespace _7WondersGame.src.cli
{
    /// <summary>The program's commands: run, replay, dataset, agents. Each returns the exit code.</summary>
    public static class Commands
    {
        public const int Ok = 0, GamesFailed = 1, BadUsage = 2;

        private static string ProgramDir => AppDomain.CurrentDomain.BaseDirectory;
        private static string ResultsDir => Path.Combine(ProgramDir, "results");
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ============ run ============

        public static async Task<int> Run(CliArgs cli, string[] args)
        {
            cli.AllowOnly("players", "games", "seed", "steps", "parallel", "out", "dataset", "log");
            List<AgentSpec> specs = ParsePlayers(cli);
            int games = cli.RequireInt("games", 1);
            ulong runSeed = ParseRunSeed(cli.Require("seed"));
            int? steps = cli.GetInt("steps", 2);
            int parallel = cli.GetInt("parallel", 1) ?? 1;
            Sweep? sweep = AgentCatalog.Validate(specs, steps);
            var (level, levelSource) = Logging.Resolve(cli.Get("log"));
            Logging.Configure(level);

            string? datasetName = cli.Get("dataset");
            List<List<int>[]>? deals = datasetName == null ? null : await LoadDataset(datasetName, specs.Count, games);

            string csvPath = Path.GetFullPath(OutputPath(cli.Get("out") ?? "results.csv"));
            DateTime started = DateTime.UtcNow;
            string runId = RunManifest.NewRunId(started, runSeed);
            string manifestPath = Path.Combine(Path.GetDirectoryName(csvPath)!, $"{runId}.run.json");
            int total = games * (sweep?.Steps ?? 1);

            var manifest = new RunManifest
            {
                RunId = runId,
                RunSeed = runSeed,
                Args = args,
                Settings = new Dictionary<string, string>
                {
                    ["command"] = "run",
                    ["players"] = string.Join(",", specs),
                    ["games"] = games.ToString(Inv),
                    ["steps"] = sweep?.Steps.ToString(Inv) ?? "",
                    ["sweep"] = sweep == null ? "" : $"seat {sweep.Seat} {sweep.Setting} from {sweep.From.ToString(Inv)} to {sweep.To.ToString(Inv)}",
                    ["parallel"] = parallel.ToString(Inv),
                    ["dataset"] = datasetName == null ? "" : ResolveInput(datasetName, ProgramDir),
                    ["log"] = $"{level} ({levelSource})",
                },
                ResultsCsv = csvPath,
                StartedUtc = started,
            };
            manifest.Save(manifestPath);

            Log.Information("Run {RunId} | seed {RunSeed} (0x{SeedHex}) | commit {Commit} | log level {Level} ({Source})",
                runId, runSeed, runSeed.ToString("x16"), manifest.GitCommit, level, levelSource);
            Log.Information("Players: {Players}", string.Join(", ", specs));
            Log.Information("{Total} games{Sweep}, {Parallel} at a time", total,
                sweep == null ? "" : $" ({games} deals x {sweep.Steps} values of {sweep.Setting})", parallel);
            Log.Information("Results: {Csv}", csvPath);

            int completed = 0, failed = 0;
            object setupLock = new();
            using (var writer = new ResultsCsvWriter(csvPath))
            {
                Parallel.For(0, total, new ParallelOptions { MaxDegreeOfParallelism = parallel }, i =>
                {
                    // game i always gets the same deal, seed and sweep value, whatever the thread timing;
                    // every sweep value plays the same deals
                    int step = i / games;
                    int deal = i % games;
                    double? value = sweep?.ValueAt(step);
                    ILogger log = Logging.ForGame(i);

                    Game game;
                    lock (setupLock)
                    {
                        List<IAgent> agents = specs.Select(s => AgentCatalog.Create(s, value)).ToList();
                        game = new Game(Seeding.GameSeed(runSeed, deal)) { Agents = agents, Trace = log };
                        if (deals != null)
                            game.InitGameNotRandom(deals[deal]);
                        else
                            game.InitGame();
                    }

                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        game.Loop(record: true);
                        writer.WriteGame(runId, i, deal, game.Seed, game.Results!);
                        int done = Interlocked.Increment(ref completed);
                        log.Information("{Done}/{Total} done in {Seconds:0.0}s{Value}: {Summary}", done, total,
                            watch.Elapsed.TotalSeconds,
                            sweep == null ? "" : $" ({sweep.Setting}={value!.Value.ToString("0.####", Inv)})",
                            Summary(game.Results!));
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failed);
                        string replay = $"replay --players {string.Join(",", specs.Select(s => WithValue(s, value)))} " +
                                        $"--game-seed 0x{game.Seed:x16}" +
                                        (datasetName != null ? $" --dataset {datasetName} --deal {deal}" : "");
                        log.Error(ex, "Game failed (seed 0x{Seed}). To replay it: {Replay}", game.Seed.ToString("x16"), replay);
                    }
                });
            }

            manifest.FinishedUtc = DateTime.UtcNow;
            manifest.GamesCompleted = completed;
            manifest.GamesFailed = failed;
            manifest.Save(manifestPath);

            Log.Information("Run {RunId} finished: {Completed} games, {Failed} failed. Manifest: {Manifest}",
                runId, completed, failed, manifestPath);
            return failed == 0 ? Ok : GamesFailed;
        }

        private static string Summary(IEnumerable<SeatResult> seats) =>
            string.Join(" | ", seats.Select(s => $"{s.Seat} {s.Agent} {s.Wonder}{s.Side} {s.VictoryPoints}vp #{s.Rank}"));

        /// <summary>The spec with its range (if any) replaced by the value this game used.</summary>
        private static string WithValue(AgentSpec spec, double? value) =>
            new AgentSpec(spec.Name, spec.Settings.Select(kv =>
                value != null && Sweep.TryParseRange(kv.Value, out _, out _)
                    ? new KeyValuePair<string, string>(kv.Key, value.Value.ToString("R", Inv))
                    : kv).ToList()).ToString();

        // ============ replay ============

        public static async Task<int> Replay(CliArgs cli)
        {
            cli.AllowOnly("from", "run", "game", "players", "game-seed", "dataset", "deal", "log");
            var (level, _) = Logging.Resolve(cli.Get("log"), LogEventLevel.Debug);
            Logging.Configure(level);

            List<AgentSpec> specs;
            ulong gameSeed;
            string? datasetName = cli.Get("dataset");
            int? deal = cli.GetInt("deal", 0);
            List<Dictionary<string, string>>? recorded = null;

            if (cli.Has("from"))
            {
                if (cli.Has("players") || cli.Has("game-seed"))
                    throw new UsageException("Give either --from with --run and --game, or --players with --game-seed.", "replay");
                string csv = ResolveInput(cli.Require("from"), ResultsDir);
                if (!File.Exists(csv))
                    throw new UsageException($"Results file not found: {csv}", "replay");
                string runId = cli.Require("run");
                int gameIndex = cli.RequireInt("game", 0);
                string gameText = gameIndex.ToString(Inv);

                recorded = ResultsCsvWriter.ReadRows(csv)
                    .Where(r => r.GetValueOrDefault("runId") == runId && r.GetValueOrDefault("gameIndex") == gameText)
                    .OrderBy(r => int.Parse(r["seat"], Inv))
                    .ToList();
                if (recorded.Count == 0)
                    throw new UsageException($"No rows for run {runId}, game {gameIndex} in {csv}.", "replay");

                specs = recorded.Select(r => AgentCatalog.FromResultRow(r["agent"], r["configJson"])).ToList();
                gameSeed = ulong.Parse(recorded[0]["seed"], Inv);

                // a run that used a dataset recorded it in its manifest
                RunManifest? manifest = RunManifest.Load(Path.Combine(Path.GetDirectoryName(csv)!, $"{runId}.run.json"));
                string? runDataset = manifest?.Settings.GetValueOrDefault("dataset");
                if (datasetName == null && !string.IsNullOrEmpty(runDataset))
                {
                    datasetName = runDataset;
                    deal ??= int.Parse(recorded[0]["dealIndex"], Inv);
                }
            }
            else
            {
                specs = ParsePlayers(cli);
                if (specs.Any(s => s.Settings.Any(kv => Sweep.TryParseRange(kv.Value, out _, out _))))
                    throw new UsageException("replay takes single setting values, not ranges a..b.", "replay");
                gameSeed = cli.GetSeed("game-seed") ?? throw new UsageException(
                    "Give --game-seed (the seed column of the results CSV), or --from with --run and --game.", "replay");
            }

            if ((datasetName == null) != (deal == null))
                throw new UsageException("--dataset and --deal go together.", "replay");
            AgentCatalog.Validate(specs, null);

            List<List<int>[]>? deals = datasetName == null ? null : await LoadDataset(datasetName, specs.Count, deal!.Value + 1);

            var game = new Game(gameSeed)
            {
                Agents = specs.Select(s => AgentCatalog.Create(s)).ToList(),
                Trace = Log.Logger,
            };
            if (deals != null)
                game.InitGameNotRandom(deals[deal!.Value]);
            else
                game.InitGame();

            Log.Information("Replaying game seed {Seed} (0x{SeedHex}){Deal} with {Players}", gameSeed, gameSeed.ToString("x16"),
                deals != null ? $", deal {deal} of {datasetName}" : "", string.Join(", ", specs));
            game.Loop(record: true);

            foreach (SeatResult s in game.Results!)
                Log.Information("Seat {Seat} {Agent} {Wonder}{Side}: {VP} VP, {Coins} coins, rank {Rank}",
                    s.Seat, s.Agent, s.Wonder, s.Side, s.VictoryPoints, s.Coins, s.Rank);

            if (recorded != null)
            {
                bool same = recorded.Count == game.Results!.Count && recorded.Zip(game.Results!).All(p =>
                    p.First["vp"] == p.Second.VictoryPoints.ToString(Inv) &&
                    p.First["coins"] == p.Second.Coins.ToString(Inv) &&
                    p.First["wonder"] == p.Second.Wonder && p.First["side"] == p.Second.Side);
                if (same)
                    Log.Information("Same result as recorded in the CSV.");
                else
                    Log.Warning("The result differs from the CSV row: the code has changed since that run " +
                                "(its commit is in the run's .run.json), or its dataset file is missing.");
            }
            return Ok;
        }

        // ============ dataset ============

        public static async Task<int> Dataset(CliArgs cli)
        {
            cli.AllowOnly("seats", "games", "seed", "out", "log");
            int seats = cli.RequireInt("seats", 3, 7);
            int games = cli.RequireInt("games", 1);
            ulong seed = ParseRunSeed(cli.Require("seed"));
            Logging.Configure(Logging.Resolve(cli.Get("log")).Level);

            string outPath = Path.GetFullPath(Path.Combine(ProgramDir, cli.Get("out") ?? $"gameDataset_{seats}p_{games}g.txt"));

            var gameDatasets = new List<List<int>[]>();
            for (int i = 0; i < games; ++i)
            {
                // the same deal 'run --seed <seed>' plays as game i
                var game = new Game(Seeding.GameSeed(seed, i))
                {
                    Agents = Enumerable.Range(0, seats).Select(_ => (IAgent)new RandomChoiceAI()).ToList(),
                };
                game.InitGame();

                var gameData = new[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
                for (int era = 0; era < 3; era++)
                    gameData[era].AddRange(game.Decks[era].Select(card => (int)card.Id));
                gameData[3].AddRange(game.Players.Select(p => (int)p.Board.Id));
                gameDatasets.Add(gameData);
            }

            await Filer.WriteGameDatasetToFile(outPath, seats, gameDatasets);
            Log.Information("Wrote {Games} deals for {Seats} players (seed {Seed}) to {File}", games, seats, seed, outPath);
            return Ok;
        }

        // ============ agents ============

        public static int Agents(CliArgs cli)
        {
            cli.AllowOnly();
            Console.WriteLine(AgentsText());
            return Ok;
        }

        public static string AgentsText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Agents (use in --players as Name[:setting=value...]):");
            sb.AppendLine();
            foreach (var entry in AgentCatalog.Entries)
            {
                sb.AppendLine($"  {entry.Name,-32} {entry.Description}");
                foreach (var (name, type, value) in AgentCatalog.SettingsOf(entry))
                {
                    string alias = AgentCatalog.Aliases.FirstOrDefault(a => a.Value == name).Key ?? "";
                    string shown = Convert.ToString(value, Inv) ?? "";
                    sb.AppendLine($"      {(alias == "" ? name : $"{alias} ({name})"),-36} {type.Name,-8} default {shown}");
                }
            }
            sb.AppendLine();
            sb.AppendLine("Agents without settings listed have none. Setting names are not case-sensitive.");
            return sb.ToString();
        }

        // ============ shared ============

        private static List<AgentSpec> ParsePlayers(CliArgs cli)
        {
            List<AgentSpec> specs = AgentSpec.ParseList(cli.Require("players"));
            if (specs.Count is < 3 or > 7)
                throw new UsageException($"--players lists {specs.Count} agents; a game has 3 to 7 players.", cli.Command);
            return specs;
        }

        /// <summary>A number, or "random" for a fresh one (it is printed and saved in the manifest).</summary>
        private static ulong ParseRunSeed(string text)
        {
            if (text.Equals("random", StringComparison.OrdinalIgnoreCase))
                return BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
            if (!CliArgs.TryParseSeed(text, out ulong seed))
                throw new UsageException($"--seed must be a number (decimal or 0x hex) or 'random', not '{text}'.");
            return seed;
        }

        /// <summary>Where a result file goes: as given if absolute, otherwise in the results folder next to the program.</summary>
        private static string OutputPath(string name) => Path.IsPathRooted(name) ? name : Path.Combine(ResultsDir, name);

        /// <summary>An input file: as given if it exists from the current folder, otherwise in <paramref name="fallbackDir"/>.</summary>
        private static string ResolveInput(string name, string fallbackDir) =>
            Path.IsPathRooted(name) || File.Exists(name) ? Path.GetFullPath(name) : Path.Combine(fallbackDir, name);

        /// <summary>The deals of a dataset file, checked against the line-up (X8).</summary>
        private static async Task<List<List<int>[]>> LoadDataset(string name, int seats, int dealsNeeded)
        {
            string path = ResolveInput(name, ProgramDir);
            if (!File.Exists(path))
                throw new UsageException($"Dataset file not found: {path}");
            int players;
            List<List<int>[]> deals;
            try
            {
                (players, deals) = await Filer.ReadGameDatasetFromFile(path);
            }
            catch (InvalidDataException ex)
            {
                throw new UsageException(ex.Message);
            }
            if (players != seats)
                throw new UsageException($"{path} deals are for {players} players, but the line-up has {seats}.");
            if (deals.Count < dealsNeeded)
                throw new UsageException($"{path} has {deals.Count} deals; {dealsNeeded} are needed.");
            return deals;
        }
    }
}
