using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.HiddenInformation;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Games.Enums;
using _7WondersGame.Engine.Games.Knowledge;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Randomness.Services;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGameTests.Engine.Games;

/// <summary>What each seat knows about hidden cards, and the guesses a fair search makes from it.</summary>
[TestClass]
public class KnowledgeTests
{
    private static Game NewGame(int players, int g)
    {
        var game = new Game(Seeding.GameSeed(31, players * 100 + g));

        // a mix, so wonder stages, discards, Babylon B and Halikarnassos all happen
        for (int seat = 0; seat < players; seat++)
        {
            if (seat % 2 == 0)
            {
                game.Agents.Add(new HeuristicAI());
            }
            else
            {
                game.Agents.Add(new RandomChoiceAI());
            }
        }

        game.InitGame();
        return game;
    }

    /// <summary>Whether the game waits for anyone's move (it doesn't once it is over).</summary>
    private static bool HasPendingPlayers(Game game)
    {
        return game.PendingPlayers().Count > 0;
    }

    /// <summary>Every pending player chooses from the same state, then all their moves are submitted.</summary>
    private static void PlayOneStep(Game game)
    {
        List<int> pendingSeats = new List<int>(game.PendingPlayers());
        List<Command> moves = new List<Command>();
        foreach (int seat in pendingSeats)
        {
            moves.Add(game.Agents[seat].Choose(new GameView(game, seat)));
        }

        for (int pendingIndex = 0; pendingIndex < pendingSeats.Count; pendingIndex++)
        {
            game.Submit(pendingSeats[pendingIndex], moves[pendingIndex]);
        }
    }

