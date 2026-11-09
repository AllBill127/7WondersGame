using Serilog;
using System.Globalization;
using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.MCTS;

namespace _7WondersGame.src
{
    internal class Program
    {
        private const string Usage =
            "args: <gameCount> <results.csv> <parallelCount> <minC> <maxC> <iterCount> <runSeed> [datasetFile]";

        /// <summary>One run: its id and seed, and the single writer all its games append to.</summary>
        private sealed record RunContext(string RunId, ulong RunSeed, ResultsCsvWriter Writer);

        static async Task<int> Main(string[] args)
        {
            // Configure global Serilog logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                //.MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            //await GenerateGameDatasets(runSeed: 0, numPlayers: 5, gameCount: 100);

            return await RunGameTests(args);
        }

        private static async Task<int> RunGameTests(string[] args)
        {
            // defaults, used when an argument is left out
            int gameCount = 1000;
            string resultsCsvName = "results.csv";
            int parallelCount = 3;
            double minC = 0, maxC = 100;
            int iterCount = 100;
            ulong? runSeedArg = null;
            string? gameDatasetFileName = null;

            Log.Information(Usage);
            if (args.Length == 0)
                Log.Warning("No arguments given; using the defaults.");
            try
            {
                if (args.Length > 0) gameCount = int.Parse(args[0], CultureInfo.InvariantCulture);
                if (args.Length > 1 && args[1] != "") resultsCsvName = args[1];
                if (args.Length > 2) parallelCount = int.Parse(args[2], CultureInfo.InvariantCulture);
                if (args.Length > 3) minC = double.Parse(args[3], CultureInfo.InvariantCulture);
                if (args.Length > 4) maxC = double.Parse(args[4], CultureInfo.InvariantCulture);
                if (args.Length > 5) iterCount = int.Parse(args[5], CultureInfo.InvariantCulture);
                if (args.Length > 6) runSeedArg = ParseSeed(args[6]);
                if (args.Length > 7 && args[7] != "") gameDatasetFileName = args[7];
            }
            catch (FormatException ex)
            {
                Log.Error("Bad argument: {message}\n{usage}", ex.Message, Usage);
                return 2;
            }

            // a missing seed is made from the clock, and recorded, so the run can still be repeated
            ulong runSeed = runSeedArg ?? (ulong)DateTime.UtcNow.Ticks;
            DateTime started = DateTime.UtcNow;
            string runId = RunManifest.NewRunId(started, runSeed);

            string programPath = AppDomain.CurrentDomain.BaseDirectory;
            Filer.SetDataPath(programPath);
            string csvPath = Path.IsPathRooted(resultsCsvName)
                ? resultsCsvName
                : Path.Combine(programPath, "results", resultsCsvName);
            string manifestPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(csvPath))!, $"{runId}.run.json");

            var manifest = new RunManifest
            {
                RunId = runId,
                RunSeed = runSeed,
                Args = args,
                Settings = new Dictionary<string, string>
                {
                    ["experiment"] = nameof(SearchOptimalCParallel),
                    ["gameCount"] = gameCount.ToString(CultureInfo.InvariantCulture),
                    ["parallelCount"] = parallelCount.ToString(CultureInfo.InvariantCulture),
                    ["minC"] = minC.ToString(CultureInfo.InvariantCulture),
                    ["maxC"] = maxC.ToString(CultureInfo.InvariantCulture),
                    ["iterCount"] = iterCount.ToString(CultureInfo.InvariantCulture),
                    ["dataset"] = gameDatasetFileName ?? "",
                },
                ResultsCsv = Path.GetFullPath(csvPath),
                StartedUtc = started,
            };
            manifest.Save(manifestPath);

            Log.Information("Run {runId} | seed {runSeed} | commit {commit}", runId, runSeed, manifest.GitCommit);
            Log.Information("Results: {csv}\nManifest: {manifest}", manifest.ResultsCsv, manifestPath);

            // read game presets dataset
            Dictionary<int, List<int>[]>? gameDatasets = null;
            if (gameDatasetFileName != null)
                gameDatasets = await Filer.ReadGameDatasetFromFile(gameDatasetFileName);

            Log.Information("Starting game simulations");
            (int completed, int failed) outcome;
            using (var writer = new ResultsCsvWriter(csvPath))
            {
                var run = new RunContext(runId, runSeed, writer);
                //outcome = RunGamesParallel(run, gameCount, parallelCount, gameDatasets);
                outcome = SearchOptimalCParallel(run, gameCount, minC, maxC, iterCount, parallelCount, gameDatasets);
            }

            manifest.FinishedUtc = DateTime.UtcNow;
            manifest.GamesCompleted = outcome.completed;
            manifest.GamesFailed = outcome.failed;
            manifest.Save(manifestPath);

