using _7WondersGame.Cli.Logs;

namespace _7WondersGame.Cli.CommandLine;

/// <summary>The text printed by 'help' and 'help &lt;command&gt;'.</summary>
public static class HelpText
{
    /// <summary>The commands that have their own help.</summary>
    public static readonly string[] Topics = { "run", "replay", "analyze", "dataset", "agents" };

    private const string ExecutableName = "7WondersGame";

    /// <summary>The help for <paramref name="topic"/> (a command, any case), or the general help.</summary>
    public static string For(string? topic)
    {
        if (topic == null)
        {
            return General;
        }
        switch (topic.ToLowerInvariant())
        {
            case "":
            case "help":
                return General;
            case "run":
                return Run;
            case "replay":
                return Replay;
            case "analyze":
                return Analyze;
            case "dataset":
                return Dataset;
            case "agents":
                return Commands.AgentsText();
            default:
                return $"No help for '{topic}'.\n\n" + General;
        }
    }

    /// <summary>The general help: the commands, options, logging and exit codes.</summary>
    public static readonly string General = $@"7 Wonders game simulator.

Usage:
  {ExecutableName} <command> [options]
  {ExecutableName} help <command>          details and examples for one command

Commands:
  run       Play games and append one row per player to a results CSV.
  replay    Play one game again and print what happens, step by step.
  analyze   Win rates with confidence intervals, and paired differences, from a results CSV.
  dataset   Write a file of pre-shuffled deals for 'run --dataset'.
  agents    List the agents and their settings.

Options are written --name value (or --name=value). Every option a command
needs must be given: nothing runs on hidden defaults.

Logging (all commands): --log-level <level>, or the {Logging.EnvironmentVariable} environment variable.
  Levels: {Logging.Levels}.
  info shows the run and one line per game; debug adds the step-by-step trace of
  each game; verbose adds playable cards, every purchase tried and the deck order.
  Without either, a Debug build logs at debug and a Release build at info.
  'replay' always traces (debug) unless --log-level says otherwise.

Exit codes: 0 done, 1 some games failed, 2 bad command line.

Example:
  {ExecutableName} run --players HeuristicAgent,RandomAgent,RandomAgent --games 100 --seed 42 --parallel 4
";

    /// <summary>The help of 'run'.</summary>
    public static readonly string Run = $@"run: play games and append the results to a CSV, one row per player.

  {ExecutableName} run --players <agents> --games <n> --seed <seed> [options]
  {ExecutableName} run --experiment <file.json> [--parallel <n>] [--output <file>] [--log-level <level>]

Required:
  --players <agents>  The agent in each seat, in seat order, comma-separated (3 to 7).
                      Each one is Name[:setting=value...], e.g.
                      MctsAgent:ExplorationConstant=1.2:IterationBudget=2000 (setting names in any case).
                      '{ExecutableName} agents' lists every agent and setting. MctsAgent: 3-4 players.
                      One setting may be a range a..b (see --steps).
  --games <n>         Number of deals to play (with a range: per value).
  --seed <seed>       Run seed: decimal or 0x hex, or 'random' for a new one (it is printed and saved).
                      The deal and every random choice of game i come from it, so the same
                      command plays the same games again, whatever --parallel is.

Optional:
  --steps <n>         With a range a..b: the number of evenly spaced values, both ends included.
                      Every value plays the same deals, so values are compared on equal games.
                      The value each game used is in the configJson column.
  --rotate yes|no     yes: play each deal once per rotation of the line-up (as many seatings as
                      players), so every agent sits in every seat on the same deals and seat
                      advantage cancels out. Each seat keeps its wonder; the agents move on one
                      seat per rotation. The games of one deal have the same dealIndex. Default no.
  --parallel <n>      Games played at the same time. Default 1.
  --output <file>        Results CSV. Default results.csv. A relative name goes in the results
                      folder next to the program. Rows are appended to an existing file (it must
                      have the same columns). <runId>.run.json beside it records the command,
                      seed, settings and git commit.
  --dataset <file>    Take the deals from a dataset file ('{ExecutableName} help dataset') instead of the
                      seed: game i uses deal i. Agents' random choices still come from the seed.
                      The file's first line says its player count, which must match --players.
  --log-level <level>       {Logging.Levels}. debug traces every game; for one game use 'replay'.

Experiment file (--experiment): a JSON object with the same option names, so the run is
described by a file that is kept with its results (copied to <runId>.experiment.json next to
the CSV). players may be a list. A relative dataset name is looked for next to the file
first. ""description"" is free text for the manifest. Comments are allowed. Only --parallel,
--output and --log-level may also be given on the command line (they win over the file).
  {{
    ""description"": ""MCTS exploration constant sweep against two heuristic players"",
    ""players"": [""MctsAgent:ExplorationConstant=0.3..2"", ""HeuristicAgent"", ""HeuristicAgent""],
    ""steps"": 6,
    ""games"": 50,
    ""seed"": 7,
    ""rotate"": true,
    ""output"": ""c-sweep.csv""
  }}
Games played: games x steps (with a range) x players (with rotate).

Examples:
  Heuristic against random, 1000 games, 8 at a time:
    {ExecutableName} run --players HeuristicAgent,RandomAgent,RandomAgent --games 1000 --seed 1 --parallel 8
  MCTS against two heuristic players, then the cheating (perfect information) version:
    {ExecutableName} run --players MctsAgent:IterationBudget=1000,HeuristicAgent,HeuristicAgent
        --games 100 --seed 1 --parallel 8
    {ExecutableName} run
        --players MctsAgent:IterationBudget=1000:HiddenInformation=PerfectInformation,HeuristicAgent,HeuristicAgent
        --games 100 --seed 1 --parallel 8
  Sweep MCTS's exploration constant from 0.3 to 2 in 6 values, 50 deals each (300 games), to its own file:
    {ExecutableName} run --players MctsAgent:ExplorationConstant=0.3..2,HeuristicAgent,HeuristicAgent
        --steps 6 --games 50 --seed 7 --parallel 8 --output c-sweep.csv
  The same sweep in every seat (900 games), from a file:
    {ExecutableName} run --experiment experiments/example.json --parallel 8
  One search on 4 threads (Parallelisation = Root, Leaf or Tree), 500 ms a move, one game at a time:
    {ExecutableName} run
        --players MctsAgent:Parallelisation=Root:ParallelWorkerCount=4:BudgetKind=Time:TimeBudgetMilliseconds=500,
                  HeuristicAgent,HeuristicAgent
        --games 50 --seed 1
  (Each example is one command line: the line breaks are only for this help.)
";