    private static Dictionary<CardId, int> Count(IEnumerable<Card> cards)
    {
        return cards
            .GroupBy(card => card.Id)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    private static bool SubsetOf(IEnumerable<Card> part, IEnumerable<Card> whole)
    {
        var w = Count(whole);
        return Count(part).All(cardCount => w.GetValueOrDefault(cardCount.Key) >= cardCount.Value);
    }

    private static bool SameCards(IEnumerable<Card> a, IEnumerable<Card> b)
    {
        return SubsetOf(a, b) && SubsetOf(b, a);
    }

    // =========== knowledge ===========

    [DataTestMethod]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    public void Knowledge_IsTrue_AndGrowsAsHandsPass(int players)
    {
        int halikarnassosLooks = 0;
        for (int g = 0; g < 8; g++)
        {
            Game game = NewGame(players, g);
            while (HasPendingPlayers(game))
            {
                halikarnassosLooks += CheckEverySeatsKnowledge(game, players);
                PlayOneStep(game);
            }
        }

        Assert.IsTrue(halikarnassosLooks > 0 || players == 3, "no Halikarnassos build happened; pick other seeds");
    }

    /// <summary>
    /// Checks what every seat knows against the real cards.
    /// </summary>
    /// <returns>How many seats are building from the discard pile with Halikarnassos now.</returns>
    private static int CheckEverySeatsKnowledge(Game game, int players)
    {
        int halikarnassosLooks = 0;
        for (int o = 0; o < players; o++)
        {
            SeatKnowledge k = game.KnowledgeOf(o)!;
            string where = $"observer {o}, {game.Phase}, turn {game.Turn}";
            Assert.IsTrue(SameCards(k.InHand[o], game.Players[o].HandCards), where);
            for (int h = 0; h < players; h++)
            {
                if (k.InHand[h].Count > 0)
                {
                    Assert.IsTrue(SubsetOf(game.Players[h].HandCards, k.InHand[h]), $"{where}, hand of {h}");
                }
            }

            Assert.IsTrue(SubsetOf(k.InPile, game.DiscardedCards), where);
            Assert.AreEqual(game.Players[o].Board.Stage, k.Buried.Count, where);

            if (game.Phase == Phase.ChooseMoves)
            {
                // one more hand seen each turn, until every hand has been round
                int seen = Enumerable.Range(0, players)
                    .Count(handOwner => handOwner != o && k.InHand[handOwner].Count > 0);
                Assert.AreEqual(Math.Min(game.Turn % 7, players - 1), seen, where);
            }

            if (game.Phase == Phase.DiscardBuild && game.PendingPlayers().Contains(o))
            {
                Assert.IsTrue(SameCards(k.InPile, game.DiscardedCards), where);
                halikarnassosLooks++;
            }
        }

        return halikarnassosLooks;
    }

    [TestMethod]
    public void AHiddenMove_StaysHidden_ABuildDoesNot()
    {
        Game game = NewGame(3, 0);
        PlayerState p0 = game.Players[0];
        PlayerState p1 = game.Players[1];
        Card discarded = p0.HandCards[0];
        Card built = p1.GetPlayableCards(p1.HandCards).First();

        game.Submit(0, new Command(ActionType.Discard, discarded));
        game.Submit(1, new Command(ActionType.Build, built));
        game.Submit(2, new Command(ActionType.Discard, game.Players[2].HandCards[0]));

        // era 1 passes west: seat 0's old hand is now seat 2's, seat 1's is seat 0's
        SeatKnowledge seat0 = game.KnowledgeOf(0)!;
        SeatKnowledge seat1 = game.KnowledgeOf(1)!;
        CollectionAssert.Contains(seat0.InPile.ToList(), discarded);
        Assert.AreEqual(6, seat0.InHand[2].Count);
        CollectionAssert.DoesNotContain(seat0.InHand[2].ToList(), discarded);
        Assert.AreEqual(0, seat1.InPile.Count);
        // seat 1 passed its hand to seat 0 and knows it without the card it built
        Assert.IsTrue(SameCards(seat1.InHand[0], game.Players[0].HandCards));
        CollectionAssert.DoesNotContain(seat1.InHand[0].ToList(), built);
        Assert.IsNull(game.DeepCopy().KnowledgeOf(0), "a copy doesn't keep track");
    }

    // =========== determinization ===========

    [DataTestMethod]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    public void ASample_KeepsWhatTheSeatKnows_AndEveryCardAddsUp(int players)
    {
        var rng = new Random(1);
        for (int g = 0; g < 4; g++)
        {
            Game game = NewGame(players, g);
            while (HasPendingPlayers(game))
            {
                foreach (int seat in game.PendingPlayers())
                {
                    CheckASampleFor(game, seat, players, rng);
                }

                PlayOneStep(game);
            }
        }
    }

    /// <summary>A guess at the hidden cards for <paramref name="seat"/> keeps everything the seat knows.</summary>
    private static void CheckASampleFor(Game game, int seat, int players, Random rng)
    {
        SeatKnowledge known = game.KnowledgeOf(seat)!;
        Game s = Determinization.Sample(game, seat, known, rng);
        string where = $"seat {seat}, {game.Phase}, turn {game.Turn}";

        Assert.AreEqual(game.Phase, s.Phase, where);
        CollectionAssert.AreEquivalent(game.PendingPlayers().ToList(), s.PendingPlayers().ToList(), where);
        Assert.IsTrue(SameCards(game.Players[seat].HandCards, s.Players[seat].HandCards), where);
        Assert.AreEqual(game.DiscardedCards.Count, s.DiscardedCards.Count, where);
        Assert.IsTrue(SubsetOf(known.InPile, s.DiscardedCards), where);
        if (game.Phase == Phase.DiscardBuild)
        {
            Assert.IsTrue(SameCards(game.DiscardedCards, s.DiscardedCards), where);
        }

        // the seat's own legal moves don't depend on the guess
        CollectionAssert.AreEquivalent(game.LegalMoves(seat), s.LegalMoves(seat), where);

        for (int h = 0; h < players; h++)
        {
            PlayerState real = game.Players[h];
            PlayerState guessed = s.Players[h];
            Assert.AreEqual(real.HandCards.Count, guessed.HandCards.Count, where);
            Assert.AreEqual(real.PlayedCards.Count, guessed.PlayedCards.Count, where);
            Assert.AreEqual(real.Resources[Resource.Coins], guessed.Resources[Resource.Coins], where);
            if (h != seat && known.InHand[h].Count > 0)
            {
                Assert.IsTrue(SubsetOf(guessed.HandCards, known.InHand[h]), $"{where}, hand of {h}");
            }

            if (h != seat && game.Era == s.Era)
            {
                Assert.IsTrue(guessed.HandCards.All(card => card.Era == game.Era), where);
            }
        }

        CheckEveryCardAddsUp(s, players, where);
    }

    /// <summary>No card more often than it exists, players + 2 guilds in all, and full decks for later eras.</summary>
    private static void CheckEveryCardAddsUp(Game s, int players, string where)
    {
        var everything = s.Players
            .SelectMany(player => player.PlayedCards.Concat(player.HandCards))
            .Concat(s.DiscardedCards)
            .Concat(s.Decks.SelectMany(deck => deck))
            .ToList();
        foreach (var (id, count) in Count(everything))
        {
            Card card = CardsDataset.GameCards[id];
            int copies = CopiesInGame(card, players);
            Assert.IsTrue(count <= copies, $"{where}: {card.Name} {count} times");
        }

        Assert.IsTrue(CountGuilds(everything) <= players + 2, where);
        for (int e = s.Era + 1; e <= 3; e++)
        {
            int deckIndex = e - 1;
            Assert.AreEqual(7 * players, s.Decks[deckIndex].Count, where);
        }
    }

    // a guild is in the game once; other cards have a copy count per player count
    private static int CopiesInGame(Card card, int players)
    {
        if (card.Type == CardType.Guild)
        {
            return 1;
        }

        return card.CopiesFor(players);
    }

    private static int CountGuilds(List<Card> cards)
    {
        return cards
            .Select(card => card.Id)
            .Distinct()
            .Count(cardId => CardsDataset.GameCards[cardId].Type == CardType.Guild);
    }

    [TestMethod]
    public void ASample_UsesNothingTheSeatCantSee()
    {
        Game game = NewGame(4, 1);
        // a few turns in: seat 0 has seen some hands but not seat 1's
        for (int t = 0; t < 2; t++)
        {
            foreach (int seat in game.PendingPlayers())
            {
                game.Submit(seat, new HeuristicAI().Choose(new GameView(game, seat)));
            }
        }

        SeatKnowledge known = game.KnowledgeOf(0)!;
        Assert.AreEqual(0, known.InHand[1].Count, "era 1 passes west, so seat 1's hand hasn't reached seat 0");

        // the same public state and knowledge, other hidden cards: seat 1's unseen hand, the later decks, the pile
        // order
        Game other = game.DeepCopy();
        other.Players[1].HandCards = other.Players[1].HandCards.AsEnumerable().Reverse().ToList();
        other.Decks[1] = other.Decks[1].AsEnumerable().Reverse().ToList();
        other.Decks[2] = other.Decks[2].Skip(3).Concat(other.Decks[2].Take(3)).ToList();
        other.DiscardedCards = other.DiscardedCards.AsEnumerable().Reverse().ToList();

        Game a = Determinization.Sample(game, 0, known, new Random(9));
        Game b = Determinization.Sample(other, 0, known, new Random(9));

        Assert.AreEqual(HiddenCards(a), HiddenCards(b));
        Assert.AreNotEqual(HiddenCards(game), HiddenCards(a), "the guess re-deals what the seat can't see");
    }

    // every hand, the discard pile (sorted: its order is hidden too) and the decks, as text
    private static string HiddenCards(Game game)
    {
        string hands = string.Join(" | ", game.Players.Select(player => CardIdsText(player.HandCards)));
        string discardPile = string.Join(",", game.DiscardedCards.Select(card => card.Id).OrderBy(cardId => cardId));
        string decks = string.Join(" / ", game.Decks.Select(CardIdsText));
        return hands + " # " + discardPile + " # " + decks;
    }

    private static string CardIdsText(List<Card> cards)
    {
        return string.Join(",", cards.Select(card => card.Id));
    }

    [TestMethod]
    public void ASample_PlaysToTheEnd()
    {
        var rng = new Random(3);
        for (int players = 3; players <= 7; players += 2)
        {
            Game game = NewGame(players, 5);
            Game s = Determinization.Sample(game, 0, game.KnowledgeOf(0), rng);
            List<IAgent> heuristicAgents = new List<IAgent>();
            for (int seat = 0; seat < players; seat++)
            {
                heuristicAgents.Add(new HeuristicAI());
            }

            s.Agents = heuristicAgents;
            s.Loop();
            Assert.AreEqual(Phase.GameOver, s.Phase);
        }
    }
}
