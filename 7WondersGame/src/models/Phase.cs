namespace _7WondersGame.src.models
{
    /// <summary>What a game is waiting for (E1). <see cref="Game.PendingPlayers"/> says who must decide.</summary>
    public enum Phase
    {
        /// <summary>Every player chooses a move; the moves happen together once all of them are in.</summary>
        ChooseMoves,

        /// <summary>Babylon B: after an era's 6th turn the player also plays the last card of the hand.</summary>
        SeventhCard,

        /// <summary>Halikarnassos: after the turn a stage was built, the player may build a card from the discard pile for free.</summary>
        DiscardBuild,

        GameOver,
    }
}
