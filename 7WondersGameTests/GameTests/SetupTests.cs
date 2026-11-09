using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;

namespace _7WondersGameTests.GameTests
{
    [TestClass]
    public class SetupTests
    {
        /// <summary>
        /// A dataset game built from a random game's decks and wonders must start
        /// in exactly the same player state (R3: dataset games used to miss the
        /// wonder's starting resource).
        /// </summary>
        [TestMethod]
        public void InitGameNotRandom_FromRandomGamesDeal_GivesSamePlayerState()
        {
            for (int repeat = 0; repeat < 20; repeat++)
            {
                Game random = NewGame((ulong)repeat);
                random.InitGame();

                List<int>[] dataset = new List<int>[4];
                for (int era = 0; era < 3; era++)
                    dataset[era] = random.Decks[era].Select(c => (int)c.Id).ToList();
                dataset[3] = random.Players.Select(p => (int)p.Board.Id).ToList();

                Game fromDataset = NewGame((ulong)repeat);
                fromDataset.InitGameNotRandom(dataset);

                for (int i = 0; i < random.Players.Count; i++)
                {
                    Player expected = random.Players[i];
                    Player actual = fromDataset.Players[i];

                    Assert.AreEqual(expected.Board.Id, actual.Board.Id, "wonder");
                    CollectionAssert.AreEquivalent(expected.Resources.ToList(), actual.Resources.ToList(), "resources");
                    Assert.AreEqual(1, actual.Resources[actual.Board.Production], "starting resource");
                    CollectionAssert.AreEqual(expected.HandCards.Select(c => c.Id).ToList(),
                                              actual.HandCards.Select(c => c.Id).ToList(), "hand");
                }
            }
        }

        private static Game NewGame(ulong seed)
        {
            Game game = new Game(seed);
            for (int i = 0; i < 3; i++)
                game.Players.Add(new RandomChoiceAI(i));
            return game;
        }
    }
}
