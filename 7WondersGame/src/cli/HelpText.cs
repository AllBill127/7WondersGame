namespace _7WondersGame.src.cli
{
    /// <summary>The text printed by 'help' and 'help &lt;command&gt;'.</summary>
    public static class HelpText
    {
        public static readonly string[] Topics = { "run", "replay", "dataset", "agents" };

        public static string For(string? topic) => topic?.ToLowerInvariant() switch
        {
            null or "" or "help" => General,
            "run" => Run,
            "replay" => Replay,
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
  {Exe} run --players DeterministicAI,RandomChoiceAI,MCTSAIv10:c=1.5 --games 100 --seed 42 --parallel 4
";

        public static readonly string Run = $@"run: play games and append the results to a CSV, one row per player.

  {Exe} run --players <agents> --games <n> --seed <seed> [options]

Required:
  --players <agents>  The agent in each seat, in seat order, comma-separated (3 to 7).
                      Each one is Name[:setting=value...], e.g. MCTSAIv10:c=1.5:iterations=2000.
                      Short setting names: c (EXPLORATION_WEIGHT), cwl (EXPLORATION_WEIGHT_WL),
                      iterations (MaxIterations). '{Exe} agents' lists every agent and setting.
                      One setting may be a range a..b (see --steps).
  --games <n>         Number of deals to play (with a range: per value).
  --seed <seed>       Run seed: decimal or 0x hex, or 'random' for a new one (it is printed and saved).
                      The deal and every random choice of game i come from it, so the same
                      command plays the same games again, whatever --parallel is.

Optional:
  --steps <n>         With a range a..b: the number of evenly spaced values, both ends included.
                      Every value plays the same deals, so values are compared on equal games.
                      The value each game used is in the configJson column.
  --parallel <n>      Games played at the same time. Default 1.
  --out <file>        Results CSV. Default results.csv. A relative name goes in the results
                      folder next to the program. Rows are appended to an existing file (it must
                      have the same columns). <runId>.run.json beside it records the command,
                      seed, settings and git commit.
  --dataset <file>    Take the deals from a dataset file ('{Exe} help dataset') instead of the
                      seed: game i uses deal i. Agents' random choices still come from the seed.
  --log <level>       {Logging.Levels}. debug traces every game; for one game use 'replay'.

Examples:
  Heuristic against random, 1000 games, 8 at a time:
    {Exe} run --players DeterministicAI,RandomChoiceAI,RandomChoiceAI --games 1000 --seed 1 --parallel 8
  MCTS with a fixed C and a smaller budget:
    {Exe} run --players DeterministicAI,RandomChoiceAI,MCTSAIv10:c=1.5:cwl=1:iterations=2000 --games 100 --seed 42 --parallel 4
  Sweep C from 0 to 2 in 11 values, 50 deals each (550 games), to its own file:
    {Exe} run --players DeterministicAI,RandomChoiceAI,MCTSAIv10:c=0..2:cwl=1 --steps 11 --games 50 --seed 7 --parallel 8 --out c-sweep.csv
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
  {Exe} replay --players DeterministicAI,RandomChoiceAI,RandomChoiceAI --game-seed 0x9e3779b97f4a7c15 --log verbose
";

        public static readonly string Dataset = $@"dataset: write pre-shuffled deals (decks and wonders) to a file, for 'run --dataset'.

  {Exe} dataset --seats <n> --games <n> --seed <seed> [--out <file>]

  --seats <n>         Number of players, 3 to 7.
  --games <n>         Number of deals.
  --seed <seed>       Deal i is the deal 'run --seed <seed>' plays as game i.
  --out <file>        Default gameDataset_<seats>p_<games>g.txt in the program folder.

Example:
  {Exe} dataset --seats 3 --games 100 --seed 5
";
    }
}
