using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Security.Authentication.ExtendedProtection;
using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.MCTS;
using OfficeOpenXml;
using System.Reflection;

namespace _7WondersGame.src
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Configure excel license for logging match results into excell
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Configure glopbal Serilog logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                //.MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            string? programPath = GetProgramFilePath();
            Log.Information("Program File Path: {programPath}", programPath);
            Filer.InitFiler(programPath, "7WondersGameResults.xlsx");

            Log.Information("Starting game simulations");

            int gameCount = 1;
            string resultsSheetName = "Test";

            RunGamesParallel(gameCount, resultsSheetName);
            await Filer.FlushMatchLogBuffer(resultsSheetName);

            Log.Information("Full run complete!");
        }

        private static void RunGames(int numGames, string logSheetName = "testRun")
        {
            for (int i = 0; i < numGames; ++i)
            {
                // create each games players
                List<Player> players = new()
                {
                    new DeterministicAI(0),
                    new DeterministicAI(1),
                    new MCTSAI(2),
                    new DeterministicAI(3),
                    new DeterministicAI(4),
                    new DeterministicAI(5),
                    new DeterministicAI(6),
                };

                // create game
                Game newGame = new Game
                {
                    Players = players,
                };
                newGame.InitGame();

                // run game and log results
                newGame.Loop(logSheetName);

                if (i % 10 == 0)
                    Log.Information("Game {gameNr} complete!", i);
            }
        }

        private static void RunGamesParallel(int numGames, string logSheetName = "testRun")
        {
            int counter = 0;

            Parallel.For(0, numGames, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
            {
                //Log.Information("Processing iteration {iterationNr}", i);

                // create each games players
                List<Player> players = new()
                {
                    new RandomChoiceAI(0),
                    new RandomChoiceAI(1),
                    new MCTSAI(2),
                    new RandomChoiceAI(3),
                    new RandomChoiceAI(4),
                    new RandomChoiceAI(5),
                    new RandomChoiceAI(6),
                };

                // create game
                Game newGame = new Game
                {
                    Players = players,
                };
                newGame.InitGame();

                // run game and log results
                newGame.Loop(logSheetName);

                counter++;

                if (counter % 10 == 0)
                {
                    Log.Information("{counter} Games complete! curr game: {gameNr} ", counter, i);
                }

                //Log.Information("Completed iteration  {iterationNr}", i);
            });
        }
        static string? GetProgramFilePath()
        {
            string codeBase = Assembly.GetExecutingAssembly().Location;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return System.IO.Path.GetDirectoryName(path);
        }
    }
}