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

            //await GenerateGameDatasets(5, 100);

            await RunGameTests(args);
        }

        private static async Task RunGameTests(string[] args)
        {
            // get main arguments log buffer size, game count to simulate, log excel file and sheet names, multithread amount and game dataset presets
            int maxLogBufferSize = 3;
            int gameCount = 1000;
            string resultsExcelName = "memTest.xlsx";
            string resultsSheetName = "rng_det";
            string gameDatasetFileName = null;
            parallelCount = 3;
            double minC = 0, maxC = 100;
            int iterCount = 100;
            ulong runSeed = 0;
            Dictionary<int, List<int>[]> gameDatasets = null;

            // read and log program cli run or default arguments
            Log.Information("input: [maxLogBufferSize], [gameCount], [resultsExcelName], [resultsSheetName], [parallelCount], [minC], [maxC], [iterCount], [gameDatasetFileName], [runSeed]");
            if (args.Length == 0)
            {
                args = Console.ReadLine().Split(", ");
            }
            if (args.Length > 0 && !int.TryParse(args[0], out maxLogBufferSize))
                maxLogBufferSize = 2;
            if (args.Length > 1 && !int.TryParse(args[1], out gameCount))
                gameCount = 4;
            if (args.Length > 2 && !string.IsNullOrEmpty(args[2]))
                resultsExcelName = args[2];
            if (args.Length > 3 && !string.IsNullOrEmpty(args[3]))
                resultsSheetName = args[3];
            if (args.Length > 4 && !int.TryParse(args[4], out parallelCount))
                parallelCount = 2; ;
            if (args.Length > 5 && !double.TryParse(args[5], out minC))
                minC = 0;
            if (args.Length > 6 && !double.TryParse(args[6], out maxC))
                maxC = 100;
            if (args.Length > 7 && !int.TryParse(args[7], out iterCount))
                iterCount = 10;
            if (args.Length > 8 && !string.IsNullOrEmpty(args[8]))
                gameDatasetFileName = args[8];
            else 
                gameDatasetFileName = null;
            if (args.Length > 9 && !string.IsNullOrEmpty(args[9]))
                runSeed = ulong.Parse(args[9]);
            Log.Information("Run seed: {runSeed}", runSeed);

            string? programPath = GetProgramFilePath();
            Log.Information("Program File Path: {programPath}", programPath);
            Log.Information("Log buffer size: {maxBufferSize} | game count: {gameCount}\nLog excel name: {resultsExcelName} | Log sheet name: {resultsSheetName}\nParallel count: {parallelCount}",
                maxLogBufferSize, gameCount, resultsExcelName, resultsSheetName, parallelCount);

            // init filer
            Filer.InitFiler(programPath, resultsExcelName, maxLogBufferSize);
            
            // read game presets dataset
            if (gameDatasetFileName != null)
                gameDatasets = await Filer.ReadGameDatasetFromFile(gameDatasetFileName);

            // run games
            Log.Information("Starting game simulations");

            //RunGamesParallel(gameCount, runSeed, resultsSheetName, gameDatasets);
            //await GenerateGameDatasets(runSeed, 3, 100);
            SearchOptimalCParallel(gameCount, minC, maxC, iterCount, runSeed, resultsSheetName, gameDatasets);

            await Filer.FlushMatchLogBuffer(resultsSheetName);
            await Filer.AwaitAllLoggingTasksAsync();

            Log.Information("Full run complete!");
        }

        private static async Task GenerateGameDatasets(ulong runSeed, int numPlayers = 3, int gameCount = 100)
        {
            // get main arguments log buffer size, game count to simulate, log excel file and sheet names, multithread amount and game dataset presets
            int maxLogBufferSize = 2;
            string resultsExcelName = "7WondersGameResults.xlsx";
            parallelCount = 3;
            Dictionary<int, List<int>[]> gameDatasets = new();

            string? programPath = GetProgramFilePath();
            Log.Information("Program File Path: {programPath}", programPath);
            Log.Information("numPlayers: {numPlayers} | game count: {gameCount}",
                numPlayers, gameCount);

            Filer.InitFiler(programPath, resultsExcelName, maxLogBufferSize);

            for (int i = 0; i < gameCount; ++i)
            {
                // create each games players
                List<Player> players = new();
                for (int j = 0; j < numPlayers; j++)
                {
                    players.Add(new RandomChoiceAI(j));
                }

                // create game
                Game newGame = new Game(Seeding.GameSeed(runSeed, i))
                {
                    Players = players,
                };
                newGame.InitGame();

                // make game data and add to dataset
                List<int>[] gameData = new List<int>[]
                {
                    new List<int>(),
                    new List<int>(),
                    new List<int>(),
                    new List<int>()
                };

                for (int j = 0; j < 3; j++)
                    foreach (var card in newGame.Decks[j])
                        gameData[j].Add((int)card.Id);

                foreach (Player player in players)
                    gameData[3].Add((int)player.Board.Id);

                gameDatasets.Add(i, gameData);

                if (i % 10 == 0)
                    Log.Information("Game {gameNr} complete!", i);
            }

            await Filer.WriteGameDatasetToFile(String.Format("gameDataset_{0}p_{1}g.txt", numPlayers, gameCount),
                gameDatasets);

            Log.Information("Full run complete!");
        }

        private static void RunGames(int numGames, ulong runSeed, string logSheetName = "testRun")
        {
            for (int i = 0; i < numGames; ++i)
            {
                // create each games players
                List<Player> players = new()
                {
                    new DeterministicAI(0),
                    new DeterministicAI(1),
                    new MCTSAIv1(2),
                    new DeterministicAI(3),
                    new DeterministicAI(4),
                    new DeterministicAI(5),
                    new DeterministicAI(6),
                };

                // create game
                Game newGame = new Game(Seeding.GameSeed(runSeed, i))
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

        private static void RunGamesParallel(int numGames, ulong runSeed, string logSheetName = "testRun", Dictionary<int, List<int>[]> gameDatasets = null)
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
                    new DeterministicAI(2),
                    new DeterministicAI(3),
                    new RandomChoiceAI(4),
                    new RandomChoiceAI(5),
                    new RandomChoiceAI(6),
                };

                // create game
                Game newGame = new Game(Seeding.GameSeed(runSeed, i))
                {
                    Players = players,
                };
                if (gameDatasets != null)
                    newGame.InitGameNotRandom(gameDatasets[i]);
                else
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

        private static void SearchOptimalCParallel(int numGames, double minC, double maxC, int iterations, ulong runSeed, string logSheetName = "testRun", Dictionary<int, List<int>[]> gameDatasets = null)
        {
            int counter = 0;
            Object setupLock = new();
            double iteratorC = (maxC - minC) / iterations;

            try
            {
                Parallel.For(0, (numGames * (iterations==1 ? 1 : (iterations + 1))), new ParallelOptions { MaxDegreeOfParallelism = parallelCount }, i =>
                {
                    Log.Information("Processing iteration {iterationNr}", i);

                    List<Player> players;
                    Game newGame;
                    // game i always gets the same seed, deal and C, whatever the thread timing
                    int gameIndex = i;
                    double c = minC + (gameIndex / numGames) * iteratorC;
                    lock (setupLock)
                    {
                        // create each games players
                        players = new()
                        {
                            new DeterministicAI(0),
                            //new MCTSAIv4(0, c, 1),
                            //new MCTSAIv4(0, 136, 5),
                            //new JMMCTSAIv2(1, c),
                            new RandomChoiceAI(1),
                            //new DeterministicAI(2),
                            new MCTSAIv10(2, c, 1),
                            //new MCTSAIv5(2, 136, 5),
                            //new JMMCTSAIv2(2, c),
                            //new DeterministicAI(3),
                            //new RandomChoiceAI(4),
                            //new RandomChoiceAI(5),
                            //new RandomChoiceAI(6),
                        };

                        // create game
                        newGame = new Game(Seeding.GameSeed(runSeed, gameIndex))
                        {
                            Players = players,
                        };
                        if (gameDatasets != null)
                            newGame.InitGameNotRandom(gameDatasets[gameIndex % numGames]);
                        else
                            newGame.InitGame();

                        Log.Information("game: {gameIndex} | seed: {seed} | C = {cValue}", gameIndex, newGame.Seed, c);

                        // Log player wondes
                        foreach (Player player in newGame.Players)
                        {
                            Log.Information("Player {playerId} wonder: {playerBoardName}", player.Id, player.Board.Name);
                        }
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
            catch (Exception ex)
            {
                Log.Error("PARALLEL FAILED: {ex}", ex.Message);
            }
        }

        static string? GetProgramFilePath()
        {
            string executablePath = AppDomain.CurrentDomain.BaseDirectory;
            return executablePath;
        }
    }
}