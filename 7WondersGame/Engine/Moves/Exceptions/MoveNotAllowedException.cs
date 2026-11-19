namespace _7WondersGame.Engine.Moves.Exceptions;

/// <summary>
/// A chosen move that the rules don't allow. The game stops here instead of carrying on in a state that shouldn't
/// exist; the message has what is needed to replay it.
/// </summary>
public sealed class MoveNotAllowedException : InvalidOperationException
{
    /// <summary>
    /// Seat <paramref name="seat"/> could not make its move; <paramref name="failure"/> says which and when.
    /// </summary>
    public MoveNotAllowedException(int seat, string failure)
        : base($"Seat {seat} {failure}")
    {
        Seat = seat;
        Failure = failure;
    }

    /// <summary>The seat whose move the rules don't allow.</summary>
    public int Seat { get; }

    /// <summary>
    /// What the seat could not do, and when, e.g. "could not Build Loom on turn 3 (era 1, game seed 42).".
    /// </summary>
    public string Failure { get; }
}
