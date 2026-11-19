using _7Wonders.Engine.Games.Entities;

namespace _7Wonders.Engine.Games.Enums;

/// <summary>What a game is waiting for. <see cref="Game.PendingPlayers"/> says who must decide.</summary>
public enum Phase
{
    /// <summary>Every player chooses a move; the moves happen together once all of them are in.</summary>
    ChooseMoves,

    /// <summary>Babylon B: after an era's 6th turn the player also plays the last card of the hand.</summary>
    SeventhCard,

    /// <summary>
    /// Halikarnassos: after the turn a stage was built, the player may build a card from the discard pile for free.
    /// </summary>
    DiscardPileBuild,

    /// <summary>The third era is over; nobody decides anything any more.</summary>
    GameOver,
}
