using _7Wonders.Agents.Catalog;
using _7Wonders.Cli.Arguments;
using _7Wonders.Cli.CommandLine;
using Serilog;

namespace _7Wonders.Cli;

/// <summary>The command-line program.</summary>
internal static class Program
{
    /// <summary>
    /// Entry point: <c>7Wonders &lt;command&gt; [options]</c>. Without arguments it prints help and exits;
    /// see <see cref="HelpText"/> for the commands and options.
    /// </summary>
    static async Task<int> Main(string[] args)
    {
        CliArguments? cli = null;
        try
        {
            cli = CliArguments.Parse(args);
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
        catch (UsageException exception)
        {
            PrintUsageError(exception.Message, exception.Command, cli);
            return Commands.BadUsage;
        }
        catch (AgentSpecificationException exception)
        {
            PrintUsageError(exception.Message, null, cli);
            return Commands.BadUsage;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>What help to print: the topic after 'help', or the command given with -h.</summary>
    private static string? HelpTopic(CliArguments cli)
    {
        if (cli.Command == "help")
        {
            return cli.Get("topic");
        }
        return cli.Command;
    }

    private static async Task<int> RunCommand(CliArguments cli, string[] args)
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

    /// <summary>
    /// Prints the mistake and where its help is: the help of <paramref name="command"/> (or else of the command
    /// given), if it has one.
    /// </summary>
    private static void PrintUsageError(string message, string? command, CliArguments? cli)
    {
        if (command == null && cli != null)
        {
            command = cli.Command;
        }

        Console.Error.WriteLine($"Error: {message}");
        if (command != null && HelpText.Topics.Contains(command))
        {
            Console.Error.WriteLine($"See: 7Wonders help {command}");
        }
        else
        {
            Console.Error.WriteLine("See: 7Wonders help");
        }
    }
}
