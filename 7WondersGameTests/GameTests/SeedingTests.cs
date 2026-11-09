using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;

namespace _7WondersGameTests.GameTests
{
    /// <summary>
    /// B1: the same seed gives the same game; nothing random depends on the clock.
    /// </summary>
    [TestClass]
    public class SeedingTests
    {
        [TestMethod]
        public void Nth_MatchesSplitMix64ReferenceOutputs()
        {
            // First outputs of the reference SplitMix64 generator started at 0.
            Assert.AreEqual(0xE220A8397B1DCDAFUL, Seeding.Nth(0, 0));
            Assert.AreEqual(0x6E789E6AA1B965F4UL, Seeding.Nth(0, 1));
            Assert.AreEqual(0x06C45D188009454FUL, Seeding.Nth(0, 2));
        }

        [TestMethod]
        public void SameSeed_GivesIdenticalGame()
        {
            for (ulong runSeed = 1; runSeed <= 10; runSeed++)
            {
                ulong seed = Seeding.GameSeed(runSeed, 0);
                var (setupA, scoresA) = PlayGame(seed);
                var (setupB, scoresB) = PlayGame(seed);

                Assert.AreEqual(setupA, setupB, $"deal, seed {seed}");
                CollectionAssert.AreEqual(scoresA, scoresB, $"final scores, seed {seed}");
            }
        }

        [TestMethod]
        public void DifferentGameIndex_GivesDifferentDeal()
        {
            var setups = Enumerable.Range(0, 10)
                .Select(i => PlayGame(Seeding.GameSeed(7, i)).setup)
                .ToList();

            Assert.AreEqual(10, setups.Distinct().Count());
        }

        [TestMethod]
        public void AgentRandom_IsPerSeatAndSeeded()
        {
            Game a = new Game(123);
            Game b = new Game(123);

            int[] seat0 = Draw(a.AgentRandom(0));
            CollectionAssert.AreEqual(seat0, Draw(b.AgentRandom(0)), "same seed, same seat");
            CollectionAssert.AreNotEqual(seat0, Draw(new Game(123).AgentRandom(1)), "other seat");
            CollectionAssert.AreNotEqual(seat0, Draw(new Game(124).AgentRandom(0)), "other seed");
            Assert.AreSame(a.AgentRandom(0), a.AgentRandom(0), "one stream per seat");
        }

        [TestMethod]
        public void DeepCopy_DoesNotUseUpTheGamesRandomStreams()
        {
            Game searched = NewGame(55);
            searched.InitGame();
            Game untouched = NewGame(55);
            untouched.InitGame();

            // a search makes copies and draws from them
            for (int n = 0; n < 5; n++)
            {
                Game copy = searched.DeepCopy();
                Draw(copy.AgentRandom(0));
                copy.GetLegalMovesPrunedDiscards(0);
            }

            CollectionAssert.AreEqual(Draw(untouched.AgentRandom(0)), Draw(searched.AgentRandom(0)), "agent stream");
            CollectionAssert.AreEqual(
                untouched.GetLegalMovesPrunedDiscards(0).Select(m => $"{m.Subcommand}:{m.Argument?.Name}").ToList(),
                searched.GetLegalMovesPrunedDiscards(0).Select(m => $"{m.Subcommand}:{m.Argument?.Name}").ToList(),
                "pruning stream");
        }

        [TestMethod]
        public void DeepCopies_GetDifferentSeeds()
        {
            Game game = NewGame(9);
            game.InitGame();
            ulong first = game.DeepCopy().Seed;
            ulong second = game.DeepCopy().Seed;

            Assert.AreNotEqual(game.Seed, first);
            Assert.AreNotEqual(first, second);
        }

        // ===== helpers =====

        private static Game NewGame(ulong seed)
        {
            Game game = new Game(seed);
            game.Players.Add(new RandomChoiceAI(0));
            game.Players.Add(new DeterministicAI(1));
            game.Players.Add(new RandomChoiceAI(2));
            return game;
        }

        /// <returns>The deal as text, and each seat's final VP.</returns>
        private static (string setup, List<int> scores) PlayGame(ulong seed)
        {
            Game game = NewGame(seed);
            game.InitGame();

            string setup = string.Join(" | ",
                game.Decks.Select(d => string.Join(",", d.Select(c => (int)c.Id)))
                    .Append(string.Join(",", game.Players.Select(p => p.Board.Id))));

            Dictionary<int, int> scores = game.Loop();
            return (setup, scores.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList());
        }

        private static int[] Draw(Random rng) => Enumerable.Range(0, 8).Select(_ => rng.Next()).ToArray();
    }
}
