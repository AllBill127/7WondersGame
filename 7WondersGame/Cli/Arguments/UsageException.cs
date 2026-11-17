namespace _7WondersGame.Cli.Arguments;

/// <summary>A mistake in the command line. The message says what is wrong; the program prints it with a help hint.</summary>
public sealed class UsageException : Exception
{
    public string? Command { get; }
    public UsageException(string message, string? command = null) : base(message) => Command = command;
}
