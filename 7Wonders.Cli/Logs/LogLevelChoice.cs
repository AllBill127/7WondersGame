using Serilog.Events;

namespace _7Wonders.Cli.Logs;

/// <summary>The log level to use, and where it came from (for the run manifest and messages).</summary>
internal sealed class LogLevelChoice
{
    /// <summary>The <paramref name="level"/> that <paramref name="source"/> gave.</summary>
    public LogLevelChoice(LogEventLevel level, string source)
    {
        Level = level;
        Source = source;
    }

    /// <summary>The level.</summary>
    public LogEventLevel Level { get; }

    /// <summary>What gave the level, e.g. "--log-level" or "Release build".</summary>
    public string Source { get; }
}
