using System.Globalization;

namespace _7WondersGame.Cli.Arguments;

/// <summary>
/// Parsed command line: <c>&lt;command&gt; [--name value | --name=value]...</c>.
/// Option names are case-insensitive; each option may be given once.
/// </summary>
public sealed class CliArguments
{
    /// <summary>Where an option's name starts, after its two dashes.</summary>
    private const int OptionNameStart = 2;

    private const string HexPrefix = "0x";

    private readonly Dictionary<string, string> _options;

    private CliArguments(string command, Dictionary<string, string> options, bool help)
    {
        Command = command;
        _options = options;
        HelpRequested = help;
    }

    /// <summary>The command, in lower case ("help" for <c>-h</c>, <c>--help</c> or <c>/?</c>).</summary>
    public string Command { get; }

    /// <summary>Whether <c>-h</c> or <c>--help</c> followed the command.</summary>
    public bool HelpRequested { get; }

    /// <summary>The options given, as written (without the dashes).</summary>
    public IEnumerable<string> Names
    {
        get
        {
            return _options.Keys;
        }
    }

    /// <summary>Reads the command and its options, or throws a usage error.</summary>
    public static CliArguments Parse(IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
        {
            return new CliArguments("help", new Dictionary<string, string>(), false);
        }

        string command = arguments[0].ToLowerInvariant();
        if (IsHelpCommand(command))
        {
            command = "help";
        }

        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        bool help = false;
        for (int argumentIndex = 1; argumentIndex < arguments.Count; argumentIndex++)
        {
            string argument = arguments[argumentIndex];
            if (IsHelpFlag(argument))
            {
                help = true;
                continue;
            }
            if (!argument.StartsWith("--"))
            {
                if (command == "help" && argumentIndex == 1)
                {
                    options["topic"] = argument;
                    continue;
                }
                throw new UsageException($"Unexpected '{argument}'. Options are written as --name value.", command);
            }

            KeyValuePair<string, string> option = ReadOption(arguments, ref argumentIndex, command);
            if (!options.TryAdd(option.Key, option.Value))
            {
                throw new UsageException($"--{option.Key} is given more than once.", command);
            }
        }
        return new CliArguments(command, options, help);
    }

    /// <summary>
    /// The option at <paramref name="optionIndex"/> (<c>--name=value</c>, or <c>--name value</c>, which moves
    /// <paramref name="optionIndex"/> on to the value), as its name and value.
    /// </summary>
    private static KeyValuePair<string, string> ReadOption(IReadOnlyList<string> args, ref int optionIndex,
                                                           string command)
    {
        string argument = args[optionIndex];
        int equalsSignIndex = argument.IndexOf('=');
        if (equalsSignIndex > 0)
        {
            int nameLength = equalsSignIndex - OptionNameStart;
            int valueStart = equalsSignIndex + 1;
            string nameBeforeEqualsSign = argument.Substring(OptionNameStart, nameLength);
            string valueAfterEqualsSign = argument.Substring(valueStart);
            return new KeyValuePair<string, string>(nameBeforeEqualsSign, valueAfterEqualsSign);
        }

        string name = argument.Substring(OptionNameStart);
        int valueIndex = optionIndex + 1;
        if (!HasValueAt(args, valueIndex))
        {
            throw new UsageException($"--{name} needs a value.", command);
        }
        optionIndex = valueIndex;
        return new KeyValuePair<string, string>(name, args[valueIndex]);
    }

    private static bool IsHelpCommand(string command)
    {
        return command == "-h" || command == "--help" || command == "/?";
    }

    private static bool IsHelpFlag(string argument)
    {
        return argument == "-h" || argument == "--help";
    }

    /// <summary>
    /// Whether there is an argument at <paramref name="index"/> that is a value, not the next option.
    /// </summary>
    private static bool HasValueAt(IReadOnlyList<string> args, int index)
    {
        return index < args.Count && !args[index].StartsWith("--");
    }

    /// <summary>Throws if an option is not one this command knows.</summary>
    public void AllowOnly(params string[] known)
    {
        foreach (string name in _options.Keys)
        {
            if (!known.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                string knownText = string.Join(", ", known.Select(knownName => "--" + knownName));
                throw new UsageException($"Unknown option --{name} for '{Command}'. Known: {knownText}.", Command);
            }
        }
    }

