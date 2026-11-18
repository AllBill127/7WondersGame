using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Games.Knowledge;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Agents.HiddenInformation;

/// <summary>
/// One guess at the hidden cards, from one seat's point of view: a copy of the game in which everything that seat
/// can't know is dealt again at random, consistent with what it does know (<see cref="SeatKnowledge"/>). What it
/// can't know:
/// <list type="bullet">
/// <item>the other players' hands, except cards it saw passing by;</item>
/// <item>the discard pile (face down), except its own discards and what it saw looking through it;</item>
/// <item>the later eras' decks and which guilds are in play.</item>
/// </list>
/// The public state (cities, coins, wonder stages, hand and pile sizes) and the seat's own hand stay as they are.
/// </summary>
/// <remarks>Runs once per MCTS iteration: no LINQ, lambdas or extra allocations.</remarks>
public static class Determinization
{
    /// <summary>
    /// A copy of the game with the hidden cards dealt again, as <paramref name="seat"/> may guess them.
    /// </summary>
    /// <param name="game">The game to copy (not changed).</param>
    /// <param name="seat">The seat whose point of view the guess takes.</param>
    /// <param name="known">What the seat knows; null means only its own hand and the public state.</param>
    /// <param name="random">The random stream every shuffle draws from.</param>
    public static Game Sample(Game game, int seat, SeatKnowledge? known, Random random)
    {
        Game copy = game.DeepCopy();
        int playerCount = copy.Players.Count;
        int era = copy.Era;
        if (era > Game.LastEra)
        {
            return copy;
        }
        List<PlayerState> players = copy.Players;
        IReadOnlyList<Card> inPile = KnownInPile(known);
        IReadOnlyList<Card> buried = KnownBuried(known);

        // every card of eras 1 to now, with the guilds in play: the ones the seat has seen, plus random others
        List<Card> seen = CardsSeen(players, seat, known, inPile, buried);
        var pool = new List<Card>();
        for (int deckEra = 1; deckEra <= era; deckEra++)
        {
            pool.AddRange(Deck(deckEra, playerCount, seen, random));
        }

        // take out the cards the seat knows where they are
        RemoveBuiltCards(pool, players);
        RemoveAll(pool, players[seat].Hand);
        RemoveAll(pool, buried);
        List<Card> pileKnown = TakeOutOfPool(pool, inPile);

        // cards seen in a hand that this guess says were buried or discarded since
        var goneFaceDown = new List<Card>();
        List<Card>?[] hands = HandsOfSeenCards(players, seat, known, pool, goneFaceDown, random);
        FillHandsWithUnseenCards(players, seat, hands, pool, era, random);

        copy.DiscardPile = DiscardPileGuess(copy, seat, pileKnown, goneFaceDown, pool, random);
        ShuffleDecks(copy, era, playerCount, random);
        return copy;
    }

    /// <summary>The discard-pile cards the seat knows about (none without knowledge).</summary>
    private static IReadOnlyList<Card> KnownInPile(SeatKnowledge? known)
    {
        if (known == null)
        {
            return Array.Empty<Card>();
        }
        return known.InPile;
    }

    /// <summary>The cards the seat buried under its wonder (none without knowledge).</summary>
    private static IReadOnlyList<Card> KnownBuried(SeatKnowledge? known)
    {
        if (known == null)
        {
            return Array.Empty<Card>();
        }
        return known.Buried;
    }

    /// <summary>
    /// The cards the seat saw in the hand <paramref name="handOwnerSeat"/> holds now (none without knowledge).
    /// </summary>
    private static IReadOnlyList<Card> SeenInHand(SeatKnowledge? known, int handOwnerSeat)
    {
        if (known == null)
        {
            return Array.Empty<Card>();
        }
        return known.InHand[handOwnerSeat];
    }

    /// <summary>
    /// Every card the seat has seen: the built cards of every city, its own hand, the cards seen in the hands, the
    /// known discard-pile cards and its buried cards.
    /// </summary>
    private static List<Card> CardsSeen(List<PlayerState> players, int seat, SeatKnowledge? known,
                                        IReadOnlyList<Card> inPile, IReadOnlyList<Card> buried)
    {
        var seen = new List<Card>();
        foreach (PlayerState player in players)
        {
            seen.AddRange(player.BuiltCards);
        }
        seen.AddRange(players[seat].Hand);
        for (int handOwnerSeat = 0; handOwnerSeat < players.Count; handOwnerSeat++)
        {
            seen.AddRange(SeenInHand(known, handOwnerSeat));
        }
        seen.AddRange(inPile);
        seen.AddRange(buried);
        return seen;
    }

