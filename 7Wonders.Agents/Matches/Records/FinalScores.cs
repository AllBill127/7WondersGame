namespace _7Wonders.Agents.Matches.Records;

/// <summary>The game is scored: every seat's victory points and where they came from.</summary>
public sealed class FinalScores : MatchEntry
{
    /// <summary>The scores of these seats.</summary>
    public FinalScores(IReadOnlyList<SeatScore> seats)
    {
        Seats = seats;
    }

    /// <summary>Each seat's score, in seat order.</summary>
    public IReadOnlyList<SeatScore> Seats { get; }
}
