using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models
{
    /// <summary>
    /// What one seat knows about the cards it can't see now (fix plan step 6, M1). Everything else hidden (the
    /// cards it never saw, the shuffle of the later decks, the guilds in play) it doesn't know.
    /// </summary>
    /// <param name="InHand">
    /// By seat: the cards this seat saw in the hand that seat holds now, minus the ones built from it since (a build is
    /// public). Cards buried under a wonder or discarded are face down, so some of these may be gone: the list holds
    /// every card still in the hand, and maybe more. Empty for a hand the seat hasn't seen this era; its own hand exactly.
    /// </param>
    /// <param name="InPile">Discard-pile cards the seat put there, or saw when it looked through the pile (Halikarnassos).</param>
    /// <param name="Buried">The cards the seat buried under its own wonder stages.</param>
    public sealed record SeatKnowledge(IReadOnlyList<IReadOnlyList<Card>> InHand, IReadOnlyList<Card> InPile, IReadOnlyList<Card> Buried);

    /// <summary>
    /// Keeps every seat's <see cref="SeatKnowledge"/> up to date. <see cref="Game"/> tells it what each seat saw;
    /// it only reads the game, so the game plays the same with or without it.
    /// </summary>
    internal sealed class KnowledgeTracker
    {
        private readonly int _n;
        // [observer, holder]: see SeatKnowledge.InHand
        private List<Card>[,] _inHand;
        private readonly List<Card>[] _inPile;
        private readonly List<Card>[] _buried;

        public KnowledgeTracker(int players)
        {
            _n = players;
            _inHand = NewHands();
            _inPile = Enumerable.Range(0, players).Select(_ => new List<Card>()).ToArray();
            _buried = Enumerable.Range(0, players).Select(_ => new List<Card>()).ToArray();
        }

        private List<Card>[,] NewHands()
        {
            var hands = new List<Card>[_n, _n];
            for (int o = 0; o < _n; o++)
                for (int h = 0; h < _n; h++)
                    hands[o, h] = new List<Card>();
            return hands;
        }

        public SeatKnowledge Of(int seat)
        {
            var hands = new List<IReadOnlyList<Card>>(_n);
            for (int h = 0; h < _n; h++)
                hands.Add(_inHand[seat, h].ToList());
            return new SeatKnowledge(hands, _inPile[seat].ToList(), _buried[seat].ToList());
        }

        /// <summary>New hands at an era start: each seat sees its own and nothing of the others'.</summary>
        public void Dealt(IReadOnlyList<PlayerState> players)
        {
            _inHand = NewHands();
            foreach (PlayerState p in players)
                _inHand[p.Id, p.Id].AddRange(p.HandCards);
        }

        /// <summary>A move made with a card (after it was placed).</summary>
        public void Moved(PlannedMove move)
        {
            int seat = move.Player.Id;
            Card card = move.Card;
            if (move.FromDiscardPile)
            {
                BuiltFromPile(card);
                return;
            }
            _inHand[seat, seat].Remove(card);
            switch (move.Action)
            {
                case ActionType.Build:
                case ActionType.BuildFreeOnce:
                    // everyone sees the card built
                    for (int o = 0; o < _n; o++)
                        if (o != seat)
                            _inHand[o, seat].Remove(card);
                    break;
                case ActionType.BuildWonder:
                    _buried[seat].Add(card);
                    break;
                case ActionType.Discard:
                    _inPile[seat].Add(card);
                    break;
            }
        }

        /// <summary>Halikarnassos: a card built from the discard pile, in sight of everyone.</summary>
        public void BuiltFromPile(Card card)
        {
            for (int o = 0; o < _n; o++)
                _inPile[o].Remove(card);
        }

        /// <summary>Halikarnassos: the seat looks through the discard pile.</summary>
        public void LooksAtPile(int seat, List<Card> pile)
        {
            _inPile[seat].Clear();
            _inPile[seat].AddRange(pile);
        }

        /// <summary>The era's last cards go to the discard pile; each seat knows its own.</summary>
        public void EraEndDiscards(IReadOnlyList<PlayerState> players)
        {
            foreach (PlayerState p in players)
                _inPile[p.Id].AddRange(p.HandCards);
            // the hands are empty until the next era's deal
            _inHand = NewHands();
        }

        /// <summary>
        /// Called before the hands pass (west if <paramref name="west"/>, else east): what each seat knows about a
        /// hand goes with it, and each seat sees the hand it receives.
        /// </summary>
        public void HandsPassing(IReadOnlyList<PlayerState> players, bool west)
        {
            int Receiver(int h) => west ? players[h].PlayerWest.Id : players[h].PlayerEast.Id;

            var next = NewHands();
            for (int o = 0; o < _n; o++)
            {
                for (int h = 0; h < _n; h++)
                {
                    // a seat knows the hand it is passing on exactly
                    IEnumerable<Card> known = h == o ? players[o].HandCards : _inHand[o, h];
                    next[o, Receiver(h)].AddRange(known);
                }
            }
            for (int h = 0; h < _n; h++)
            {
                int r = Receiver(h);
                next[r, r].Clear();
                next[r, r].AddRange(players[h].HandCards);
            }
            _inHand = next;
        }
    }
}
