using System.Globalization;

namespace _7Wonders.Cli.Arguments;

/// <summary>
/// Parsed command line: <c>&lt;command&gt; [--name value | --name=value]...</c>.
/// Option names are case-insensitive; each option may be given once.
/// </summary>
internal sealed class CliArguments
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

        // OrdinalIgnoreCase: option names match character by character, ignoring upper and lower case, the same on
        // every machine (a culture's rules could match differently).
        Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);
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

            // `ref`: ReadOption moves the loop's own index on past the option's value.
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
    private static KeyValuePair<string, string> ReadOption(IReadOnlyList<string> arguments, ref int optionIndex,
                                                           string command)
    {
        string argument = arguments[optionIndex];
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
        if (!HasValueAt(arguments, valueIndex))
        {
            throw new UsageException($"--{name} needs a value.", command);
        }
        optionIndex = valueIndex;
        return new KeyValuePair<string, string>(name, arguments[valueIndex]);
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
    private static bool HasValueAt(IReadOnlyList<string> arguments, int index)
    {
        return index < arguments.Count && !arguments[index].StartsWith("--");
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
        Dictionary<string, string> merged = new(defaults, StringComparer.OrdinalIgnoreCase);
        foreach ((string name, string value) in _options)
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

    /// <summary>
    /// A whole number from <paramref name="minimum"/> to <paramref name="maximum"/>, or null if not given.
    /// </summary>
    public int? GetWholeNumber(string name, int minimum, int maximum = int.MaxValue)
    {
        string? text = Get(name);
        if (text == null)
        {
            return null;
        }

        // The invariant culture reads numbers the same way on every machine.
        bool isWholeNumber = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value);
        if (!isWholeNumber || value < minimum || value > maximum)
        {
            throw new UsageException(WholeNumberRangeMessage(name, minimum, maximum, text), Command);
        }
        return value;
    }

    private static string WholeNumberRangeMessage(string name, int minimum, int maximum, string text)
    {
        if (maximum == int.MaxValue)
        {
            return $"--{name} must be a whole number of at least {minimum}, not '{text}'.";
        }
        return $"--{name} must be a whole number from {minimum} to {maximum}, not '{text}'.";
    }

    /// <summary>
    /// A whole number of at least <paramref name="minimum"/>, or <paramref name="defaultValue"/> if not given.
    /// </summary>
    public int GetWholeNumberOrDefault(string name, int minimum, int defaultValue)
    {
        int? value = GetWholeNumber(name, minimum);
        if (value == null)
        {
            return defaultValue;
        }
        return value.Value;
    }

    /// <summary>
    /// A whole number from <paramref name="minimum"/> to <paramref name="maximum"/>, or a usage error if not given.
    /// </summary>
    public int RequireWholeNumber(string name, int minimum, int maximum = int.MaxValue)
    {
        int? value = GetWholeNumber(name, minimum, maximum);
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
