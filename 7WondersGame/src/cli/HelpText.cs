namespace _7WondersGame.src.cli
{
    /// <summary>The text printed by 'help' and 'help &lt;command&gt;'.</summary>
    public static class HelpText
    {
        public static readonly string[] Topics = { "run", "replay", "analyze", "dataset", "agents" };

        public static string For(string? topic) => topic?.ToLowerInvariant() switch
        {
            null or "" or "help" => General,
            "run" => Run,
            "replay" => Replay,
            "analyze" => Analyze,
            "dataset" => Dataset,
            "agents" => Commands.AgentsText(),
            _ => $"No help for '{topic}'.\n\n" + General,
        };

        private const string Exe = "7WondersGame";

        public static readonly string General = $@"7 Wonders game simulator.

Usage:
  {Exe} <command> [options]
  {Exe} help <command>          details and examples for one command

Commands:
  run       Play games and append one row per player to a results CSV.
  replay    Play one game again and print what happens, step by step.
  analyze   Win rates with confidence intervals, and paired differences, from a results CSV.
  dataset   Write a file of pre-shuffled deals for 'run --dataset'.
  agents    List the agents and their settings.

Options are written --name value (or --name=value). Every option a command
needs must be given: nothing runs on hidden defaults.

Logging (all commands): --log <level>, or the {Logging.EnvVar} environment variable.
  Levels: {Logging.Levels}.
  info shows the run and one line per game; debug adds the step-by-step trace of
  each game; verbose adds playable cards, every purchase tried and the deck order.
  Without either, a Debug build logs at debug and a Release build at info.
  'replay' always traces (debug) unless --log says otherwise.

Exit codes: 0 done, 1 some games failed, 2 bad command line.

Example:
  {Exe} run --players HeuristicAI,RandomChoiceAI,RandomChoiceAI --games 100 --seed 42 --parallel 4
";

        public static readonly string Run = $@"run: play games and append the results to a CSV, one row per player.

  {Exe} run --players <agents> --games <n> --seed <seed> [options]
  {Exe} run --experiment <file.json> [--parallel <n>] [--out <file>] [--log <level>]

Required:
  --players <agents>  The agent in each seat, in seat order, comma-separated (3 to 7).
                      Each one is Name[:setting=value...], e.g. MctsAgent:C=1.2:Iterations=2000
                      (setting names in any case; n = Iterations, eps = Epsilon).
                      '{Exe} agents' lists every agent and setting. MctsAgent: 3-4 players.
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
  --out <file>        Results CSV. Default results.csv. A relative name goes in the results
                      folder next to the program. Rows are appended to an existing file (it must
                      have the same columns). <runId>.run.json beside it records the command,
                      seed, settings and git commit.
  --dataset <file>    Take the deals from a dataset file ('{Exe} help dataset') instead of the
                      seed: game i uses deal i. Agents' random choices still come from the seed.
                      The file's first line says its player count, which must match --players.
  --log <level>       {Logging.Levels}. debug traces every game; for one game use 'replay'.

Experiment file (--experiment): a JSON object with the same option names, so the run is
described by a file that is kept with its results (copied to <runId>.experiment.json next to
the CSV). players may be a list. A relative dataset name is looked for next to the file
first. ""description"" is free text for the manifest. Comments are allowed. Only --parallel,
--out and --log may also be given on the command line (they win over the file).
  {{
    ""description"": ""MCTS C sweep against two heuristic players"",
    ""players"": [""MctsAgent:C=0.3..2"", ""HeuristicAI"", ""HeuristicAI""],
    ""steps"": 6,
    ""games"": 50,
    ""seed"": 7,
    ""rotate"": true,
    ""out"": ""c-sweep.csv""
  }}
Games played: games x steps (with a range) x players (with rotate).

Examples:
  Heuristic against random, 1000 games, 8 at a time:
    {Exe} run --players HeuristicAI,RandomChoiceAI,RandomChoiceAI --games 1000 --seed 1 --parallel 8
  MCTS against two heuristic players, then the cheating (perfect information) version:
    {Exe} run --players MctsAgent:n=1000,HeuristicAI,HeuristicAI --games 100 --seed 1 --parallel 8
    {Exe} run --players MctsAgent:n=1000:Information=PerfectInformation,HeuristicAI,HeuristicAI --games 100 --seed 1 --parallel 8
  Sweep MCTS's C from 0.3 to 2 in 6 values, 50 deals each (300 games), to its own file:
    {Exe} run --players MctsAgent:C=0.3..2,HeuristicAI,HeuristicAI --steps 6 --games 50 --seed 7 --parallel 8 --out c-sweep.csv
  The same sweep in every seat (900 games), from a file:
    {Exe} run --experiment experiments/example.json --parallel 8
";

        public static readonly string Replay = $@"replay: play one game again and print what happens, step by step.

From a results CSV (the agents, settings and seed come from the rows):
  {Exe} replay --from <results.csv> --run <runId> --game <gameIndex>

From a seed:
  {Exe} replay --players <agents> --game-seed <seed> [--dataset <file> --deal <n>]

  --from <file>       A results CSV (a relative name is looked for in the current folder, then
                      in the results folder). If the run used a dataset, its <runId>.run.json
                      says which, and that file is used.
  --run <runId>       The runId column.
  --game <n>          The gameIndex column.
  --players <agents>  As in 'run', single values only.
  --game-seed <seed>  The seed column of the CSV (decimal or 0x hex). A failed game's log line
                      prints the full replay command.
  --dataset <file>    Deal from a dataset file instead; needs --deal (the dealIndex column).
  --log <level>       Default debug. verbose adds playable cards, purchases and the deck order.

The trace shows: setup (seed, agents, wonders), each turn's hands and coins, every choice,
what each build or discard cost and paid to whom, wonder effects, special turns
(Babylon B, Halikarnassos, Olympia), hand passing, battles and the final score per category.
With --from, the final scores are checked against the CSV. Nothing is written to the CSV.
To keep the trace, redirect it: ... > trace.txt

Examples:
  {Exe} replay --from results.csv --run 20261109-191254-0000000000003039 --game 12
  {Exe} replay --players HeuristicAI,RandomChoiceAI,RandomChoiceAI --game-seed 0x9e3779b97f4a7c15 --log verbose
";

        public static readonly string Analyze = $@"analyze: summarize a results CSV: who wins how often, and which differences are more than chance.

  {Exe} analyze --from <results.csv> [--run <runId>[,<runId>...] | --run all] [--by <grouping>] [--vs <n>] [--out <file.csv>]

  --from <file>       A results CSV (a relative name is looked for in the current folder, then in
                      the results folder).
  --run <ids>         The runs to include (the runId column). Needed when the file has more than
                      one run; 'all' takes every run (only if they are the same kind of experiment).
  --by <grouping>     player (default): each agent configuration is a group, named by the agent and
                      the settings that differ (e.g. MctsAgent C=0.3); agent: one group per agent
                      name; seat; wonder (board and side).
  --vs <n>            Compare every group with group n only (e.g. the baseline). Default: all pairs,
                      if there are at most 8 groups (otherwise only with --vs).
  --out <file.csv>    Also write the group table there, and the pairs to <file>.pairs.csv (a relative
                      name goes next to the results CSV).
  --log <level>       {Logging.Levels}.

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
  {Exe} analyze --from c-sweep.csv
  {Exe} analyze --from results.csv --run 20261109-191254-0000000000003039 --by agent
  {Exe} analyze --from c-sweep.csv --vs 1 --out c-sweep-summary.csv
";

        public static readonly string Dataset = $@"dataset: write pre-shuffled deals (decks and wonders) to a file, for 'run --dataset'.

  {Exe} dataset --seats <n> --games <n> --seed <seed> [--out <file>]

  --seats <n>         Number of players, 3 to 7.
  --games <n>         Number of deals.
  --seed <seed>       Deal i is the deal 'run --seed <seed>' plays as game i.
  --out <file>        Default gameDataset_<seats>p_<games>g.txt in the program folder.

The file starts with 'Players: <seats>'. When a run reads it, the count must match the line-up
and every deal is checked (deck sizes, card copies for that player count, guilds, wonders).
A file made before this header existed needs it added as its first line.

Example:
  {Exe} dataset --seats 3 --games 100 --seed 5
";
    }
}