            Log.Information("Full run complete! {completed} games, {failed} failed", outcome.completed, outcome.failed);
            return outcome.failed == 0 ? 0 : 1;
        }

        /// <summary>Accepts a decimal number or 0x-prefixed hex.</summary>
        private static ulong ParseSeed(string text) =>
            text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? ulong.Parse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : ulong.Parse(text, CultureInfo.InvariantCulture);

        /// <summary>
        /// Plays one game to the end and appends its rows to the results CSV.
        /// Returns false (and logs the seed so the game can be replayed) if the game throws.
        /// </summary>
        private static bool PlayAndRecord(RunContext run, Game game, long gameIndex, long dealIndex)
        {
            try
            {
                game.Loop(record: true);
                run.Writer.WriteGame(run.RunId, gameIndex, dealIndex, game.Seed, game.Results!);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Game {gameIndex} (seed {seed}) failed: {ex}", gameIndex, game.Seed, ex);
                return false;
            }
        }

        private static async Task GenerateGameDatasets(ulong runSeed, int numPlayers = 3, int gameCount = 100)
        {
            Dictionary<int, List<int>[]> gameDatasets = new();

            string programPath = AppDomain.CurrentDomain.BaseDirectory;
            Filer.SetDataPath(programPath);
            Log.Information("Program File Path: {programPath}", programPath);
            Log.Information("numPlayers: {numPlayers} | game count: {gameCount} | seed: {runSeed}",
                numPlayers, gameCount, runSeed);

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

        private static (int completed, int failed) RunGames(RunContext run, int numGames)
        {
            int completed = 0, failed = 0;
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
                Game newGame = new Game(Seeding.GameSeed(run.RunSeed, i))
                {
                    Players = players,
                };
                newGame.InitGame();

                // run game and log results
                if (PlayAndRecord(run, newGame, i, i)) completed++; else failed++;

                if (i % 10 == 0)
                    Log.Information("Game {gameNr} complete!", i);
            }
            return (completed, failed);
        }

        private static (int completed, int failed) RunGamesParallel(RunContext run, int numGames, int parallelCount,
            Dictionary<int, List<int>[]>? gameDatasets = null)
        {
            int completed = 0, failed = 0;

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
                Game newGame = new Game(Seeding.GameSeed(run.RunSeed, i))
                {
                    Players = players,
                };
                if (gameDatasets != null)
                    newGame.InitGameNotRandom(gameDatasets[i]);
                else
                    newGame.InitGame();

                // Log player wonders
                foreach (Player player in newGame.Players)
                {
                    Log.Information("Player {playerId} wonder: {playerBoardName}", player.Id, player.Board.Name);
                }

                // run game and log results
                bool ok = PlayAndRecord(run, newGame, i, i);
                int done = ok ? Interlocked.Increment(ref completed) : Interlocked.Increment(ref failed);

                if (ok && done % 10 == 0)
                {
                    Log.Information("{counter} Games complete! curr game: {gameNr} ", done, i);
                }

                Log.Information("Completed iteration  {iterationNr}", i);
            });

            return (completed, failed);
        }

        private static (int completed, int failed) SearchOptimalCParallel(RunContext run, int numGames, double minC, double maxC,
            int iterations, int parallelCount, Dictionary<int, List<int>[]>? gameDatasets = null)
        {
            int completed = 0, failed = 0;
            Object setupLock = new();
            double iteratorC = (maxC - minC) / iterations;

            Parallel.For(0, (numGames * (iterations==1 ? 1 : (iterations + 1))), new ParallelOptions { MaxDegreeOfParallelism = parallelCount }, i =>
            {
                Log.Information("Processing iteration {iterationNr}", i);

                // game i always gets the same seed, deal and C, whatever the thread timing
                int gameIndex = i;
                int dealIndex = gameDatasets != null ? gameIndex % numGames : gameIndex;
                double c = minC + (gameIndex / numGames) * iteratorC;

                List<Player> players;
                Game newGame;
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
                    newGame = new Game(Seeding.GameSeed(run.RunSeed, gameIndex))
                    {
                        Players = players,
                    };
                    if (gameDatasets != null)
                        newGame.InitGameNotRandom(gameDatasets[dealIndex]);
                    else
                        newGame.InitGame();

                    Log.Information("game: {gameIndex} | seed: {seed} | C = {cValue}", gameIndex, newGame.Seed, c);

                    // Log player wonders
                    foreach (Player player in newGame.Players)
                    {
                        Log.Information("Player {playerId} wonder: {playerBoardName}", player.Id, player.Board.Name);
                    }
                }

                // run game and log results
                bool ok = PlayAndRecord(run, newGame, gameIndex, dealIndex);
                int done = ok ? Interlocked.Increment(ref completed) : Interlocked.Increment(ref failed);

                if (ok && done % 10 == 0)
                {
                    Log.Information("{counter} Games complete! curr game: {gameNr} ", done, i);
                }

                Log.Information("Completed iteration  {iterationNr}", i);
            });

            return (completed, failed);
        }
    }
}
