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
    private readonly int _playerCount;
    // [observer, holder]: see SeatKnowledge.InHand
    private List<Card>[,] _inHand;
    private readonly List<Card>[] _inPile;
    private readonly List<Card>[] _buried;

    /// <summary>A tracker for a game of this many players, before the deal: nobody knows anything yet.</summary>
    public KnowledgeTracker(int players)
    {
        _playerCount = players;
        _inHand = NewHands();
        _inPile = NewCardListPerSeat();
        _buried = NewCardListPerSeat();
    }

    private List<Card>[,] NewHands()
    {
        var hands = new List<Card>[_playerCount, _playerCount];
        for (int observerSeat = 0; observerSeat < _playerCount; observerSeat++)
        {
            for (int holderSeat = 0; holderSeat < _playerCount; holderSeat++)
            {
                hands[observerSeat, holderSeat] = new List<Card>();
            }
        }
        return hands;
    }

    private List<Card>[] NewCardListPerSeat()
    {
        var lists = new List<Card>[_playerCount];
        for (int seat = 0; seat < _playerCount; seat++)
        {
            lists[seat] = new List<Card>();
        }
        return lists;
    }

    /// <summary>A copy of what the seat knows now.</summary>
    public SeatKnowledge Of(int seat)
    {
        var hands = new List<IReadOnlyList<Card>>(_playerCount);
        for (int holderSeat = 0; holderSeat < _playerCount; holderSeat++)
        {
            hands.Add(_inHand[seat, holderSeat].ToList());
        }
        return new SeatKnowledge(hands, _inPile[seat].ToList(), _buried[seat].ToList());
    }

    /// <summary>New hands at an era start: each seat sees its own and nothing of the others'.</summary>
    public void Dealt(IReadOnlyList<PlayerState> players)
    {
        _inHand = NewHands();
        foreach (PlayerState player in players)
        {
            _inHand[player.Id, player.Id].AddRange(player.HandCards);
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
                for (int observerSeat = 0; observerSeat < _playerCount; observerSeat++)
                {
                    if (observerSeat != seat)
                    {
                        _inHand[observerSeat, seat].Remove(card);
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
        for (int observerSeat = 0; observerSeat < _playerCount; observerSeat++)
        {
            _inPile[observerSeat].Remove(card);
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
        foreach (PlayerState player in players)
        {
            _inPile[player.Id].AddRange(player.HandCards);
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
        for (int observerSeat = 0; observerSeat < _playerCount; observerSeat++)
        {
            for (int holderSeat = 0; holderSeat < _playerCount; holderSeat++)
            {
                IEnumerable<Card> known = _inHand[observerSeat, holderSeat];
                // a seat knows the hand it is passing on exactly
                if (holderSeat == observerSeat)
                {
                    known = players[observerSeat].HandCards;
                }
                int receiverSeat = ReceiverOf(players, holderSeat, west);
                next[observerSeat, receiverSeat].AddRange(known);
            }
        }
        for (int holderSeat = 0; holderSeat < _playerCount; holderSeat++)
        {
            int receiverSeat = ReceiverOf(players, holderSeat, west);
            next[receiverSeat, receiverSeat].Clear();
            next[receiverSeat, receiverSeat].AddRange(players[holderSeat].HandCards);
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
