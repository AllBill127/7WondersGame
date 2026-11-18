using System.Text.Json;
using _7WondersGame.Cli.Agents;
using _7WondersGame.Cli.Arguments;
using _7WondersGame.Cli.CommandLine;

namespace _7WondersGame.Cli.Experiments;

/// <summary>
/// What 'run' plays, from the command line or from an experiment file (<c>--experiment file.json</c>, which uses the
/// same option names). Game <c>i</c> always gets the same deal, seating and sweep value.
/// </summary>
public sealed class RunPlan
{
    /// <summary>The options 'run' takes.</summary>
    public static readonly string[] Options =
        { "players", "games", "seed", "steps", "rotate", "dataset", "parallel", "output", "log-level", "experiment" };

    /// <summary>
    /// The options that may go with --experiment: they say where and how fast it runs, not what it plays.
    /// </summary>
    public static readonly string[] MachineOptions = { "parallel", "output", "log-level" };

    /// <summary>The keys of an experiment file: the options (except experiment) and a free-text description.</summary>
    public static readonly string[] FileKeys = Options
        .Where(option => option != "experiment")
        .Append("description")
        .ToArray();

    private const string DefaultOutput = "results.csv";

    /// <summary>The line-up as listed: the agent in each seat when not rotated.</summary>
    public List<AgentSpec> Players { get; init; } = new();

    /// <summary>Number of deals (per sweep value).</summary>
    public int Games { get; init; }

    /// <summary>The run's seed: game <c>i</c>'s deal comes from it and the deal number.</summary>
    public ulong Seed { get; init; }

    /// <summary>The setting that is a range, if any.</summary>
    public Sweep? Sweep { get; init; }

    /// <summary>Plays each deal once per rotation of the line-up, so every agent sits in every seat.</summary>
    public bool Rotate { get; init; }

    /// <summary>
    /// A dataset file, as given (a relative name in an experiment file is made absolute if it is next to the file).
    /// </summary>
    public string? Dataset { get; init; }

    /// <summary>How many games run at a time.</summary>
    public int Parallel { get; init; } = 1;

    /// <summary>The results CSV, as given.</summary>
    public string Output { get; init; } = DefaultOutput;

    /// <summary>The log level, as given, or null for the default.</summary>
    public string? LogLevel { get; init; }

    /// <summary>The experiment file (full path), if the plan came from one.</summary>
    public string? Experiment { get; init; }

    /// <summary>The experiment file's free-text description, if it has one.</summary>
    public string? Description { get; init; }

    /// <summary>How many seatings each deal is played in.</summary>
    public int Rotations
    {
        get
        {
            if (Rotate)
            {
                return Players.Count;
            }
            return 1;
        }
    }

    /// <summary>How many values the range setting takes (1 without one).</summary>
    public int Steps
    {
        get
        {
            if (Sweep == null)
            {
                return 1;
            }
            return Sweep.Steps;
        }
    }

    /// <summary>How many games the run plays.</summary>
    public int Total
    {
        get
        {
            return Games * Rotations * Steps;
        }
    }

    /// <summary>
    /// Game <paramref name="index"/>: sweep values outermost, then deals, then rotations, so the rotations of a deal
    /// are neighbours. Without rotation it is game = step * games + deal.
    /// </summary>
    public GameSlot At(int index)
    {
        if (index < 0 || index >= Total)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int perStep = Games * Rotations;
        int step = index / perStep;
        int positionInStep = index % perStep;
        double? value = null;
        if (Sweep != null)
        {
            value = Sweep.ValueAt(step);
        }
        return new GameSlot(index, step, positionInStep / Rotations, positionInStep % Rotations, value);
    }

    /// <summary>
    /// The agent in each seat for a rotation: the line-up moved <paramref name="rotation"/> seats on, so the first
    /// listed agent sits in seat <paramref name="rotation"/>.
    /// </summary>
    public List<AgentSpec> SeatsFor(int rotation)
    {
        var seats = new List<AgentSpec>(Players.Count);
        for (int seat = 0; seat < Players.Count; seat++)
        {
            int listedPosition = ListedPositionInSeat(seat, rotation);
            seats.Add(Players[listedPosition]);
        }
        return seats;
    }

    /// <summary>The position in the line-up as listed of the agent that sits in <paramref name="seat"/>.</summary>
    private int ListedPositionInSeat(int seat, int rotation)
    {
        int playerCount = Players.Count;
        return ((seat - rotation) % playerCount + playerCount) % playerCount;
    }

