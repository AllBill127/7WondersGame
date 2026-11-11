using Serilog;
using Serilog.Events;

namespace _7WondersGame.src.cli
{
    /// <summary>
    /// Log level and console logger setup.
    /// Information: run progress and one line per game. Debug: the step-by-step trace of real games.
    /// Verbose: more detail (playable cards, every attempted purchase, deck order).
    /// </summary>
    public static class Logging
    {
        public const string EnvVar = "SEVENWONDERS_LOG";

        public const string Levels = "error, warning, info, debug, verbose";

        /// <summary>
        /// The level to use, and where it came from: <c>--log</c>, then the command's own default (replay traces),
        /// then the <see cref="EnvVar"/> environment variable, then the build (Debug builds: debug, Release: info).
        /// </summary>
        public static (LogEventLevel Level, string Source) Resolve(string? option, LogEventLevel? commandDefault = null)
        {
            if (option != null)
                return (Parse(option, "--log"), "--log");
            if (commandDefault != null)
                return (commandDefault.Value, "command default");
            string? env = Environment.GetEnvironmentVariable(EnvVar);
            if (!string.IsNullOrWhiteSpace(env))
                return (Parse(env, EnvVar), EnvVar);
#if DEBUG
            return (LogEventLevel.Debug, "Debug build");
#else
            return (LogEventLevel.Information, "Release build");
#endif
        }

        public static LogEventLevel Parse(string text, string where) => text.Trim().ToLowerInvariant() switch
        {
            "error" => LogEventLevel.Error,
            "warning" or "warn" => LogEventLevel.Warning,
            "info" or "information" => LogEventLevel.Information,
            "debug" => LogEventLevel.Debug,
            "verbose" or "trace" => LogEventLevel.Verbose,
            _ => throw new UsageException($"{where}: unknown log level '{text}' (use {Levels})."),
        };

        /// <summary>
        /// Console logger. Lines from a game's trace carry a "[g12] " prefix (the GamePrefix property) so parallel
        /// games can be told apart.
        /// </summary>
        public static void Configure(LogEventLevel level)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {GamePrefix}{Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        /// <summary>The trace logger for game <paramref name="gameIndex"/>.</summary>
        public static ILogger ForGame(long gameIndex) => Log.ForContext("GamePrefix", $"[g{gameIndex}] ");
    }
}
