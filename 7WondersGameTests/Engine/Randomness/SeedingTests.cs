using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Randomness.Services;

namespace _7WondersGameTests.Engine.Randomness;

/// <summary>
/// The same seed gives the same game; nothing random depends on the clock.
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
            .Select(gameIndex => PlayGame(Seeding.GameSeed(7, gameIndex)).setup)
            .ToList();

        Assert.AreEqual(10, setups.Distinct().Count());
    }

    [TestMethod]
    public void AgentRandom_IsPerSeatAndSeeded()
    {
        Game firstGame = new Game(123);
        Game secondGame = new Game(123);

        int[] seat0 = Draw(firstGame.AgentRandom(0));
        CollectionAssert.AreEqual(seat0, Draw(secondGame.AgentRandom(0)), "same seed, same seat");
        CollectionAssert.AreNotEqual(seat0, Draw(new Game(123).AgentRandom(1)), "other seat");
        CollectionAssert.AreNotEqual(seat0, Draw(new Game(124).AgentRandom(0)), "other seed");
        Assert.AreSame(firstGame.AgentRandom(0), firstGame.AgentRandom(0), "one stream per seat");
    }

    [TestMethod]
    public void DeepCopy_DoesNotUseUpTheGamesRandomStreams()
    {
        Game searched = NewGame(55);
        searched.SetUpRandomGame();
        Game untouched = NewGame(55);
        untouched.SetUpRandomGame();

        // a search makes copies and draws from them
        for (int copyIndex = 0; copyIndex < 5; copyIndex++)
        {
            Game copy = searched.DeepCopy();
            Draw(copy.AgentRandom(0));
            copy.LegalMovesPruned(0);
        }

        CollectionAssert.AreEqual(Draw(untouched.AgentRandom(0)), Draw(searched.AgentRandom(0)), "agent stream");
        CollectionAssert.AreEqual(
            untouched.LegalMovesPruned(0).Select(MoveText).ToList(),
            searched.LegalMovesPruned(0).Select(MoveText).ToList(),
            "pruning stream");
    }

    [TestMethod]
    public void DeepCopies_GetDifferentSeeds()
    {
        Game game = NewGame(9);
        game.SetUpRandomGame();
        ulong first = game.DeepCopy().Seed;
        ulong second = game.DeepCopy().Seed;

        Assert.AreNotEqual(game.Seed, first);
        Assert.AreNotEqual(first, second);
    }

    // ===== helpers =====

    private static Game NewGame(ulong seed)
    {
        Game game = new Game(seed);
        game.Agents.Add(new RandomAgent());
        game.Agents.Add(new HeuristicAgent());
        game.Agents.Add(new RandomAgent());
        return game;
    }

    /// <returns>The deal as text, and each seat's final VP.</returns>
    private static (string setup, List<int> scores) PlayGame(ulong seed)
    {
        Game game = NewGame(seed);
        game.SetUpRandomGame();

        List<string> dealParts = new List<string>();
        foreach (List<Card> deck in game.Decks)
        {
            dealParts.Add(DeckText(deck));
        }

        dealParts.Add(string.Join(",", game.Players.Select(player => player.Wonder.Id)));
        string setup = string.Join(" | ", dealParts);

        Dictionary<int, int> scores = game.Loop();
        List<int> scoresInSeatOrder = scores
            .OrderBy(seatScore => seatScore.Key)
            .Select(seatScore => seatScore.Value)
            .ToList();
        return (setup, scoresInSeatOrder);
    }

    private static string DeckText(List<Card> deck)
    {
        return string.Join(",", deck.Select(card => (int)card.Id));
    }

    // the move's action and card name, or an empty name for a move without a card
    private static string MoveText(Move move)
    {
        string cardName = "";
        if (move.Card != null)
        {
            cardName = move.Card.Name;
        }

        return $"{move.Action}:{cardName}";
    }

    private static int[] Draw(Random random)
    {
        int[] draws = new int[8];
        for (int drawIndex = 0; drawIndex < draws.Length; drawIndex++)
        {
            draws[drawIndex] = random.Next();
        }

        return draws;
    }
}
