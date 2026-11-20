using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Games.Enums;
using _7Wonders.Engine.Wonders.Enums;

namespace _7Wonders.Agents.Matches.Records;

/// <summary>
/// What a seat's agent was shown when it was asked for a decision: its city, its hand and what it could build.
/// </summary>
public sealed class SeatShown : MatchEntry
{
    /// <summary>What seat <paramref name="seat"/> was shown in <paramref name="phase"/>.</summary>
    public SeatShown(Phase phase,
                     int seat,
                     int coins,
                     int shields,
                     WonderId wonder,
                     int builtStageCount,
                     IReadOnlyList<Card> hand,
                     IReadOnlyList<Card> discardPile,
                     IReadOnlyList<Card> buildableCards,
                     bool canBuildWonder)
    {
        Phase = phase;
        Seat = seat;
        Coins = coins;
        Shields = shields;
        Wonder = wonder;
        BuiltStageCount = builtStageCount;
        Hand = hand;
        DiscardPile = discardPile;
        BuildableCards = buildableCards;
        CanBuildWonder = canBuildWonder;
    }

    /// <summary>
    /// The decision asked for: a move from the hand, or an extra move an effect gives (the last card of the hand, a
    /// free build from the discard pile).
    /// </summary>
    public Phase Phase { get; }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The seat's coins.</summary>
    public int Coins { get; }

    /// <summary>The seat's shields.</summary>
    public int Shields { get; }

    /// <summary>The seat's wonder board and side.</summary>
    public WonderId Wonder { get; }

    /// <summary>The wonder stages the seat has built.</summary>
    public int BuiltStageCount { get; }

    /// <summary>The seat's hand.</summary>
    public IReadOnlyList<Card> Hand { get; }

    /// <summary>The discard pile for a build from it; empty for any other decision.</summary>
    public IReadOnlyList<Card> DiscardPile { get; }

    /// <summary>The cards the seat could build, as its agent's view gave them.</summary>
    public IReadOnlyList<Card> BuildableCards { get; }

    /// <summary>Whether the seat could build its next wonder stage.</summary>
    public bool CanBuildWonder { get; }
}
