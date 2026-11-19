using _7Wonders.Engine.Cards.Entities;

namespace _7Wonders.Agents.Matches.Records;

/// <summary>How the match starts: the game seed, who sits where on which board, and each era's deck.</summary>
public sealed class MatchSetUp : MatchEntry
{
    /// <summary>A match set up with these seats and decks.</summary>
    public MatchSetUp(ulong seed, IReadOnlyList<SeatSetUp> seats, IReadOnlyList<IReadOnlyList<Card>> decks)
    {
        Seed = seed;
        Seats = seats;
        Decks = decks;
    }

    /// <summary>The game seed, from which every random draw of the game comes.</summary>
    public ulong Seed { get; }

    /// <summary>Each seat's agent and board, in seat order.</summary>
    public IReadOnlyList<SeatSetUp> Seats { get; }

    /// <summary>The decks of eras I, II and III before the first deal.</summary>
    public IReadOnlyList<IReadOnlyList<Card>> Decks { get; }
}
