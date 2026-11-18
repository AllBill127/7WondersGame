namespace _7WondersGame.Cli.Arguments;

/// <summary>
/// A mistake in the command line. The message says what is wrong; the program prints it with a help hint.
/// </summary>
public sealed class UsageException : Exception
{
    /// <summary>
    /// A mistake described by <paramref name="message"/>, made in <paramref name="command"/> if known.
    /// </summary>
    public UsageException(string message, string? command = null) : base(message)
    {
        Command = command;
    }

    /// <summary>The command whose help the hint points to, if known.</summary>
    public string? Command { get; }
}
