using System.Reflection;
using System.Runtime.CompilerServices;
using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;

namespace _7WondersGameTests.GameTests
{
    /// <summary>
    /// Step 4 (F) guard: the golden master. 200 seeded games (40 for each player count, 3 to 7) played through
    /// <see cref="Game.Loop"/> with the heuristic and random agents, and their final state, are stored in
    /// GoldenMaster/golden-master.txt. A refactoring step must reproduce it exactly. The deliberate rules
    /// changes (R7, R8, R5) re-record it in their own commit:
    ///     set GOLDEN_MASTER_RECORD=1, run this test, unset it, and commit the new file.
    /// </summary>
    [TestClass]
    public class GoldenMasterTests
    {
        public const string RecordEnvVar = "GOLDEN_MASTER_RECORD";
        private const ulong RunSeed = 0x60_1D_E4_3A_57E2;
        private const int GamesPerPlayerCount = 40;

        private static readonly Func<int, Player>[] Agents =
        {
            id => new DeterministicAI(id),
            id => new RandomChoiceAI(id),
            id => new PriorityWonderRandomChoiceAI(id),
            id => new PriorityStructureRandomChoiceAI(id),
        };

        private static string MasterPath([CallerFilePath] string thisFile = "") =>
            Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(thisFile))!, "GoldenMaster", "golden-master.txt");

        [TestMethod]
        public void Games_MatchTheGoldenMaster()
        {
            List<string> actual = PlayAll();
            string path = MasterPath();

            if (Environment.GetEnvironmentVariable(RecordEnvVar) == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, string.Join("\n", actual) + "\n");
                Assert.Inconclusive($"Recorded {actual.Count} lines to {path}. Unset {RecordEnvVar} and commit the file.");
            }

            Assert.IsTrue(File.Exists(path), $"No golden master at {path}. Record it with {RecordEnvVar}=1.");
            string[] expected = File.ReadAllLines(path).Where(l => l.Length > 0).ToArray();

            for (int i = 0; i < Math.Min(expected.Length, actual.Count); i++)
                if (expected[i] != actual[i])
                    Assert.Fail($"First difference at line {i + 1}:\nexpected: {expected[i]}\nactual:   {actual[i]}");
            Assert.AreEqual(expected.Length, actual.Count, "line count");
        }

        [TestMethod]
        public void Games_AreReproducible()
        {
            // the master is only useful if a game depends on nothing but its seed
            CollectionAssert.AreEqual(PlayGame(5, 3), PlayGame(5, 3));
        }

        private static List<string> PlayAll()
        {
            var lines = new List<string>();
            for (int players = 3; players <= 7; players++)
                for (int k = 0; k < GamesPerPlayerCount; k++)
                    lines.AddRange(PlayGame(players, k));
            return lines;
        }

        /// <summary>One line per seat, then one for the discard pile.</summary>
        private static List<string> PlayGame(int players, int k)
        {
            ulong seed = Seeding.GameSeed(RunSeed, players * 1000 + k);
            var game = new Game(seed)
            {
                // rotate the line-up so every agent plays every seat
                Players = Enumerable.Range(0, players).Select(seat => Agents[(k + seat) % Agents.Length](seat)).ToList(),
            };

            WithDatasetFor(players, () =>
            {
                game.InitGame();
                game.Loop(record: true);
            });

            var lines = new List<string>();
            string prefix = $"p{players} g{k} seed={seed}";
            foreach (Player p in game.Players)
            {
                lines.Add($"{prefix} seat={p.Id} {p.GetType().Name} {p.Board.Id} stage={p.Board.Stage} " +
                          $"vp={p.VictoryPoints} coins={p.Resources[Resource.Coins]} " +
                          $"military=+{p.VictoryTokens}/-{p.DefeatTokens} " +
                          $"built={string.Join(",", p.PlayedCards.Select(c => c.Id))}");
            }
            lines.Add($"{prefix} discarded={string.Join(",", game.DiscardedCards.Select(c => c.Id))}");
            return lines;
        }

        /// <summary>
        /// Until E7 is fixed, <see cref="CardsDataset.GetInstance"/> keeps the first player count it sees.
        /// Swap in the right one for this game and put the old one back (tests run one at a time).
        /// </summary>
        private static void WithDatasetFor(int players, Action action)
        {
            FieldInfo? field = typeof(CardsDataset).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null)
            {
                action();   // E7 fixed: no singleton any more
                return;
            }

            object? saved = field.GetValue(null);
            try
            {
                field.SetValue(null, null);
                CardsDataset.GetInstance(players);
                action();
            }
            finally
            {
                field.SetValue(null, saved);
            }
        }
    }
}
