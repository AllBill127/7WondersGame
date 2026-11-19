using _7WondersGame.Engine.Cards.Entities;

namespace _7WondersGame.Engine.Cards.Services;

/// <summary>Card names for the trace.</summary>
public static class CardNames
{
    /// <summary>The cards' names, separated by commas: "Loom, Baths, Altar".</summary>
    public static string Join(IEnumerable<Card> cards)
    {
        return string.Join(", ", cards.Select(card => card.Name));
    }
}
