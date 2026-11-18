namespace _7WondersGame.Engine.Payment.Dtos;

/// <summary>
/// How a player pays for a card or wonder stage: the coins given to each neighbour for the resources bought
/// from them. A card's own coin cost (for example Tree Farm's 1 coin) is paid separately.
/// Two plans are equal when they pay each neighbour the same.
/// </summary>
public readonly struct PaymentPlan : IEquatable<PaymentPlan>
{
    /// <summary>A plan paying <paramref name="toWest"/> coins west and <paramref name="toEast"/> east.</summary>
    public PaymentPlan(int toWest, int toEast)
    {
        ToWest = toWest;
        ToEast = toEast;
    }

    /// <summary>Coins paid to the west neighbour.</summary>
    public int ToWest { get; }

    /// <summary>Coins paid to the east neighbour.</summary>
    public int ToEast { get; }

    /// <summary>Coins paid to both neighbours together.</summary>
    public int Total
    {
        get
        {
            return ToWest + ToEast;
        }
    }

    /// <summary>Everything comes from the player's own city.</summary>
    public static PaymentPlan Free
    {
        get
        {
            return default;
        }
    }

    /// <inheritdoc/>
    public bool Equals(PaymentPlan other)
    {
        return ToWest == other.ToWest && ToEast == other.ToEast;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is PaymentPlan other)
        {
            return Equals(other);
        }
        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(ToWest, ToEast);
    }

    /// <summary>Whether both plans pay each neighbour the same.</summary>
    public static bool operator ==(PaymentPlan left, PaymentPlan right)
    {
        return left.Equals(right);
    }

    /// <summary>Whether the plans pay a neighbour differently.</summary>
    public static bool operator !=(PaymentPlan left, PaymentPlan right)
    {
        return !left.Equals(right);
    }

    /// <summary>For test failure messages, e.g. <c>PaymentPlan { ToWest = 2, ToEast = 1, Total = 3 }</c>.</summary>
    public override string ToString()
    {
        return $"PaymentPlan {{ ToWest = {ToWest}, ToEast = {ToEast}, Total = {Total} }}";
    }
}