    /// <summary>Whether the option was given.</summary>
    public bool Has(string name)
    {
        return _options.ContainsKey(name);
    }

    /// <summary>
    /// These options plus <paramref name="defaults"/> (e.g. from an experiment file); an option given here wins.
    /// </summary>
    public CliArguments WithDefaults(IReadOnlyDictionary<string, string> defaults)
    {
        var merged = new Dictionary<string, string>(defaults, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in _options)
        {
            merged[name] = value;
        }
        return new CliArguments(Command, merged, HelpRequested);
    }

    /// <summary>yes/no (or true/false), or null if not given.</summary>
    public bool? GetYesNo(string name)
    {
        string? text = Get(name);
        if (text == null)
        {
            return null;
        }

        string answer = text.ToLowerInvariant();
        if (answer == "yes" || answer == "true")
        {
            return true;
        }
        if (answer == "no" || answer == "false")
        {
            return false;
        }
        throw new UsageException($"--{name} must be yes or no, not '{text}'.", Command);
    }

    /// <summary>yes/no (or true/false), or <paramref name="defaultValue"/> if not given.</summary>
    public bool GetYesNoOrDefault(string name, bool defaultValue)
    {
        bool? answer = GetYesNo(name);
        if (answer == null)
        {
            return defaultValue;
        }
        return answer.Value;
    }

    /// <summary>The option's value as written, or null if not given.</summary>
    public string? Get(string name)
    {
        if (_options.TryGetValue(name, out string? value))
        {
            return value;
        }
        return null;
    }

    /// <summary>The option's value as written, or <paramref name="defaultValue"/> if not given.</summary>
    public string GetOrDefault(string name, string defaultValue)
    {
        string? value = Get(name);
        if (value == null)
        {
            return defaultValue;
        }
        return value;
    }

    /// <summary>The option's value as written, or a usage error if not given.</summary>
    public string Require(string name)
    {
        string? value = Get(name);
        if (value == null)
        {
            throw new UsageException($"--{name} is required.", Command);
        }
        return value;
    }

    /// <summary>A whole number from <paramref name="min"/> to <paramref name="max"/>, or null if not given.</summary>
    public int? GetInt(string name, int min, int max = int.MaxValue)
    {
        string? text = Get(name);
        if (text == null)
        {
            return null;
        }

        bool isWholeNumber = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value);
        if (!isWholeNumber || value < min || value > max)
        {
            throw new UsageException(WholeNumberRangeMessage(name, min, max, text), Command);
        }
        return value;
    }

    private static string WholeNumberRangeMessage(string name, int min, int max, string text)
    {
        if (max == int.MaxValue)
        {
            return $"--{name} must be a whole number of at least {min}, not '{text}'.";
        }
        return $"--{name} must be a whole number from {min} to {max}, not '{text}'.";
    }

    /// <summary>
    /// A whole number of at least <paramref name="min"/>, or <paramref name="defaultValue"/> if not given.
    /// </summary>
    public int GetIntOrDefault(string name, int min, int defaultValue)
    {
        int? value = GetInt(name, min);
        if (value == null)
        {
            return defaultValue;
        }
        return value.Value;
    }

    /// <summary>
    /// A whole number from <paramref name="min"/> to <paramref name="max"/>, or a usage error if not given.
    /// </summary>
    public int RequireInt(string name, int min, int max = int.MaxValue)
    {
        int? value = GetInt(name, min, max);
        if (value == null)
        {
            throw new UsageException($"--{name} is required.", Command);
        }
        return value.Value;
    }

    /// <summary>A seed: a decimal number or 0x-prefixed hex.</summary>
    public static bool TryParseSeed(string text, out ulong seed)
    {
        if (text.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string hexDigits = text.Substring(HexPrefix.Length);
            return ulong.TryParse(hexDigits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out seed);
        }
        return ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed);
    }

    /// <summary>A seed (decimal or 0x hex), or null if not given.</summary>
    public ulong? GetSeed(string name)
    {
        string? text = Get(name);
        if (text == null)
        {
            return null;
        }
        if (!TryParseSeed(text, out ulong seed))
        {
            throw new UsageException($"--{name} must be a number (decimal or 0x hex), not '{text}'.", Command);
        }
        return seed;
    }
}
