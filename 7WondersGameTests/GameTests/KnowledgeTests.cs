using _7WondersGame.src.models;
using _7WondersGame.src.models.Agents;
using _7WondersGame.src.models.Effects;
using _7WondersGame.src.models.Players;

namespace _7WondersGameTests.GameTests
{
    /// <summary>Step 6 (M1): what each seat knows about hidden cards, and the guesses a fair search makes from it.</summary>
    [TestClass]
    public class KnowledgeTests
    {
        private static Game NewGame(int players, int g)
        {
            var game = new Game(Seeding.GameSeed(31, players * 100 + g))
            {
                // a mix, so wonder stages, discards, Babylon B and Halikarnassos all happen
                Agents = Enumerable.Range(0, players)
                    .Select(s => s % 2 == 0 ? (IAgent)new HeuristicAI() : new RandomChoiceAI()).ToList(),
            };
            game.InitGame();
            return game;
        }

        /// <summary>Plays the game to the end, calling <paramref name="check"/> before every decision step.</summary>
        private static void Play(Game game, Action<Game> check)
        {
            while (game.PendingPlayers() is { Count: > 0 } pending)
            {
                check(game);
                var moves = pending.Select(s => (s, game.Agents[s].Choose(new GameView(game, s)))).ToList();
                foreach (var (seat, move) in moves)
                    game.Submit(seat, move);
            }
        }

        private static Dictionary<CardId, int> Count(IEnumerable<Card> cards) =>
            cards.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.Count());

        private static bool SubsetOf(IEnumerable<Card> part, IEnumerable<Card> whole)
        {
            var w = Count(whole);
            return Count(part).All(kv => w.GetValueOrDefault(kv.Key) >= kv.Value);
        }

