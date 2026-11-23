using _7Wonders.Engine.Cards.Entities;

namespace _7Wonders.Engine.Games.Knowledge;

/// <summary>
/// What one seat knows about the cards it can't see now. Everything else hidden (the cards it never
/// saw, the shuffle of the later decks, the guilds in play) it doesn't know.
/// </summary>
public sealed class SeatKnowledge
{
    /// <summary>A seat's knowledge of the hands, the discard pile and its own buried cards.</summary>
    public SeatKnowledge(IReadOnlyList<IReadOnlyList<Card>> inHand,
                         IReadOnlyList<Card> inPile,
                         IReadOnlyList<Card> buried)
    {
        InHand = inHand;
        InPile = inPile;
        Buried = buried;
    }

    /// <summary>
    /// By seat: the cards this seat saw in the hand that seat holds now, minus the ones built from it since (a build
    /// is public). Cards buried under a wonder or discarded are face down, so some of these may be gone: the list
    /// holds every card still in the hand, and maybe more. Empty for a hand the seat hasn't seen this era; its own
    /// hand exactly.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<Card>> InHand { get; }

    /// <summary>
    /// Discard-pile cards the seat put there, or saw when it looked through the pile (Halikarnassos).
    /// </summary>
    public IReadOnlyList<Card> InPile { get; }

    /// <summary>The cards the seat buried under its own wonder stages.</summary>
    public IReadOnlyList<Card> Buried { get; }
}