    /// <summary>Takes every city's built cards out of the pool.</summary>
    private static void RemoveBuiltCards(List<Card> pool, List<PlayerState> players)
    {
        foreach (PlayerState player in players)
        {
            RemoveAll(pool, player.BuiltCards);
        }
    }

    /// <summary>Takes one copy of each of the cards out of the pool (a card not in it is skipped).</summary>
    private static void RemoveAll(List<Card> pool, IEnumerable<Card> cards)
    {
        foreach (Card card in cards)
        {
            pool.Remove(card);
        }
    }

    /// <summary>The cards that were still in the pool, in their order; each is taken out of it.</summary>
    private static List<Card> TakeOutOfPool(List<Card> pool, IReadOnlyList<Card> cards)
    {
        var taken = new List<Card>();
        foreach (Card card in cards)
        {
            if (pool.Remove(card))
            {
                taken.Add(card);
            }
        }
        return taken;
    }

    /// <summary>
    /// Each other seat's hand (null for the searching seat): the cards seen in it, shuffled and cut to the hand's
    /// size. The cards cut off were buried or discarded since; they go to <paramref name="goneFaceDown"/>.
    /// </summary>
    private static List<Card>?[] HandsOfSeenCards(List<PlayerState> players, int seat, SeatKnowledge? known,
                                                  List<Card> pool, List<Card> goneFaceDown, Random random)
    {
        var hands = new List<Card>?[players.Count];
        for (int handOwnerSeat = 0; handOwnerSeat < players.Count; handOwnerSeat++)
        {
            if (handOwnerSeat == seat)
            {
                continue;
            }
            int size = players[handOwnerSeat].Hand.Count;
            List<Card> candidates = TakeOutOfPool(pool, SeenInHand(known, handOwnerSeat));
            Shuffle(candidates, random);
            hands[handOwnerSeat] = FirstCards(candidates, size);
            AddCardsAfter(goneFaceDown, candidates, size);
        }
        return hands;
    }

    /// <summary>A new list of the first <paramref name="count"/> cards (all of them if there are fewer).</summary>
    private static List<Card> FirstCards(List<Card> cards, int count)
    {
        var first = new List<Card>();
        for (int cardIndex = 0; cardIndex < count && cardIndex < cards.Count; cardIndex++)
        {
            first.Add(cards[cardIndex]);
        }
        return first;
    }

    /// <summary>Adds the cards after the first <paramref name="count"/> to <paramref name="target"/>.</summary>
    private static void AddCardsAfter(List<Card> target, List<Card> cards, int count)
    {
        for (int cardIndex = count; cardIndex < cards.Count; cardIndex++)
        {
            target.Add(cards[cardIndex]);
        }
    }

    /// <summary>
    /// Fills each other hand up to its real size with unseen cards of this era, drawn in a random order from the
    /// pool, and gives the hands to the players.
    /// </summary>
    private static void FillHandsWithUnseenCards(List<PlayerState> players, int seat, List<Card>?[] hands,
                                                 List<Card> pool, int era, Random random)
    {
        List<Card> unseenThisEra = CardsOfEra(pool, era);
        Shuffle(unseenThisEra, random);
        int next = 0;
        for (int handOwnerSeat = 0; handOwnerSeat < players.Count; handOwnerSeat++)
        {
            List<Card>? hand = hands[handOwnerSeat];
            if (hand == null)
            {
                continue;
            }
            while (hand.Count < players[handOwnerSeat].Hand.Count)
            {
                if (next == unseenThisEra.Count)
                {
                    throw new InvalidOperationException(
                        $"Not enough unseen era {era} cards to fill seat {handOwnerSeat}'s hand "
                        + $"(seat {seat}'s guess).");
                }
                Card card = unseenThisEra[next];
                next++;
                pool.Remove(card);
                hand.Add(card);
            }
            players[handOwnerSeat].Hand = hand;
        }
    }

