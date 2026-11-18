using System.Globalization;
using System.Text;

namespace _7WondersGame.Cli.Results;

/// <summary>
/// The one writer of the results CSV: one row per (game, seat), appended and flushed after every game,
/// so a crash loses at most the game being written. Safe to call from parallel games.
/// </summary>
public sealed class ResultsCsvWriter : IDisposable
{
    /// <summary>The columns, in the order of the header and of every row.</summary>
    public static readonly string[] Columns =
    {
        "runId", "gameIndex", "dealIndex", "seed", "seat", "agent", "configJson",
        "wonder", "side", "vp", "coins", "rank", "winShare", "agentStats",
    };

    /// <summary>The characters that make a field need quotes.</summary>
    private static readonly char[] CharactersToQuote = { ',', '"', '\n', '\r' };

    private readonly object _lock = new();
    private readonly StreamWriter _writer;

    /// <summary>
    /// Opens the file for appending (creating it and writing the header if it is new or empty).
    /// Throws if an existing file has a different header, so rows with different columns never mix.
    /// </summary>
    public ResultsCsvWriter(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        bool isNew = !File.Exists(FilePath) || new FileInfo(FilePath).Length == 0;
        if (!isNew)
        {
            string? existingHeader = File.ReadLines(FilePath).FirstOrDefault();
            if (existingHeader != Header)
            {
                throw new InvalidOperationException(
                    $"{FilePath} has a different header. Expected: {Header}. Found: {existingHeader}");
            }
        }

        var stream = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n" };
        if (isNew)
        {
            _writer.WriteLine(Header);
            _writer.Flush();
        }
    }

    /// <summary>The header line: the column names.</summary>
    public static string Header
    {
        get
        {
            return string.Join(",", Columns);
        }
    }

    /// <summary>The results CSV (full path).</summary>
    public string FilePath { get; }

    /// <summary>Appends one row per seat of a finished game, all at once, and flushes them.</summary>
    public void WriteGame(string runId, long gameIndex, long dealIndex, ulong seed, IEnumerable<SeatResult> seats)
    {
        var rows = new StringBuilder();
        foreach (SeatResult seatResult in seats)
        {
            rows.Append(FormatRow(runId, gameIndex, dealIndex, seed, seatResult)).Append('\n');
        }

        lock (_lock)
        {
            _writer.Write(rows.ToString());
            _writer.Flush();
        }
    }

    /// <summary>One seat's row, its fields quoted where needed (no line break at the end).</summary>
    public static string FormatRow(string runId, long gameIndex, long dealIndex, ulong seed, SeatResult seatResult)
    {
        var invariantCulture = CultureInfo.InvariantCulture;
        string[] fields =
        {
            runId,
            gameIndex.ToString(invariantCulture),
            dealIndex.ToString(invariantCulture),
            seed.ToString(invariantCulture),
            seatResult.Seat.ToString(invariantCulture),
            seatResult.Agent,
            seatResult.ConfigJson,
            seatResult.Wonder,
            seatResult.Side,
            seatResult.VictoryPoints.ToString(invariantCulture),
            seatResult.Coins.ToString(invariantCulture),
            seatResult.Rank.ToString(invariantCulture),
            seatResult.WinShare.ToString("0.######", invariantCulture),
            seatResult.StatsJson,
        };
        return string.Join(",", fields.Select(Escape));
    }

    /// <summary>
    /// RFC 4180 quoting: fields with commas, quotes or line breaks are quoted, and quotes are doubled.
    /// </summary>
    public static string Escape(string field)
    {
        if (field.IndexOfAny(CharactersToQuote) < 0)
        {
            return field;
        }
        return "\"" + field.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// Reads a results CSV back as one dictionary (column → value) per row.
    /// Handles the quoting <see cref="Escape"/> writes (no line breaks inside fields, which this writer never
    /// produces).
    /// </summary>
    public static IEnumerable<Dictionary<string, string>> ReadRows(string filePath)
    {
        string[]? header = null;
        foreach (string line in File.ReadLines(filePath))
        {
            if (line.Length == 0)
            {
                continue;
            }
            List<string> fields = SplitRow(line);
            if (header == null)
            {
                header = fields.ToArray();
                continue;
            }
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int column = 0; column < header.Length && column < fields.Count; column++)
            {
                row[header[column]] = fields[column];
            }
            yield return row;
        }
    }

    /// <summary>The fields of one CSV line, unquoted.</summary>
    public static List<string> SplitRow(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        for (int position = 0; position < line.Length; position++)
        {
            char character = line[position];
            if (quoted)
            {
                int nextIndex = position + 1;
                if (character == '"' && IsQuoteAt(line, nextIndex))
                {
                    // a doubled quote inside a quoted field is one quote
                    field.Append('"');
                    position = nextIndex;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(character);
                }
            }
            else if (character == '"')
            {
                quoted = true;
            }
            else if (character == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(character);
            }
        }
        fields.Add(field.ToString());
        return fields;
    }

    private static bool IsQuoteAt(string line, int index)
    {
        return index < line.Length && line[index] == '"';
    }

    /// <summary>Closes the file.</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Dispose();
        }
    }
}
