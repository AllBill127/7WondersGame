using System.Diagnostics;
using System.Globalization;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Cli.Agents;
using _7WondersGame.Cli.Experiments;
using _7WondersGame.Cli.Logs;
using _7WondersGame.Cli.Results;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Randomness.Services;
using Serilog;

namespace _7WondersGame.Cli.CommandLine;

/// <summary>
/// Plays the games of a run, several at a time, and appends each finished game's rows to the results CSV. A game
/// that fails is logged with the command that replays it, and the others go on.
/// </summary>
internal sealed class GameRunner
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private readonly RunPlan _plan;
    private readonly string _runId;
    private readonly List<List<int>[]>? _deals;
    private readonly ResultsCsvWriter _writer;
    private readonly Stopwatch _runClock;
    private readonly object _setupLock = new();
    private int _completed;
    private int _failed;

    /// <param name="plan">What the run plays.</param>
    /// <param name="runId">The run's id, written in every row.</param>
    /// <param name="deals">The deals of the plan's dataset file, or null to deal from each game's seed.</param>
    /// <param name="writer">The results CSV.</param>
    /// <param name="runClock">Started when the run started: for the time left in the progress lines.</param>
    public GameRunner(RunPlan plan, string runId, List<List<int>[]>? deals, ResultsCsvWriter writer,
                      Stopwatch runClock)
    {
        _plan = plan;
        _runId = runId;
        _deals = deals;
        _writer = writer;
        _runClock = runClock;
    }

    /// <summary>How many games finished and were written.</summary>
    public int Completed
    {
        get
        {
            return _completed;
        }
    }

    /// <summary>How many games failed.</summary>
    public int Failed
    {
        get
        {
            return _failed;
        }
    }

    /// <summary>Plays every game of the plan, at most the plan's <see cref="RunPlan.Parallel"/> at a time.</summary>
    public void PlayAll()
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = _plan.Parallel };
        Parallel.For(0, _plan.Total, options, PlayGame);
    }

    /// <summary>
    /// Plays game <paramref name="i"/>. It always gets the same deal, seating, seed and sweep value, whatever the
    /// thread timing; every sweep value and every seating plays the same deals.
    /// </summary>
    private void PlayGame(int gameIndex)
    {
        GameSlot slot = _plan.At(gameIndex);
        List<AgentSpec> seats = _plan.SeatsFor(slot.Rotation);
        ILogger log = Logging.ForGame(gameIndex);
        Game game = SetUpGame(slot, seats, log);

        var watch = Stopwatch.StartNew();
        try
        {
            game.Loop(record: true);
            _writer.WriteGame(_runId, gameIndex, slot.Deal, game.Seed, game.Results!);
            int done = Interlocked.Increment(ref _completed);
            LogGameDone(log, slot, game.Results!, done, watch.Elapsed);
        }
        catch (Exception exception)
        {
            Interlocked.Increment(ref _failed);
            log.Error(exception, "Game failed (seed 0x{Seed}). To replay it: {Replay}", game.Seed.ToString("x16"),
                ReplayCommand(game, slot, seats));
        }
    }

    private Game SetUpGame(GameSlot slot, List<AgentSpec> seats, ILogger log)
    {
        lock (_setupLock)
        {
            var agents = new List<IAgent>(seats.Count);
            foreach (AgentSpec seat in seats)
            {
                agents.Add(AgentCatalog.Create(seat, slot.Value));
            }
            var game = new Game(Seeding.GameSeed(_plan.Seed, slot.Deal)) { Agents = agents, Trace = log };
            if (_deals != null)
            {
                game.SetUpGameFromDataset(_deals[slot.Deal]);
            }
            else
            {
                game.SetUpRandomGame();
            }
            return game;
        }
    }

    private void LogGameDone(ILogger log, GameSlot slot, List<SeatResult> results, int done, TimeSpan gameTime)
    {
        int total = _plan.Total;
        TimeSpan elapsed = _runClock.Elapsed;
        TimeSpan left = elapsed / done * (total - done);
        log.Information("{Done}/{Total} done in {Seconds:0.0}s{Value}: {Summary} | run {Elapsed}, about {Left} left",
            done, total, gameTime.TotalSeconds, SweepValueNote(slot.Value), Summary(results),
            Commands.Clock(elapsed), Commands.Clock(left));
    }

    /// <summary>The range setting's value in this game, for the progress line ("" without a range).</summary>
    private string SweepValueNote(double? value)
    {
        Sweep? sweep = _plan.Sweep;
        if (sweep == null)
        {
            return "";
        }
        return $" ({sweep.Setting}={value!.Value.ToString("0.####", InvariantCulture)})";
    }

    private static string Summary(IEnumerable<SeatResult> seats)
    {
        return string.Join(" | ", seats.Select(SummaryOf));
    }

    private static string SummaryOf(SeatResult seat)
    {
        return $"{seat.Seat} {seat.Agent} {seat.Wonder}{seat.Side} {seat.VictoryPoints}vp #{seat.Rank}";
    }

    /// <summary>The command that plays a failed game again, step by step.</summary>
    private string ReplayCommand(Game game, GameSlot slot, List<AgentSpec> seats)
    {
        var players = new List<string>(seats.Count);
        foreach (AgentSpec seat in seats)
        {
            players.Add(WithValue(seat, slot.Value));
        }
        string replay = $"replay --players {string.Join(",", players)} --game-seed 0x{game.Seed:x16}";
        if (_plan.Dataset != null)
        {
            replay += $" --dataset {_plan.Dataset} --deal {slot.Deal}";
        }
        return replay;
    }

    /// <summary>The spec with its range (if any) replaced by the value this game used.</summary>
    private static string WithValue(AgentSpec spec, double? value)
    {
        var settings = new List<KeyValuePair<string, string>>(spec.Settings.Count);
        foreach (KeyValuePair<string, string> setting in spec.Settings)
        {
            if (value != null && Sweep.TryParseRange(setting.Value, out _, out _))
            {
                string valueText = value.Value.ToString("R", InvariantCulture);
                settings.Add(new KeyValuePair<string, string>(setting.Key, valueText));
            }
            else
            {
                settings.Add(setting);
            }
        }
        return new AgentSpec(spec.Name, settings).ToString();
    }
}