    /// <summary>A new list of the pool's cards of this era, in pool order.</summary>
    private static List<Card> CardsOfEra(List<Card> pool, int era)
    {
        var cardsOfEra = new List<Card>();
        foreach (Card card in pool)
        {
            if (card.Era == era)
            {
                cardsOfEra.Add(card);
            }
        }
        return cardsOfEra;
    }

    /// <summary>
    /// The discard pile: what the seat knows is there, then random cards out of those gone face down and the rest of
    /// the pool, up to the pile's real size.
    /// </summary>
    private static List<Card> DiscardPileGuess(Game copy, int seat, List<Card> pileKnown, List<Card> goneFaceDown,
                                               List<Card> pool, Random random)
    {
        var unplacedCards = new List<Card>(goneFaceDown);
        unplacedCards.AddRange(pool);
        Shuffle(unplacedCards, random);
        int unknownInPile = copy.DiscardPile.Count - pileKnown.Count;
        if (unknownInPile > unplacedCards.Count)
        {
            throw new InvalidOperationException(
                $"Not enough unseen cards to fill the discard pile (seat {seat}'s guess).");
        }
        for (int unplacedIndex = 0; unplacedIndex < unknownInPile; unplacedIndex++)
        {
            pileKnown.Add(unplacedCards[unplacedIndex]);
        }
        return pileKnown;
    }

    /// <summary>The decks: those already dealt are used up; the later ones are new shuffles.</summary>
    private static void ShuffleDecks(Game copy, int era, int playerCount, Random random)
    {
        for (int deckEra = 1; deckEra <= Game.LastEra; deckEra++)
        {
            int deckIndex = deckEra - 1;
            if (deckEra <= era)
            {
                copy.Decks[deckIndex] = new List<Card>();
            }
            else
            {
                List<Card> deck = Deck(deckEra, playerCount, Array.Empty<Card>(), random);
                Shuffle(deck, random);
                copy.Decks[deckIndex] = deck;
            }
        }
    }

    /// <summary>
    /// The era's cards for this many players. In era 3, players + 2 guilds: every guild in <paramref name="seen"/>,
    /// then random other guilds.
    /// </summary>
    private static List<Card> Deck(int era, int players, IReadOnlyList<Card> seen, Random random)
    {
        var deck = new List<Card>();
        foreach (Card card in CardsDataset.Cards.Values)
        {
            if (card.Era != era)
            {
                continue;
            }
            for (int copyIndex = 0; copyIndex < card.CopiesFor(players); copyIndex++)
            {
                deck.Add(card);
            }
        }
        if (era == Game.LastEra)
        {
            deck.AddRange(GuildsInPlay(players, seen, random));
        }
        return deck;
    }

    /// <summary>
    /// Players + 2 guilds: every guild in <paramref name="seen"/> (once each), then random other guilds.
    /// </summary>
    private static List<Card> GuildsInPlay(int players, IReadOnlyList<Card> seen, Random random)
    {
        var guilds = new List<Card>();
        foreach (Card card in seen)
        {
            if (card.Type == CardType.Guild && !guilds.Contains(card))
            {
                guilds.Add(card);
            }
        }

        var others = new List<Card>();
        foreach (Card guild in CardsDataset.GuildCards.Values)
        {
            if (!guilds.Contains(guild))
            {
                others.Add(guild);
            }
        }
        Shuffle(others, random);

        int guildsInPlay = players + 2;
        int missingGuilds = guildsInPlay - guilds.Count;
        for (int otherIndex = 0; otherIndex < missingGuilds && otherIndex < others.Count; otherIndex++)
        {
            guilds.Add(others[otherIndex]);
        }
        return guilds;
    }

    /// <summary>Fisher-Yates shuffle, drawing from <paramref name="random"/>.</summary>
    private static void Shuffle(List<Card> cards, Random random)
    {
        for (int position = cards.Count - 1; position > 0; position--)
        {
            int randomPosition = random.Next(position + 1);
            Swap(cards, position, randomPosition);
        }
    }

    private static void Swap(List<Card> cards, int firstIndex, int secondIndex)
    {
        Card first = cards[firstIndex];
        cards[firstIndex] = cards[secondIndex];
        cards[secondIndex] = first;
    }
}
