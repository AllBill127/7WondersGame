using _7Wonders.Engine.Cards.Entities;

namespace _7Wonders.Engine.Cards.Services;

/// <summary>Card names for the trace.</summary>
public static class CardNames
{
    /// <summary>The cards' names, separated by commas: "Loom, Baths, Altar".</summary>
    public static string Join(IEnumerable<Card> cards)
    {
        // Select turns each card into its name (`card => card.Name` is the rule applied to each one).
        return string.Join(", ", cards.Select(card => card.Name));
    }
}
