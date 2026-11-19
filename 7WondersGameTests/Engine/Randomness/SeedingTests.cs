using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Matches;
using _7WondersGame.Cli.Results;
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
            Game gameA = SetUpGame(seed);
            string setupA = SetupText(gameA);
            List<int> scoresA = PlayToTheEnd(gameA);
            Game gameB = SetUpGame(seed);
            string setupB = SetupText(gameB);
            List<int> scoresB = PlayToTheEnd(gameB);

            Assert.AreEqual(setupA, setupB, $"deal, seed {seed}");
            CollectionAssert.AreEqual(scoresA, scoresB, $"final scores, seed {seed}");
        }
    }

    [TestMethod]
    public void DifferentGameIndex_GivesDifferentDeal()
    {
        List<string> setups = Enumerable.Range(0, 10)
            .Select(gameIndex => SetupText(SetUpGame(Seeding.GameSeed(7, gameIndex))))
            .ToList();

        Assert.AreEqual(10, setups.Distinct().Count());
    }

    [TestMethod]
    public void SeatRandom_IsPerSeatAndSeeded()
    {
        Game firstGame = new(123);
        Game secondGame = new(123);

        int[] seat0 = Draw(firstGame.SeatRandom(0));
        CollectionAssert.AreEqual(seat0, Draw(secondGame.SeatRandom(0)), "same seed, same seat");
        CollectionAssert.AreNotEqual(seat0, Draw(new Game(123).SeatRandom(1)), "other seat");
        CollectionAssert.AreNotEqual(seat0, Draw(new Game(124).SeatRandom(0)), "other seed");
        Assert.AreSame(firstGame.SeatRandom(0), firstGame.SeatRandom(0), "one stream per seat");
    }

    [TestMethod]
    public void DeepCopy_DoesNotUseUpTheGamesRandomStreams()
    {
        Game searched = SetUpGame(55);
        Game untouched = SetUpGame(55);

        // a search makes copies and draws from them
        for (int copyIndex = 0; copyIndex < 5; copyIndex++)
        {
            Game copy = searched.DeepCopy();
            Draw(copy.SeatRandom(0));
            copy.LegalMovesPruned(0);
        }

        CollectionAssert.AreEqual(Draw(untouched.SeatRandom(0)), Draw(searched.SeatRandom(0)), "agent stream");
        CollectionAssert.AreEqual(
            untouched.LegalMovesPruned(0).Select(MoveText).ToList(),
            searched.LegalMovesPruned(0).Select(MoveText).ToList(),
            "pruning stream");
    }

    [TestMethod]
    public void DeepCopies_GetDifferentSeeds()
    {
        Game game = SetUpGame(9);
        ulong first = game.DeepCopy().Seed;
        ulong second = game.DeepCopy().Seed;

        Assert.AreNotEqual(game.Seed, first);
        Assert.AreNotEqual(first, second);
    }

    // ===== helpers =====

    /// <summary>A three-player game of the seed, dealt and not played yet.</summary>
    private static Game SetUpGame(ulong seed)
    {
        Game game = new(seed);
        game.SetUpRandomGame(3);
        return game;
    }

    /// <summary>The deal of a game set up and not played yet, as text: the era decks and the wonder boards.</summary>
    private static string SetupText(Game game)
    {
        List<string> dealParts = new();
        foreach (List<Card> deck in game.Decks)
        {
            dealParts.Add(DeckText(deck));
        }

        dealParts.Add(string.Join(",", game.Players.Select(player => player.Wonder.Id)));
        return string.Join(" | ", dealParts);
    }

    /// <summary>Plays the game to the end; each seat's final VP, in seat order.</summary>
    private static List<int> PlayToTheEnd(Game game)
    {
        List<IAgent> agents = new() { new RandomAgent(), new HeuristicAgent(), new RandomAgent() };
        List<SeatResult> results = new Match(game, agents).Play();
        return results.Select(result => result.VictoryPoints).ToList();
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
