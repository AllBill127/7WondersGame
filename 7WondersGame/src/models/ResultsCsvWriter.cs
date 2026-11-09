using System.Globalization;
using System.Text;

namespace _7WondersGame.src.models
{
    /// <summary>
    /// The one writer of the results CSV (B2): one row per (game, seat), appended and flushed after every game,
    /// so a crash loses at most the game being written. Safe to call from parallel games.
    /// </summary>
    public sealed class ResultsCsvWriter : IDisposable
    {
        public static readonly string[] Columns =
        {
            "runId", "gameIndex", "dealIndex", "seed", "seat", "agent", "configJson",
            "wonder", "side", "vp", "coins", "rank", "winShare",
        };
        public static string Header => string.Join(",", Columns);

        private readonly object _lock = new();
        private readonly StreamWriter _writer;

        public string FilePath { get; }

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
                    throw new InvalidOperationException(
                        $"{FilePath} has a different header. Expected: {Header}. Found: {existingHeader}");
            }

            var stream = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n" };
            if (isNew)
            {
                _writer.WriteLine(Header);
                _writer.Flush();
            }
        }

        public void WriteGame(string runId, long gameIndex, long dealIndex, ulong seed, IEnumerable<SeatResult> seats)
        {
            var sb = new StringBuilder();
            foreach (SeatResult s in seats)
                sb.Append(FormatRow(runId, gameIndex, dealIndex, seed, s)).Append('\n');

            lock (_lock)
            {
                _writer.Write(sb.ToString());
                _writer.Flush();
            }
        }

        public static string FormatRow(string runId, long gameIndex, long dealIndex, ulong seed, SeatResult s)
        {
            var inv = CultureInfo.InvariantCulture;
            string[] fields =
            {
                runId,
                gameIndex.ToString(inv),
                dealIndex.ToString(inv),
                seed.ToString(inv),
                s.Seat.ToString(inv),
                s.Agent,
                s.ConfigJson,
                s.Wonder,
                s.Side,
                s.VictoryPoints.ToString(inv),
                s.Coins.ToString(inv),
                s.Rank.ToString(inv),
                s.WinShare.ToString("0.######", inv),
            };
            return string.Join(",", fields.Select(Escape));
        }

        /// <summary>RFC 4180 quoting: fields with commas, quotes or line breaks are quoted, and quotes are doubled.</summary>
        public static string Escape(string field) =>
            field.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
                ? "\"" + field.Replace("\"", "\"\"") + "\""
                : field;

        public void Dispose()
        {
            lock (_lock)
                _writer.Dispose();
        }
    }
}
