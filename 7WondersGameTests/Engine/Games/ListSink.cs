using Serilog.Core;
using Serilog.Events;

namespace _7WondersGameTests.Engine.Games;

internal sealed class ListSink : ILogEventSink
{
    public readonly List<LogEvent> Events = new();
    public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    // rendered like the console ({Message:lj}): strings without quotes
    private static readonly Serilog.Formatting.Display.MessageTemplateTextFormatter Format = new("{Message:lj}");
    public IEnumerable<string> Messages => Events.Select(e =>
    {
        var w = new StringWriter();
        Format.Format(e, w);
        return w.ToString();
    });
}