    /// <summary>The plan the command line gives, or the experiment file it names.</summary>
    public static RunPlan FromCli(CliArguments cli)
    {
        cli.AllowOnly(Options);
        string? experiment = null;
        string? description = null;
        if (cli.Has("experiment"))
        {
            CheckOnlyMachineOptionsWithExperiment(cli);
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
            Rotate = cli.GetYesNoOrDefault("rotate", false),
            Dataset = cli.Get("dataset"),
            Parallel = cli.GetIntOrDefault("parallel", 1, 1),
            Output = cli.GetOrDefault("output", DefaultOutput),
            LogLevel = cli.Get("log-level"),
            Experiment = experiment,
            Description = description,
        };
    }

    /// <summary>The file sets what the run plays, so the command line may only add the machine options.</summary>
    private static void CheckOnlyMachineOptionsWithExperiment(CliArguments cli)
    {
        foreach (string name in cli.Names)
        {
            bool isExperiment = name.Equals("experiment", StringComparison.OrdinalIgnoreCase);
            bool isMachineOption = MachineOptions.Contains(name, StringComparer.OrdinalIgnoreCase);
            if (!isExperiment && !isMachineOption)
            {
                throw new UsageException(
                    $"--{name} can't go with --experiment: the file sets it. " +
                    "Only --parallel, --output and --log-level can.",
                    "run");
            }
        }
    }

    /// <summary>The line-up of <c>--players</c>: 3 to 7 agents.</summary>
    public static List<AgentSpec> ParsePlayers(string text, string command)
    {
        List<AgentSpec> specs = AgentSpec.ParseList(text);
        if (specs.Count < 3 || specs.Count > 7)
        {
            throw new UsageException($"--players lists {specs.Count} agents; a game has 3 to 7 players.", command);
        }
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
        {
            throw new UsageException($"Experiment file not found: {path}", "run");
        }

        using (JsonDocument doc = ParseExperimentJson(path))
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new UsageException(
                    $"{path} must hold one JSON object, e.g. {{ \"players\": [...], \"games\": 100, \"seed\": 1 }}.",
                    "run");
            }

            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? description = null;
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                string key = FileKeyFor(property.Name, path);
                bool isRepeated = options.ContainsKey(key) || (key == "description" && description != null);
                if (isRepeated)
                {
                    throw new UsageException($"{path}: '{key}' is given more than once.", "run");
                }
                if (property.Value.ValueKind == JsonValueKind.Null)
                {
                    continue;
                }

                string value = OptionText(property.Value, key, path);
                if (key == "description")
                {
                    description = value;
                    continue;
                }
                if (key == "dataset")
                {
                    value = DatasetBesideFile(value, path);
                }
                options[key] = value;
            }
            return (options, description);
        }
    }

    private static JsonDocument ParseExperimentJson(string path)
    {
        var jsonOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        };
        try
        {
            return JsonDocument.Parse(File.ReadAllText(path), jsonOptions);
        }
        catch (JsonException exception)
        {
            throw new UsageException($"{path} is not valid JSON: {exception.Message}", "run");
        }
    }

    /// <summary>The option a key of the file names (any case), or a usage error.</summary>
    private static string FileKeyFor(string name, string path)
    {
        foreach (string key in FileKeys)
        {
            if (key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }
        throw new UsageException($"{path}: unknown key '{name}'. Known: {string.Join(", ", FileKeys)}.", "run");
    }

    /// <summary>A value of the file as the command line would write it.</summary>
    private static string OptionText(JsonElement value, string key, string path)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                return value.GetString()!;
            case JsonValueKind.Number:
                return value.GetRawText();
            case JsonValueKind.True:
                return "yes";
            case JsonValueKind.False:
                return "no";
            case JsonValueKind.Array:
                if (key == "players")
                {
                    return PlayersText(value, path);
                }
                break;
        }
        throw new UsageException($"{path}: '{key}' should be a string, number or true/false.", "run");
    }

    /// <summary>A players list of the file as one comma-separated line-up.</summary>
    private static string PlayersText(JsonElement players, string path)
    {
        var specs = new List<string>();
        foreach (JsonElement player in players.EnumerateArray())
        {
            if (player.ValueKind != JsonValueKind.String)
            {
                throw new UsageException(
                    $"{path}: each player is a string like \"HeuristicAgent\" or " +
                    "\"MctsAgent:ExplorationConstant=1.2\".",
                    "run");
            }
            specs.Add(player.GetString()!);
        }
        return string.Join(",", specs);
    }

    /// <summary>A relative dataset name that is a file next to the experiment file, as a full path.</summary>
    private static string DatasetBesideFile(string dataset, string path)
    {
        if (Path.IsPathRooted(dataset))
        {
            return dataset;
        }
        string beside = Path.Combine(Path.GetDirectoryName(path)!, dataset);
        if (File.Exists(beside))
        {
            return Path.GetFullPath(beside);
        }
        return dataset;
    }
}