        private static bool SameCards(IEnumerable<Card> a, IEnumerable<Card> b) => SubsetOf(a, b) && SubsetOf(b, a);

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
                Play(game, game =>
                {
                    for (int o = 0; o < players; o++)
                    {
                        SeatKnowledge k = game.KnowledgeOf(o)!;
                        string where = $"observer {o}, {game.Phase}, turn {game.Turn}";
                        Assert.IsTrue(SameCards(k.InHand[o], game.Players[o].HandCards), where);
                        for (int h = 0; h < players; h++)
                            if (k.InHand[h].Count > 0)
                                Assert.IsTrue(SubsetOf(game.Players[h].HandCards, k.InHand[h]), $"{where}, hand of {h}");
                        Assert.IsTrue(SubsetOf(k.InPile, game.DiscardedCards), where);
                        Assert.AreEqual(game.Players[o].Board.Stage, k.Buried.Count, where);

                        if (game.Phase == Phase.ChooseMoves)
                        {
                            // one more hand seen each turn, until every hand has been round
                            int seen = Enumerable.Range(0, players).Count(h => h != o && k.InHand[h].Count > 0);
                            Assert.AreEqual(Math.Min(game.Turn % 7, players - 1), seen, where);
                        }
                        if (game.Phase == Phase.DiscardBuild && game.PendingPlayers().Contains(o))
                        {
                            Assert.IsTrue(SameCards(k.InPile, game.DiscardedCards), where);
                            halikarnassosLooks++;
                        }
                    }
                });
            }
            Assert.IsTrue(halikarnassosLooks > 0 || players == 3, "no Halikarnassos build happened; pick other seeds");
        }

        [TestMethod]
        public void AHiddenMove_StaysHidden_ABuildDoesNot()
        {
            Game game = NewGame(3, 0);
            PlayerState p0 = game.Players[0], p1 = game.Players[1];
            Card discarded = p0.HandCards[0];
            Card built = p1.GetPlayableCards(p1.HandCards).First();

            game.Submit(0, new Command(ActionType.Discard, discarded));
            game.Submit(1, new Command(ActionType.Build, built));
            game.Submit(2, new Command(ActionType.Discard, game.Players[2].HandCards[0]));

            // era 1 passes west: seat 0's old hand is now seat 2's, seat 1's is seat 0's
            SeatKnowledge seat0 = game.KnowledgeOf(0)!, seat1 = game.KnowledgeOf(1)!;
            CollectionAssert.Contains(seat0.InPile.ToList(), discarded);
            Assert.AreEqual(6, seat0.InHand[2].Count);
            CollectionAssert.DoesNotContain(seat0.InHand[2].ToList(), discarded);
            Assert.AreEqual(0, seat1.InPile.Count);
            // seat 1 passed its hand to seat 0 and knows it without the card it built
            Assert.IsTrue(SameCards(seat1.InHand[0], game.Players[0].HandCards));
            CollectionAssert.DoesNotContain(seat1.InHand[0].ToList(), built);
            Assert.AreEqual(0, game.DeepCopy().KnowledgeOf(0) == null ? 0 : 1, "a copy doesn't keep track");
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
                Play(game, game =>
                {
                    foreach (int seat in game.PendingPlayers())
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
                            Assert.IsTrue(SameCards(game.DiscardedCards, s.DiscardedCards), where);
                        // the seat's own legal moves don't depend on the guess
                        CollectionAssert.AreEquivalent(game.LegalMoves(seat), s.LegalMoves(seat), where);

                        for (int h = 0; h < players; h++)
                        {
                            Assert.AreEqual(game.Players[h].HandCards.Count, s.Players[h].HandCards.Count, where);
                            Assert.AreEqual(game.Players[h].PlayedCards.Count, s.Players[h].PlayedCards.Count, where);
                            Assert.AreEqual(game.Players[h].Resources[Resource.Coins], s.Players[h].Resources[Resource.Coins], where);
                            if (h != seat && known.InHand[h].Count > 0)
                                Assert.IsTrue(SubsetOf(s.Players[h].HandCards, known.InHand[h]), $"{where}, hand of {h}");
                            if (h != seat && game.Era == s.Era)
                                Assert.IsTrue(s.Players[h].HandCards.All(c => c.Era == game.Era), where);
                        }

                        // no card more often than it exists, and players + 2 guilds in all
                        var everything = s.Players.SelectMany(p => p.PlayedCards.Concat(p.HandCards))
                            .Concat(s.DiscardedCards).Concat(s.Decks.SelectMany(d => d)).ToList();
                        foreach (var (id, count) in Count(everything))
                        {
                            Card card = CardsDataset.GameCards[id];
                            int copies = card.Type == CardType.Guild ? 1 : card.CopiesFor(players);
                            Assert.IsTrue(count <= copies, $"{where}: {card.Name} {count} times");
                        }
                        Assert.IsTrue(everything.Select(c => c.Id).Distinct().Count(id => CardsDataset.GameCards[id].Type == CardType.Guild) <= players + 2, where);
                        for (int e = s.Era + 1; e <= 3; e++)
                            Assert.AreEqual(7 * players, s.Decks[e - 1].Count, where);
                    }
                });
            }
        }

        [TestMethod]
        public void ASample_UsesNothingTheSeatCantSee()
        {
            Game game = NewGame(4, 1);
            // a few turns in: seat 0 has seen some hands but not seat 1's
            for (int t = 0; t < 2; t++)
                foreach (int seat in game.PendingPlayers())
                    game.Submit(seat, new HeuristicAI().Choose(new GameView(game, seat)));
            SeatKnowledge known = game.KnowledgeOf(0)!;
            Assert.AreEqual(0, known.InHand[1].Count, "era 1 passes west, so seat 1's hand hasn't reached seat 0");

            // the same public state and knowledge, other hidden cards: seat 1's unseen hand, the later decks, the pile order
            Game other = game.DeepCopy();
            other.Players[1].HandCards = other.Players[1].HandCards.AsEnumerable().Reverse().ToList();
            other.Decks[1] = other.Decks[1].AsEnumerable().Reverse().ToList();
            other.Decks[2] = other.Decks[2].Skip(3).Concat(other.Decks[2].Take(3)).ToList();
            other.DiscardedCards = other.DiscardedCards.AsEnumerable().Reverse().ToList();

            Game a = Determinization.Sample(game, 0, known, new Random(9));
            Game b = Determinization.Sample(other, 0, known, new Random(9));

            string Hidden(Game g) => string.Join(" | ", g.Players.Select(p => string.Join(",", p.HandCards.Select(c => c.Id))))
                + " # " + string.Join(",", g.DiscardedCards.Select(c => c.Id).OrderBy(id => id))
                + " # " + string.Join(" / ", g.Decks.Select(d => string.Join(",", d.Select(c => c.Id))));
            Assert.AreEqual(Hidden(a), Hidden(b));
            Assert.AreNotEqual(Hidden(game), Hidden(a), "the guess re-deals what the seat can't see");
        }

        [TestMethod]
        public void ASample_PlaysToTheEnd()
        {
            var rng = new Random(3);
            for (int players = 3; players <= 7; players += 2)
            {
                Game game = NewGame(players, 5);
                Game s = Determinization.Sample(game, 0, game.KnowledgeOf(0), rng);
                s.Agents = Enumerable.Range(0, players).Select(_ => (IAgent)new HeuristicAI()).ToList();
                s.Loop();
                Assert.AreEqual(Phase.GameOver, s.Phase);
            }
        }
    }
}
