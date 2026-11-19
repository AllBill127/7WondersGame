using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Players.Entities;

namespace _7Wonders.Engine.Moves.Dtos;

/// <summary>
/// A player's move as it goes into <see cref="Game.ResolveMoves"/>, with the cards it is made from. A struct,
/// because every turn of every rollout makes one per player.
/// </summary>
internal readonly struct SubmittedMove
{
    /// <summary><paramref name="player"/>'s <paramref name="move"/>, made from <paramref name="source"/>.</summary>
    public SubmittedMove(PlayerState player, Move move, List<Card> source)
    {
        Player = player;
        Move = move;
        Source = source;
    }

    /// <summary>The player making the move.</summary>
    public PlayerState Player { get; }

    /// <summary>What the player does, and with which card.</summary>
    public Move Move { get; }

    /// <summary>Where the card comes from: the hand, or the discard pile (Halikarnassos).</summary>
    public List<Card> Source { get; }
}
