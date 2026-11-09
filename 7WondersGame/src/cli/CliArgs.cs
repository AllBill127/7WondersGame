using System.Globalization;

namespace _7WondersGame.src.cli
{
    /// <summary>A mistake in the command line. The message says what is wrong; the program prints it with a help hint.</summary>
    public sealed class UsageException : Exception
    {
        public string? Command { get; }
        public UsageException(string message, string? command = null) : base(message) => Command = command;
    }

    /// <summary>
    /// Parsed command line: <c>&lt;command&gt; [--name value | --name=value]...</c>.
    /// Option names are case-insensitive; each option may be given once.
    /// </summary>
    public sealed class CliArgs
    {
        public string Command { get; }
        public bool HelpRequested { get; }
        private readonly Dictionary<string, string> _options;

        private CliArgs(string command, Dictionary<string, string> options, bool help)
        {
            Command = command;
            _options = options;
            HelpRequested = help;
        }

        public static CliArgs Parse(IReadOnlyList<string> args)
        {
            if (args.Count == 0)
                return new CliArgs("help", new(), false);

            string command = args[0].ToLowerInvariant();
            if (command is "-h" or "--help" or "/?")
                command = "help";

            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool help = false;
            for (int i = 1; i < args.Count; i++)
            {
                string arg = args[i];
                if (arg is "-h" or "--help")
                {
                    help = true;
                    continue;
                }
                if (!arg.StartsWith("--"))
                {
                    if (command == "help" && i == 1)
                    {
                        options["topic"] = arg;
                        continue;
                    }
                    throw new UsageException($"Unexpected '{arg}'. Options are written as --name value.", command);
                }

                string name, value;
                int eq = arg.IndexOf('=');
                if (eq > 0)
                {
                    name = arg[2..eq];
                    value = arg[(eq + 1)..];
                }
                else
                {
                    name = arg[2..];
                    if (i + 1 >= args.Count || args[i + 1].StartsWith("--"))
                        throw new UsageException($"--{name} needs a value.", command);
                    value = args[++i];
                }
                if (!options.TryAdd(name, value))
                    throw new UsageException($"--{name} is given more than once.", command);
            }
            return new CliArgs(command, options, help);
        }

        /// <summary>Throws if an option is not one this command knows.</summary>
        public void AllowOnly(params string[] known)
        {
            foreach (string name in _options.Keys)
                if (!known.Contains(name, StringComparer.OrdinalIgnoreCase))
                    throw new UsageException(
                        $"Unknown option --{name} for '{Command}'. Known: {string.Join(", ", known.Select(k => "--" + k))}.", Command);
        }

        public bool Has(string name) => _options.ContainsKey(name);

        public string? Get(string name) => _options.TryGetValue(name, out string? v) ? v : null;

        public string Require(string name) =>
            Get(name) ?? throw new UsageException($"--{name} is required.", Command);

        public int? GetInt(string name, int min, int max = int.MaxValue)
        {
            string? text = Get(name);
            if (text == null)
                return null;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value < min || value > max)
                throw new UsageException(
                    max == int.MaxValue
                        ? $"--{name} must be a whole number of at least {min}, not '{text}'."
                        : $"--{name} must be a whole number from {min} to {max}, not '{text}'.", Command);
            return value;
        }

        public int RequireInt(string name, int min, int max = int.MaxValue) =>
            GetInt(name, min, max) ?? throw new UsageException($"--{name} is required.", Command);

        /// <summary>A seed: a decimal number or 0x-prefixed hex.</summary>
        public static bool TryParseSeed(string text, out ulong seed) =>
            text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? ulong.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out seed)
                : ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed);

        public ulong? GetSeed(string name)
        {
            string? text = Get(name);
            if (text == null)
                return null;
            if (!TryParseSeed(text, out ulong seed))
                throw new UsageException($"--{name} must be a number (decimal or 0x hex), not '{text}'.", Command);
            return seed;
        }
    }
}
