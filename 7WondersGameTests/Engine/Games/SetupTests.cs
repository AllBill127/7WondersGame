using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGameTests.Engine.Games;

/// <summary>Setting up a game from a dataset deal.</summary>
[TestClass]
public class SetupTests
{
    /// <summary>
    /// A dataset game built from a random game's decks and wonders must start
    /// in exactly the same player state (dataset games used to miss the
    /// wonder's starting resource).
    /// </summary>
    [TestMethod]
    public void InitGameNotRandom_FromRandomGamesDeal_GivesSamePlayerState()
    {
        for (int repeat = 0; repeat < 20; repeat++)
        {
            Game random = NewGame((ulong)repeat);
            random.InitGame();

            List<int>[] dataset = DealOf(random);

            Game fromDataset = NewGame((ulong)repeat);
            fromDataset.InitGameNotRandom(dataset);

            for (int seat = 0; seat < random.Players.Count; seat++)
            {
                PlayerState expected = random.Players[seat];
                PlayerState actual = fromDataset.Players[seat];

                Assert.AreEqual(expected.Board.Id, actual.Board.Id, "wonder");
                CollectionAssert.AreEquivalent(expected.Resources.ToList(), actual.Resources.ToList(), "resources");
                Assert.AreEqual(1, actual.Resources[actual.Board.Production], "starting resource");
                CollectionAssert.AreEqual(
                    expected.HandCards.Select(card => card.Id).ToList(),
                    actual.HandCards.Select(card => card.Id).ToList(),
                    "hand");
            }
        }
    }

    // the three eras' decks, then the wonders, as card and wonder ids (the dataset file's layout)
    private static List<int>[] DealOf(Game game)
    {
        List<int>[] deal = new List<int>[4];
        for (int era = 0; era < 3; era++)
        {
            deal[era] = game.Decks[era].Select(card => (int)card.Id).ToList();
        }

        deal[3] = game.Players.Select(player => (int)player.Board.Id).ToList();
        return deal;
    }

    private static Game NewGame(ulong seed)
    {
        Game game = new Game(seed);
        for (int agentIndex = 0; agentIndex < 3; agentIndex++)
        {
            game.Agents.Add(new RandomChoiceAI());
        }

        return game;
    }
}
