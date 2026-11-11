using System.Text.Json;

namespace _7WondersGame.src.cli
{
    /// <summary>Which deal, seating and sweep value game <see cref="Index"/> of a run plays.</summary>
    /// <param name="Rotation">The line-up moved this many seats on (0 = as listed).</param>
    /// <param name="Value">The value of the range setting, if there is one.</param>
    public readonly record struct GameSlot(int Index, int Step, int Deal, int Rotation, double? Value);

    /// <summary>
    /// What 'run' plays (step 7, B3), from the command line or from an experiment file (<c>--experiment file.json</c>,
    /// which uses the same option names). Game <c>i</c> always gets the same deal, seating and sweep value.
    /// </summary>
    public sealed class RunPlan
    {
        /// <summary>The options 'run' takes.</summary>
        public static readonly string[] Options =
            { "players", "games", "seed", "steps", "rotate", "dataset", "parallel", "out", "log", "experiment" };

        /// <summary>The options that may go with --experiment: they say where and how fast it runs, not what it plays.</summary>
        public static readonly string[] MachineOptions = { "parallel", "out", "log" };

        /// <summary>The keys of an experiment file: the options (except experiment) and a free-text description.</summary>
        public static readonly string[] FileKeys = Options.Where(o => o != "experiment").Append("description").ToArray();

        /// <summary>The line-up as listed: the agent in each seat when not rotated.</summary>
        public List<AgentSpec> Players { get; init; } = new();
        /// <summary>Number of deals (per sweep value).</summary>
        public int Games { get; init; }
        public ulong Seed { get; init; }
        public Sweep? Sweep { get; init; }
        /// <summary>Plays each deal once per rotation of the line-up, so every agent sits in every seat.</summary>
        public bool Rotate { get; init; }
        /// <summary>A dataset file, as given (a relative name in an experiment file is made absolute if it is next to the file).</summary>
        public string? Dataset { get; init; }
        public int Parallel { get; init; } = 1;
        public string Out { get; init; } = "results.csv";
        public string? Log { get; init; }
        /// <summary>The experiment file (full path), if the plan came from one.</summary>
        public string? Experiment { get; init; }
        public string? Description { get; init; }

        public int Rotations => Rotate ? Players.Count : 1;
        public int Steps => Sweep?.Steps ?? 1;
        public int Total => Games * Rotations * Steps;

        /// <summary>
        /// Game <paramref name="index"/>: sweep values outermost, then deals, then rotations, so the rotations of a deal
        /// are neighbours. Without rotation it is the numbering of step 3b (game = step * games + deal).
        /// </summary>
        public GameSlot At(int index)
        {
            if (index < 0 || index >= Total)
                throw new ArgumentOutOfRangeException(nameof(index));
            int perStep = Games * Rotations;
            int step = index / perStep, rest = index % perStep;
            return new GameSlot(index, step, rest / Rotations, rest % Rotations, Sweep?.ValueAt(step));
        }

        /// <summary>The agent in each seat for a rotation: the line-up moved <paramref name="rotation"/> seats on, so the first listed agent sits in seat <paramref name="rotation"/>.</summary>
        public List<AgentSpec> SeatsFor(int rotation)
        {
            int n = Players.Count;
            return Enumerable.Range(0, n).Select(seat => Players[((seat - rotation) % n + n) % n]).ToList();
        }

        public static RunPlan FromCli(CliArgs cli)
        {
            cli.AllowOnly(Options);
            string? experiment = null, description = null;
            if (cli.Has("experiment"))
            {
                foreach (string name in cli.Names)
                    if (!name.Equals("experiment", StringComparison.OrdinalIgnoreCase) &&
                        !MachineOptions.Contains(name, StringComparer.OrdinalIgnoreCase))
                        throw new UsageException(
                            $"--{name} can't go with --experiment: the file sets it. Only --parallel, --out and --log can.", "run");
                experiment = Path.GetFullPath(cli.Get("experiment")!);
                (Dictionary<string, string> fileOptions, description) = LoadExperiment(experiment);
                cli = cli.WithDefaults(fileOptions);
            }

            List<AgentSpec> players = ParsePlayers(cli.Require("players"), "run");
            int? steps = cli.GetInt("steps", 2);
            return new RunPlan
            {
                Players = players,
                Games = cli.RequireInt("games", 1),
                Seed = Commands.ParseRunSeed(cli.Require("seed")),
                Sweep = AgentCatalog.Validate(players, steps),
                Rotate = cli.GetYesNo("rotate") ?? false,
                Dataset = cli.Get("dataset"),
                Parallel = cli.GetInt("parallel", 1) ?? 1,
                Out = cli.Get("out") ?? "results.csv",
                Log = cli.Get("log"),
                Experiment = experiment,
                Description = description,
            };
        }

        public static List<AgentSpec> ParsePlayers(string text, string command)
        {
            List<AgentSpec> specs = AgentSpec.ParseList(text);
            if (specs.Count is < 3 or > 7)
                throw new UsageException($"--players lists {specs.Count} agents; a game has 3 to 7 players.", command);
            return specs;
        }

        /// <summary>
        /// An experiment file's options as command-line text: a JSON object whose keys are the option names
        /// (<see cref="FileKeys"/>, any case). players may be a list; true/false mean yes/no.
        /// A relative dataset name is looked for next to the file first.
        /// </summary>
        public static (Dictionary<string, string> Options, string? Description) LoadExperiment(string path)
        {
            if (!File.Exists(path))
                throw new UsageException($"Experiment file not found: {path}", "run");

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(File.ReadAllText(path),
                    new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            }
            catch (JsonException ex)
            {
                throw new UsageException($"{path} is not valid JSON: {ex.Message}", "run");
            }

            using (doc)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    throw new UsageException($"{path} must hold one JSON object, e.g. {{ \"players\": [...], \"games\": 100, \"seed\": 1 }}.", "run");

                var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string? description = null;
                foreach (JsonProperty p in doc.RootElement.EnumerateObject())
                {
                    string key = FileKeys.FirstOrDefault(k => k.Equals(p.Name, StringComparison.OrdinalIgnoreCase))
                        ?? throw new UsageException($"{path}: unknown key '{p.Name}'. Known: {string.Join(", ", FileKeys)}.", "run");
                    if (options.ContainsKey(key) || key == "description" && description != null)
                        throw new UsageException($"{path}: '{key}' is given more than once.", "run");
                    if (p.Value.ValueKind == JsonValueKind.Null)
                        continue;

                    string value = p.Value.ValueKind switch
                    {
                        JsonValueKind.String => p.Value.GetString()!,
                        JsonValueKind.Number => p.Value.GetRawText(),
                        JsonValueKind.True => "yes",
                        JsonValueKind.False => "no",
                        JsonValueKind.Array when key == "players" => string.Join(",", p.Value.EnumerateArray().Select(e =>
                            e.ValueKind == JsonValueKind.String
                                ? e.GetString()!
                                : throw new UsageException($"{path}: each player is a string like \"HeuristicAI\" or \"MctsAgent:C=1.2\".", "run"))),
                        _ => throw new UsageException($"{path}: '{key}' should be a string, number or true/false.", "run"),
                    };
                    if (key == "description")
                    {
                        description = value;
                        continue;
                    }
                    if (key == "dataset" && !Path.IsPathRooted(value))
                    {
                        string beside = Path.Combine(Path.GetDirectoryName(path)!, value);
                        if (File.Exists(beside))
                            value = Path.GetFullPath(beside);
                    }
                    options[key] = value;
                }
                return (options, description);
            }
        }
    }
}
