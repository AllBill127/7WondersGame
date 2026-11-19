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
    private static Game NewGame(int players, int gameIndex)
    {
        Game game = new(Seeding.GameSeed(31, players * 100 + gameIndex));

        // a mix, so wonder stages, discards, Babylon B and Halikarnassos all happen
        for (int seat = 0; seat < players; seat++)
        {
            if (seat % 2 == 0)
            {
                game.Agents.Add(new HeuristicAgent());
            }
            else
            {
                game.Agents.Add(new RandomAgent());
            }
        }

        game.SetUpRandomGame();
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
        List<int> pendingSeats = new(game.PendingPlayers());
        List<Move> moves = new();
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
        Dictionary<CardId, int> wholeCounts = Count(whole);
        return Count(part).All(cardCount => wholeCounts.GetValueOrDefault(cardCount.Key) >= cardCount.Value);
    }

    private static bool SameCards(IEnumerable<Card> first, IEnumerable<Card> second)
    {
        return SubsetOf(first, second) && SubsetOf(second, first);
    }

    // =========== knowledge ===========

    [DataTestMethod]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    public void Knowledge_IsTrue_AndGrowsAsHandsPass(int players)
    {
        int halikarnassosLooks = 0;
        for (int gameIndex = 0; gameIndex < 8; gameIndex++)
        {
            Game game = NewGame(players, gameIndex);
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
        for (int observerSeat = 0; observerSeat < players; observerSeat++)
        {
            SeatKnowledge? knowledge = game.KnowledgeOf(observerSeat);
            Assert.IsNotNull(knowledge);
            string where = $"observer {observerSeat}, {game.Phase}, turn {game.Turn}";
            Assert.IsTrue(SameCards(knowledge.InHand[observerSeat], game.Players[observerSeat].Hand), where);
            for (int handOwnerSeat = 0; handOwnerSeat < players; handOwnerSeat++)
            {
                if (knowledge.InHand[handOwnerSeat].Count > 0)
                {
                    Assert.IsTrue(SubsetOf(game.Players[handOwnerSeat].Hand, knowledge.InHand[handOwnerSeat]),
                                  $"{where}, hand of {handOwnerSeat}");
                }
            }

            Assert.IsTrue(SubsetOf(knowledge.InPile, game.DiscardPile), where);
            Assert.AreEqual(game.Players[observerSeat].Wonder.BuiltStageCount, knowledge.Buried.Count, where);

            if (game.Phase == Phase.ChooseMoves)
            {
                // one more hand seen each turn, until every hand has been round
                int seen = Enumerable.Range(0, players)
                    .Count(handOwner => handOwner != observerSeat && knowledge.InHand[handOwner].Count > 0);
                Assert.AreEqual(Math.Min(game.Turn % 7, players - 1), seen, where);
            }

            if (game.Phase == Phase.DiscardPileBuild && game.PendingPlayers().Contains(observerSeat))
            {
                Assert.IsTrue(SameCards(knowledge.InPile, game.DiscardPile), where);
                halikarnassosLooks++;
            }
        }

        return halikarnassosLooks;
    }

    [TestMethod]
    public void AHiddenMove_StaysHidden_ABuildDoesNot()
    {
        Game game = NewGame(3, 0);
        PlayerState firstPlayer = game.Players[0];
        PlayerState secondPlayer = game.Players[1];
        Card discarded = firstPlayer.Hand[0];
        Card built = BuildRules.BuildableCards(secondPlayer, secondPlayer.Hand).First();

        game.Submit(0, new Move(MoveAction.Discard, discarded));
        game.Submit(1, new Move(MoveAction.Build, built));
        game.Submit(2, new Move(MoveAction.Discard, game.Players[2].Hand[0]));

        // era 1 passes west: seat 0's old hand is now seat 2's, seat 1's is seat 0's
        SeatKnowledge? seat0 = game.KnowledgeOf(0);
        Assert.IsNotNull(seat0);
        SeatKnowledge? seat1 = game.KnowledgeOf(1);
        Assert.IsNotNull(seat1);
        CollectionAssert.Contains(seat0.InPile.ToList(), discarded);
        Assert.AreEqual(6, seat0.InHand[2].Count);
        CollectionAssert.DoesNotContain(seat0.InHand[2].ToList(), discarded);
        Assert.AreEqual(0, seat1.InPile.Count);
        // seat 1 passed its hand to seat 0 and knows it without the card it built
        Assert.IsTrue(SameCards(seat1.InHand[0], game.Players[0].Hand));
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
        Random random = new(1);
        for (int gameIndex = 0; gameIndex < 4; gameIndex++)
        {
            Game game = NewGame(players, gameIndex);
            while (HasPendingPlayers(game))
            {
                foreach (int seat in game.PendingPlayers())
                {
                    CheckASampleFor(game, seat, players, random);
                }

                PlayOneStep(game);
            }
        }
    }

    /// <summary>A guess at the hidden cards for <paramref name="seat"/> keeps everything the seat knows.</summary>
    private static void CheckASampleFor(Game game, int seat, int players, Random random)
    {
        SeatKnowledge? known = game.KnowledgeOf(seat);
        Assert.IsNotNull(known);
        Game sample = Determinization.Sample(game, seat, known, random);
        string where = $"seat {seat}, {game.Phase}, turn {game.Turn}";

        Assert.AreEqual(game.Phase, sample.Phase, where);
        CollectionAssert.AreEquivalent(game.PendingPlayers().ToList(), sample.PendingPlayers().ToList(), where);
        Assert.IsTrue(SameCards(game.Players[seat].Hand, sample.Players[seat].Hand), where);
        Assert.AreEqual(game.DiscardPile.Count, sample.DiscardPile.Count, where);
        Assert.IsTrue(SubsetOf(known.InPile, sample.DiscardPile), where);
        if (game.Phase == Phase.DiscardPileBuild)
        {
            Assert.IsTrue(SameCards(game.DiscardPile, sample.DiscardPile), where);
        }

        // the seat's own legal moves don't depend on the guess
        CollectionAssert.AreEquivalent(game.LegalMoves(seat), sample.LegalMoves(seat), where);

        for (int handOwnerSeat = 0; handOwnerSeat < players; handOwnerSeat++)
        {
            PlayerState real = game.Players[handOwnerSeat];
            PlayerState guessed = sample.Players[handOwnerSeat];
            Assert.AreEqual(real.Hand.Count, guessed.Hand.Count, where);
            Assert.AreEqual(real.BuiltCards.Count, guessed.BuiltCards.Count, where);
            Assert.AreEqual(real.Resources[Resource.Coins], guessed.Resources[Resource.Coins], where);
            if (handOwnerSeat != seat && known.InHand[handOwnerSeat].Count > 0)
            {
                Assert.IsTrue(SubsetOf(guessed.Hand, known.InHand[handOwnerSeat]),
                              $"{where}, hand of {handOwnerSeat}");
            }

            if (handOwnerSeat != seat && game.Era == sample.Era)
            {
                Assert.IsTrue(guessed.Hand.All(card => card.Era == game.Era), where);
            }
        }

        CheckEveryCardAddsUp(sample, players, where);
    }

    /// <summary>No card more often than it exists, players + 2 guilds in all, and full decks for later eras.</summary>
    private static void CheckEveryCardAddsUp(Game sample, int players, string where)
    {
        List<Card> everything = sample.Players
            .SelectMany(player => player.BuiltCards.Concat(player.Hand))
            .Concat(sample.DiscardPile)
            .Concat(sample.Decks.SelectMany(deck => deck))
            .ToList();
        foreach ((CardId id, int count) in Count(everything))
        {
            Card card = CardsDataset.GameCards[id];
            int copies = CopiesInGame(card, players);
            Assert.IsTrue(count <= copies, $"{where}: {card.Name} {count} times");
        }

        Assert.IsTrue(CountGuilds(everything) <= players + 2, where);
        for (int deckEra = sample.Era + 1; deckEra <= 3; deckEra++)
        {
            int deckIndex = deckEra - 1;
            Assert.AreEqual(7 * players, sample.Decks[deckIndex].Count, where);
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
        for (int turn = 0; turn < 2; turn++)
        {
            foreach (int seat in game.PendingPlayers())
            {
                game.Submit(seat, new HeuristicAgent().Choose(new GameView(game, seat)));
            }
        }

        SeatKnowledge? known = game.KnowledgeOf(0);
        Assert.IsNotNull(known);
        Assert.AreEqual(0, known.InHand[1].Count, "era 1 passes west, so seat 1's hand hasn't reached seat 0");

        // the same public state and knowledge, other hidden cards: seat 1's unseen hand, the later decks, the pile
        // order
        Game other = game.DeepCopy();
        other.Players[1].Hand = other.Players[1].Hand.AsEnumerable().Reverse().ToList();
        other.Decks[1] = other.Decks[1].AsEnumerable().Reverse().ToList();
        other.Decks[2] = other.Decks[2].Skip(3).Concat(other.Decks[2].Take(3)).ToList();
        other.DiscardPile = other.DiscardPile.AsEnumerable().Reverse().ToList();

        Game firstSample = Determinization.Sample(game, 0, known, new Random(9));
        Game secondSample = Determinization.Sample(other, 0, known, new Random(9));

        Assert.AreEqual(HiddenCards(firstSample), HiddenCards(secondSample));
        Assert.AreNotEqual(HiddenCards(game), HiddenCards(firstSample), "the guess re-deals what the seat can't see");
    }

    // every hand, the discard pile (sorted: its order is hidden too) and the decks, as text
    private static string HiddenCards(Game game)
    {
        string hands = string.Join(" | ", game.Players.Select(player => CardIdsText(player.Hand)));
        string discardPile = string.Join(",", game.DiscardPile.Select(card => card.Id).OrderBy(cardId => cardId));
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
        Random random = new(3);
        for (int players = 3; players <= 7; players += 2)
        {
            Game game = NewGame(players, 5);
            Game sample = Determinization.Sample(game, 0, game.KnowledgeOf(0), random);
            List<IAgent> heuristicAgents = new();
            for (int seat = 0; seat < players; seat++)
            {
                heuristicAgents.Add(new HeuristicAgent());
            }

            sample.Agents = heuristicAgents;
            sample.Loop();
            Assert.AreEqual(Phase.GameOver, sample.Phase);
        }
    }
}
