using _7WondersGame.src.cli;
using Serilog;

namespace _7WondersGame.src
{
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
                    Console.WriteLine(HelpText.For(cli.Command == "help" ? cli.Get("topic") : cli.Command));
                    return Commands.Ok;
                }

                return cli.Command switch
                {
                    "run" => await Commands.Run(cli, args),
                    "replay" => await Commands.Replay(cli),
                    "analyze" => Commands.Analyze(cli),
                    "dataset" => await Commands.Dataset(cli),
                    "agents" => Commands.Agents(cli),
                    _ => throw new UsageException($"Unknown command '{args[0]}'."),
                };
            }
            catch (UsageException ex)
            {
                string? command = ex.Command ?? cli?.Command;
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.Error.WriteLine(command != null && HelpText.Topics.Contains(command)
                    ? $"See: 7WondersGame help {command}"
                    : "See: 7WondersGame help");
                return Commands.BadUsage;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
