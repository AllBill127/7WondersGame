using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Moves.Enums;

namespace _7Wonders.Engine.Games.Events;

/// <summary>One seat's move has been carried out: what it built or discarded, what it paid and what changed.</summary>
public sealed class MoveResolved : GameEvent
{
    /// <summary>The event with these values.</summary>
    public MoveResolved(int seat,
                        MoveAction action,
                        Card? card,
                        int builtStageCount,
                        int coinsBefore,
                        int coinsAfter,
                        int westSeat,
                        int paidToWest,
                        int eastSeat,
                        int paidToEast,
                        int paidToBank,
                        IReadOnlyList<string> gained,
                        IReadOnlyList<string> usedUp,
                        IReadOnlyList<IEffectSource> effectSources)
    {
        Seat = seat;
        Action = action;
        Card = card;
        BuiltStageCount = builtStageCount;
        CoinsBefore = coinsBefore;
        CoinsAfter = coinsAfter;
        WestSeat = westSeat;
        PaidToWest = paidToWest;
        EastSeat = eastSeat;
        PaidToEast = paidToEast;
        PaidToBank = paidToBank;
        Gained = gained;
        UsedUp = usedUp;
        EffectSources = effectSources;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>What the seat did with the card.</summary>
    public MoveAction Action { get; }

    /// <summary>The card used; null if the seat skipped the move.</summary>
    public Card? Card { get; }

    /// <summary>The wonder stages the seat has built after the move.</summary>
    public int BuiltStageCount { get; }

    /// <summary>The seat's coins before the turn's moves.</summary>
    public int CoinsBefore { get; }

    /// <summary>The seat's coins after them.</summary>
    public int CoinsAfter { get; }

    /// <summary>The west neighbour's seat.</summary>
    public int WestSeat { get; }

    /// <summary>The coins paid to the west neighbour for resources.</summary>
    public int PaidToWest { get; }

    /// <summary>The east neighbour's seat.</summary>
    public int EastSeat { get; }

    /// <summary>The coins paid to the east neighbour for resources.</summary>
    public int PaidToEast { get; }

    /// <summary>The coins paid to the bank (the card's coin cost).</summary>
    public int PaidToBank { get; }

    /// <summary>The abilities and one-use charges the move gave, by name.</summary>
    public IReadOnlyList<string> Gained { get; }

    /// <summary>The one-use charges (and abilities) the move used up, by name.</summary>
    public IReadOnlyList<string> UsedUp { get; }

    /// <summary>
    /// The cards and wonder stages whose effect the move used: those giving the free build each era for
    /// <see cref="MoveAction.BuildWithFreeBuild"/>; empty for any other move.
    /// </summary>
    public IReadOnlyList<IEffectSource> EffectSources { get; }
}
