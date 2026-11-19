namespace _7Wonders.Engine.Moves.Enums;

/// <summary>What a player does with a card on their turn.</summary>
public enum MoveAction
{
    /// <summary>Build the card as a structure, paying its cost (or chaining).</summary>
    Build,
    /// <summary>Build the card for free with Olympia A's once-per-age power.</summary>
    BuildWithOlympiaFreeBuild,
    /// <summary>Use the card to build the next wonder stage.</summary>
    BuildWonderStage,
    /// <summary>Discard the card for 3 coins.</summary>
    Discard,
    /// <summary>No move (only in extra turns when nothing can be played).</summary>
    Skip,
}
