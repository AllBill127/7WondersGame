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
        private static int parallelCount = 2;
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

            // get main arguments log buffer size, game count to simulate, log sheet name

            int maxLogBufferSize = 2;
            int gameCount = 10;
            string resultsExcelName = "7WondersGameResults.xlsx";
            string resultsSheetName = "TestRun6";
            parallelCount = 1;

            if (args.Length > 0 && !int.TryParse(args[0], out maxLogBufferSize))
                maxLogBufferSize = 2;
            if (args.Length > 1 && !int.TryParse(args[1], out gameCount))
                gameCount = 4;
            if (args.Length > 2 && !string.IsNullOrEmpty(args[2]))
                resultsExcelName = args[2];
            if (args.Length > 3 && !string.IsNullOrEmpty(args[3]))
                resultsSheetName = args[3];
            if (args.Length > 4 && !int.TryParse(args[4], out parallelCount))
                parallelCount = 2;

            string? programPath = GetProgramFilePath();
            Log.Information("Program File Path: {programPath}", programPath);
            Log.Information("Log buffer size: {maxBufferSize} | game count: {gameCount}\nLog excel name: {resultsExcelName} | Log sheet name: {resultsSheetName}\nParallel count: {parallelCount}",
                maxLogBufferSize, gameCount, resultsExcelName, resultsSheetName, parallelCount);

            Filer.InitFiler(programPath, resultsExcelName, maxLogBufferSize);

            Log.Information("Starting game simulations");

            RunGamesParallel(gameCount, resultsSheetName);
            await Filer.FlushMatchLogBuffer(resultsSheetName);
            await Filer.AwaitAllLoggingTasksAsync();

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

            Parallel.For(0, numGames, new ParallelOptions { MaxDegreeOfParallelism = parallelCount }, i =>
            {
                Log.Information("Processing iteration {iterationNr}", i);

                // create each games players
                List<Player> players = new()
                {
                    new DeterministicAI(0),
                    new DeterministicAI(1),
                    new MCTSAI(2),
                    new DeterministicAI(3),
                    new DeterministicAI(4),
                    //new DeterministicAI(5),
                    //new RandomChoiceAI(6),
                };

                // create game
                Game newGame = new Game
                {
                    Players = players,
                };
                newGame.InitGame();

                // Log player wondes
                foreach (Player player in newGame.Players)
                {
                    Log.Information("Player {playerId} wonder: {playerBoardName}", player.Id, player.Board.Name);
                }

                // run game and log results
                newGame.Loop(logSheetName);

                counter++;

                if (counter % 10 == 0)
                {
                    Log.Information("{counter} Games complete! curr game: {gameNr} ", counter, i);
                }

                Log.Information("Completed iteration  {iterationNr}", i);
            });
        }
        static string? GetProgramFilePath()
        {
            string executablePath = AppDomain.CurrentDomain.BaseDirectory;
            return executablePath;
        }
    }
}