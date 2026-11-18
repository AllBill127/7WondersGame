using _7WondersGame.Cli.Arguments;
using Serilog;
using Serilog.Events;

namespace _7WondersGame.Cli.Logs;

/// <summary>
/// Log level and console logger setup.
/// Information: run progress and one line per game. Debug: the step-by-step trace of real games.
/// Verbose: more detail (playable cards, every attempted purchase, deck order).
/// </summary>
public static class Logging
{
    /// <summary>The environment variable that sets the log level when <c>--log</c> doesn't.</summary>
    public const string EnvVar = "SEVENWONDERS_LOG";

    /// <summary>The level names, for help and error messages.</summary>
    public const string Levels = "error, warning, info, debug, verbose";

    private const string ConsoleTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {GamePrefix}{Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// The level to use, and where it came from: <c>--log</c>, then the command's own default (replay traces),
    /// then the <see cref="EnvVar"/> environment variable, then the build (Debug builds: debug, Release: info).
    /// </summary>
    public static (LogEventLevel Level, string Source) Resolve(string? option, LogEventLevel? commandDefault = null)
    {
        if (option != null)
        {
            return (Parse(option, "--log"), "--log");
        }
        if (commandDefault != null)
        {
            return (commandDefault.Value, "command default");
        }
        string? env = Environment.GetEnvironmentVariable(EnvVar);
        if (!string.IsNullOrWhiteSpace(env))
        {
            return (Parse(env, EnvVar), EnvVar);
        }
#if DEBUG
        return (LogEventLevel.Debug, "Debug build");
#else
        return (LogEventLevel.Information, "Release build");
#endif
    }

    /// <summary>The level a name means (any case), or a usage error naming <paramref name="where"/>.</summary>
    public static LogEventLevel Parse(string text, string where)
    {
        switch (text.Trim().ToLowerInvariant())
        {
            case "error":
                return LogEventLevel.Error;
            case "warning":
            case "warn":
                return LogEventLevel.Warning;
            case "info":
            case "information":
                return LogEventLevel.Information;
            case "debug":
                return LogEventLevel.Debug;
            case "verbose":
            case "trace":
                return LogEventLevel.Verbose;
            default:
                throw new UsageException($"{where}: unknown log level '{text}' (use {Levels}).");
        }
    }

    /// <summary>
    /// Console logger. Lines from a game's trace carry a "[g12] " prefix (the GamePrefix property) so parallel
    /// games can be told apart.
    /// </summary>
    public static void Configure(LogEventLevel level)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .WriteTo.Console(outputTemplate: ConsoleTemplate)
            .CreateLogger();
    }

    /// <summary>The trace logger for game <paramref name="gameIndex"/>.</summary>
    public static ILogger ForGame(long gameIndex)
    {
        return Log.ForContext("GamePrefix", $"[g{gameIndex}] ");
    }
}
