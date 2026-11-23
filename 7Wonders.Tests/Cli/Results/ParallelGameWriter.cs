using _7Wonders.Cli.Results;

namespace _7Wonders.Tests.Cli.Results;

/// <summary>Writes one three-seat game per call, for many threads to call at once.</summary>
internal sealed class ParallelGameWriter
{
    private readonly ResultsCsvWriter _writer;

    /// <summary>Writes through <paramref name="writer"/>.</summary>
    public ParallelGameWriter(ResultsCsvWriter writer)
    {
        _writer = writer;
    }

    /// <summary>Writes game <paramref name="gameIndex"/>, its deal and seed numbered the same.</summary>
    public void WriteGame(int gameIndex)
    {
        _writer.WriteGame("run", gameIndex, gameIndex, (ulong)gameIndex, ResultsLoggingTests.Seats(3));
    }
}
