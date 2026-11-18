using _7WondersGame.Cli.Arguments;
using _7WondersGame.Cli.CommandLine;
using Serilog;

namespace _7WondersGame;

/// <summary>The command-line program.</summary>
internal static class Program
{
    /// <summary>
    /// Entry point: <c>7WondersGame &lt;command&gt; [options]</c>. Without arguments it prints help and exits;
    /// see <see cref="HelpText"/> for the commands and options.
    /// </summary>
    static async Task<int> Main(string[] args)
    {
        CliArgs? cli = null;
        try
        {
            cli = CliArgs.Parse(args);
            if (args.Length == 0)
            {
                Console.WriteLine(HelpText.General);
                return Commands.BadUsage;
            }
            if (cli.Command == "help" || cli.HelpRequested)
            {
                Console.WriteLine(HelpText.For(HelpTopic(cli)));
                return Commands.Ok;
            }

            return await RunCommand(cli, args);
        }
        catch (UsageException ex)
        {
            PrintUsageError(ex, cli);
            return Commands.BadUsage;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>What help to print: the topic after 'help', or the command given with -h.</summary>
    private static string? HelpTopic(CliArgs cli)
    {
        if (cli.Command == "help")
        {
            return cli.Get("topic");
        }
        return cli.Command;
    }

    private static async Task<int> RunCommand(CliArgs cli, string[] args)
    {
        switch (cli.Command)
        {
            case "run":
                return await Commands.Run(cli, args);
            case "replay":
                return await Commands.Replay(cli);
            case "analyze":
                return Commands.Analyze(cli);
            case "dataset":
                return await Commands.Dataset(cli);
            case "agents":
                return Commands.Agents(cli);
            default:
                throw new UsageException($"Unknown command '{args[0]}'.");
        }
    }

    /// <summary>Prints the mistake and where its help is: the command's own if it has one.</summary>
    private static void PrintUsageError(UsageException ex, CliArgs? cli)
    {
        string? command = ex.Command;
        if (command == null && cli != null)
        {
            command = cli.Command;
        }

        Console.Error.WriteLine($"Error: {ex.Message}");
        if (command != null && HelpText.Topics.Contains(command))
        {
            Console.Error.WriteLine($"See: 7WondersGame help {command}");
        }
        else
        {
            Console.Error.WriteLine("See: 7WondersGame help");
        }
    }
}
