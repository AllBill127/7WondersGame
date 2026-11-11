using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models
{
    /// <summary>
    /// One guess at the hidden cards, from one seat's point of view (fix plan step 6, M1): a copy of the game in which
    /// everything that seat can't know is dealt again at random, consistent with what it does know
    /// (<see cref="SeatKnowledge"/>). What it can't know:
    /// <list type="bullet">
    /// <item>the other players' hands, except cards it saw passing by;</item>
    /// <item>the discard pile (face down), except its own discards and what it saw looking through it;</item>
    /// <item>the later eras' decks and which guilds are in play.</item>
    /// </list>
    /// The public state (cities, coins, wonder stages, hand and pile sizes) and the seat's own hand stay as they are.
    /// </summary>
    public static class Determinization
    {
        /// <param name="game">The game to copy (not changed).</param>
        /// <param name="known">What the seat knows; null means only its own hand and the public state.</param>
        public static Game Sample(Game game, int seat, SeatKnowledge? known, Random rng)
        {
            Game copy = game.DeepCopy();
            int n = copy.Players.Count;
            int era = copy.Era;
            if (era > 3)
                return copy;
            List<PlayerState> players = copy.Players;
            IReadOnlyList<Card> inPile = known?.InPile ?? Array.Empty<Card>();
            IReadOnlyList<Card> buried = known?.Buried ?? Array.Empty<Card>();
            IReadOnlyList<Card> InHand(int h) => known?.InHand[h] ?? Array.Empty<Card>();

            // every card of eras 1 to now, with the guilds in play: the ones the seat has seen, plus random others
            var seen = new List<Card>(players.SelectMany(p => p.PlayedCards));
            seen.AddRange(players[seat].HandCards);
            for (int h = 0; h < n; h++)
                seen.AddRange(InHand(h));
            seen.AddRange(inPile);
            seen.AddRange(buried);
            var pool = new List<Card>();
            for (int e = 1; e <= era; e++)
                pool.AddRange(Deck(e, n, seen, rng));

            // take out the cards the seat knows where they are
            foreach (Card c in players.SelectMany(p => p.PlayedCards))
                pool.Remove(c);
            foreach (Card c in players[seat].HandCards)
                pool.Remove(c);
            foreach (Card c in buried)
                pool.Remove(c);
            List<Card> pileKnown = inPile.Where(pool.Remove).ToList();

            // each other hand: the cards seen in it (some of them may be gone face down), then unseen cards
            var hands = new List<Card>?[n];
            // cards seen in a hand that this guess says were buried or discarded since
            var goneFaceDown = new List<Card>();
            for (int h = 0; h < n; h++)
            {
                if (h == seat)
                    continue;
                int size = players[h].HandCards.Count;
                List<Card> candidates = InHand(h).Where(pool.Remove).ToList();
                Shuffle(candidates, rng);
                hands[h] = candidates.Take(size).ToList();
                goneFaceDown.AddRange(candidates.Skip(size));
            }
            List<Card> unseenThisEra = pool.Where(c => c.Era == era).ToList();
            Shuffle(unseenThisEra, rng);
            int next = 0;
            for (int h = 0; h < n; h++)
            {
                if (hands[h] is not { } hand)
                    continue;
                while (hand.Count < players[h].HandCards.Count)
                {
                    if (next == unseenThisEra.Count)
                        throw new InvalidOperationException($"Not enough unseen era {era} cards to fill seat {h}'s hand (seat {seat}'s guess).");
                    Card c = unseenThisEra[next++];
                    pool.Remove(c);
                    hand.Add(c);
                }
                players[h].HandCards = hand;
            }

            // the discard pile: what the seat knows is there, then cards gone face down
            List<Card> rest = goneFaceDown.Concat(pool).ToList();
            Shuffle(rest, rng);
            int unknownInPile = copy.DiscardedCards.Count - pileKnown.Count;
            if (unknownInPile > rest.Count)
                throw new InvalidOperationException($"Not enough unseen cards to fill the discard pile (seat {seat}'s guess).");
            copy.DiscardedCards = pileKnown.Concat(rest.Take(unknownInPile)).ToList();

            // the decks: dealt ones are used up; later ones are new shuffles
            for (int e = 1; e <= 3; e++)
            {
                if (e <= era)
                    copy.Decks[e - 1] = new List<Card>();
                else
                {
                    List<Card> deck = Deck(e, n, Array.Empty<Card>(), rng);
                    Shuffle(deck, rng);
                    copy.Decks[e - 1] = deck;
                }
            }
            return copy;
        }

        /// <summary>
        /// The era's cards for this many players. In era 3, players + 2 guilds: every guild in <paramref name="seen"/>,
        /// then random other guilds.
        /// </summary>
        private static List<Card> Deck(int era, int players, IEnumerable<Card> seen, Random rng)
        {
            var deck = new List<Card>();
            foreach (Card card in CardsDataset.Cards.Values.Where(c => c.Era == era))
                for (int i = 0; i < card.CopiesFor(players); i++)
                    deck.Add(card);
            if (era == 3)
            {
                List<Card> guilds = seen.Where(c => c.Type == CardType.Guild).Distinct().ToList();
                List<Card> others = CardsDataset.GuildCards.Values.Except(guilds).ToList();
                Shuffle(others, rng);
                guilds.AddRange(others.Take(players + 2 - guilds.Count));
                deck.AddRange(guilds);
            }
            return deck;
        }

        private static void Shuffle(List<Card> cards, Random rng)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }
    }
}