    /// <summary>The help of 'replay'.</summary>
    public static readonly string Replay = $@"replay: play one game again and print what happens, step by step.

From a results CSV (the agents, settings and seed come from the rows):
  {ExecutableName} replay --from <results.csv> --run <runId> --game <gameIndex>

From a seed:
  {ExecutableName} replay --players <agents> --game-seed <seed> [--dataset <file> --deal <n>]

  --from <file>       A results CSV (a relative name is looked for in the current folder, then
                      in the results folder). If the run used a dataset, its <runId>.run.json
                      says which, and that file is used.
  --run <runId>       The runId column.
  --game <n>          The gameIndex column.
  --players <agents>  As in 'run', single values only.
  --game-seed <seed>  The seed column of the CSV (decimal or 0x hex). A failed game's log line
                      prints the full replay command.
  --dataset <file>    Deal from a dataset file instead; needs --deal (the dealIndex column).
  --log-level <level>       Default debug. verbose adds playable cards, purchases and the deck order.

The trace shows: setup (seed, agents, wonders), each turn's hands and coins, every choice,
what each build or discard cost and paid to whom, wonder effects, special turns
(Babylon B, Halikarnassos, Olympia), hand passing, battles and the final score per category.
With --from, the final scores are checked against the CSV. Nothing is written to the CSV.
To keep the trace, redirect it: ... > trace.txt

Examples:
  {ExecutableName} replay --from results.csv --run 20261109-191254-0000000000003039 --game 12
  {ExecutableName} replay --players HeuristicAgent,RandomAgent,RandomAgent --game-seed 0x9e3779b97f4a7c15
      --log-level verbose
";

    /// <summary>The help of 'analyze'.</summary>
    public static readonly string Analyze =
        $@"analyze: summarize a results CSV: who wins how often, and which differences are more than chance.

  {ExecutableName} analyze --from <results.csv> [--run <runId>[,<runId>...] | --run all] [--group-by <grouping>]
                [--compare-with <n>] [--output <file.csv>]

  --from <file>       A results CSV (a relative name is looked for in the current folder, then in
                      the results folder).
  --run <ids>         The runs to include (the runId column). Needed when the file has more than
                      one run; 'all' takes every run (only if they are the same kind of experiment).
  --group-by <grouping>     player (default): each agent configuration is a group, named by the agent and
                      the settings that differ (e.g. MctsAgent ExplorationConstant=0.3); agent: one group per agent
                      name; seat; wonder (board and side).
  --compare-with <n>            Compare every group with group n only (e.g. the baseline). Default: all pairs,
                      if there are at most 8 groups (otherwise only with --compare-with).
  --output <file.csv>    Also write the group table there, and the pairs to <file>.pairs.csv (a relative
                      name goes next to the results CSV).
  --log-level <level>       {Logging.Levels}.

Per group: games (rows), win rate (a k-way tie for first counts 1/k) with its 95% Wilson
interval, the fair share (1 / players: what an agent no better than the others gets), mean VP
+- its standard error, and mean rank.

Paired differences: for two groups, the deals both played (same seed column = same deal) are
matched. Per deal each group's VP and win share are averaged over its games, and the
differences A - B are averaged over the deals, with a 95% interval (mean +- 1.96 standard
errors). Pairing removes the luck of the deal, so it finds smaller differences than comparing
the two win rates. A * marks a difference whose interval leaves out 0. With many pairs some
will be marked by chance (about 1 in 20 when there is no difference).

Works for: line-ups in one game (head to head, best with --rotate yes), and sweeps (each value
of a range plays the same deals, so values are compared deal by deal).

Examples:
  {ExecutableName} analyze --from c-sweep.csv
  {ExecutableName} analyze --from results.csv --run 20261109-191254-0000000000003039 --group-by agent
  {ExecutableName} analyze --from c-sweep.csv --compare-with 1 --output c-sweep-summary.csv
";

    /// <summary>The help of 'dataset'.</summary>
    public static readonly string Dataset =
        $@"dataset: write pre-shuffled deals (decks and wonders) to a file, for 'run --dataset'.

  {ExecutableName} dataset --seats <n> --games <n> --seed <seed> [--output <file>]

  --seats <n>         Number of players, 3 to 7.
  --games <n>         Number of deals.
  --seed <seed>       Deal i is the deal 'run --seed <seed>' plays as game i.
  --output <file>        Default gameDataset_<seats>p_<games>g.txt in the program folder.

The file starts with 'Players: <seats>'. When a run reads it, the count must match the line-up
and every deal is checked (deck sizes, card copies for that player count, guilds, wonders).
A file made before this header existed needs it added as its first line.

Example:
  {ExecutableName} dataset --seats 3 --games 100 --seed 5
";
}
