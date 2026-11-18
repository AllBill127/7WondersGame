using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace _7WondersGameTests.Engine.Games;

/// <summary>A log sink that keeps every event in memory, so a test can read what was logged.</summary>
internal sealed class ListSink : ILogEventSink
{
    // rendered like the console ({Message:lj}): strings without quotes
    private static readonly MessageTemplateTextFormatter Format = new("{Message:lj}");

    /// <summary>Every event logged so far, in order.</summary>
    public List<LogEvent> Events { get; } = new();

    /// <summary>Every event's message, rendered the way the console shows it.</summary>
    public IEnumerable<string> Messages
    {
        get
        {
            return Events.Select(Render);
        }
    }

    /// <summary>Keeps the event.</summary>
    public void Emit(LogEvent logEvent)
    {
        Events.Add(logEvent);
    }

    private static string Render(LogEvent logEvent)
    {
        StringWriter writer = new StringWriter();
        Format.Format(logEvent, writer);
        return writer.ToString();
    }
}
