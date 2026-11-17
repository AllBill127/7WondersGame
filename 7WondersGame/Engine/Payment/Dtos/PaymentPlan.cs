namespace _7WondersGame.Engine.Payment.Dtos;

/// <summary>
/// How a player pays for a card or wonder stage: the coins given to each neighbour for the resources bought
/// from them. A card's own coin cost (for example Tree Farm's 1 coin) is paid separately.
/// </summary>
public readonly record struct PaymentPlan(int ToWest, int ToEast)
{
    public int Total => ToWest + ToEast;

    /// <summary>Everything comes from the player's own city.</summary>
    public static PaymentPlan Free => default;
}
