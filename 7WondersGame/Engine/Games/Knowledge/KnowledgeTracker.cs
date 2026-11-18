using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Games.Knowledge;

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

    /// <summary>A tracker for a game of this many players, before the deal: nobody knows anything yet.</summary>
    public KnowledgeTracker(int players)
    {
        _n = players;
        _inHand = NewHands();
        _inPile = NewCardListPerSeat();
        _buried = NewCardListPerSeat();
    }

    private List<Card>[,] NewHands()
    {
        var hands = new List<Card>[_n, _n];
        for (int o = 0; o < _n; o++)
        {
            for (int h = 0; h < _n; h++)
            {
                hands[o, h] = new List<Card>();
            }
        }
        return hands;
    }

    private List<Card>[] NewCardListPerSeat()
    {
        var lists = new List<Card>[_n];
        for (int seat = 0; seat < _n; seat++)
        {
            lists[seat] = new List<Card>();
        }
        return lists;
    }

    /// <summary>A copy of what the seat knows now.</summary>
    public SeatKnowledge Of(int seat)
    {
        var hands = new List<IReadOnlyList<Card>>(_n);
        for (int h = 0; h < _n; h++)
        {
            hands.Add(_inHand[seat, h].ToList());
        }
        return new SeatKnowledge(hands, _inPile[seat].ToList(), _buried[seat].ToList());
    }

    /// <summary>New hands at an era start: each seat sees its own and nothing of the others'.</summary>
    public void Dealt(IReadOnlyList<PlayerState> players)
    {
        _inHand = NewHands();
        foreach (PlayerState p in players)
        {
            _inHand[p.Id, p.Id].AddRange(p.HandCards);
        }
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
                {
                    if (o != seat)
                    {
                        _inHand[o, seat].Remove(card);
                    }
                }
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
        {
            _inPile[o].Remove(card);
        }
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
        {
            _inPile[p.Id].AddRange(p.HandCards);
        }
        // the hands are empty until the next era's deal
        _inHand = NewHands();
    }

    /// <summary>
    /// Called before the hands pass (west if <paramref name="west"/>, else east): what each seat knows about a
    /// hand goes with it, and each seat sees the hand it receives.
    /// </summary>
    public void HandsPassing(IReadOnlyList<PlayerState> players, bool west)
    {
        var next = NewHands();
        for (int o = 0; o < _n; o++)
        {
            for (int h = 0; h < _n; h++)
            {
                IEnumerable<Card> known = _inHand[o, h];
                // a seat knows the hand it is passing on exactly
                if (h == o)
                {
                    known = players[o].HandCards;
                }
                int receiverSeat = ReceiverOf(players, h, west);
                next[o, receiverSeat].AddRange(known);
            }
        }
        for (int h = 0; h < _n; h++)
        {
            int r = ReceiverOf(players, h, west);
            next[r, r].Clear();
            next[r, r].AddRange(players[h].HandCards);
        }
        _inHand = next;
    }

    /// <summary>The seat that receives the hand held in <paramref name="holderSeat"/>.</summary>
    private static int ReceiverOf(IReadOnlyList<PlayerState> players, int holderSeat, bool west)
    {
        PlayerState holder = players[holderSeat];
        if (west)
        {
            return holder.PlayerWest.Id;
        }
        return holder.PlayerEast.Id;
    }
}
