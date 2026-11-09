namespace _7WondersGame.src.models
{
    /// <summary>What a player does with a card on their turn (E4).</summary>
    public enum ActionType
    {
        /// <summary>Build the card as a structure, paying its cost (or chaining).</summary>
        Build,
        /// <summary>Build the card for free with Olympia A's once-per-age power.</summary>
        BuildFreeOnce,
        /// <summary>Use the card to build the next wonder stage.</summary>
        BuildWonder,
        /// <summary>Discard the card for 3 coins.</summary>
        Discard,
        /// <summary>No move (only in extra turns when nothing can be played).</summary>
        Skip,
    }

    /// <summary>
    /// A move: an action and the card it uses (null only for <see cref="ActionType.Skip"/>).
    /// Two moves are equal when their action and card (compared by Id) are equal.
    /// </summary>
    public sealed record Command(ActionType Action, Card? Card)
    {
        public override string ToString() => Card == null ? Action.ToString() : $"{Action} {Card.Name}";
    }
}
