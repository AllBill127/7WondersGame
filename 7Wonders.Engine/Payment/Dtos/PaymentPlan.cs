namespace _7Wonders.Engine.Payment.Dtos;

/// <summary>
/// How a player pays for a card or wonder stage: the coins given to each neighbour for the resources bought
/// from them, and which goods those are. A card's own coin cost (for example Tree Farm's 1 coin) is paid separately.
/// Two plans are equal when they pay each neighbour the same; the goods aren't compared, since they are only
/// reported (in the game record) and never change what anyone pays.
/// </summary>
/// <remarks>
/// A <c>readonly struct</c> is a value, not an object: it lives inside the variable or field that holds it, is copied
/// when assigned, and creating one allocates nothing on the heap. <c>readonly</c> means no field changes after the
/// constructor. Plans are made for every card a heuristic looks at in every rollout, so a class here would allocate
/// millions of objects per search.
/// </remarks>
public readonly struct PaymentPlan : IEquatable<PaymentPlan>
{
    /// <summary>
    /// A plan paying <paramref name="toWest"/> coins west and <paramref name="toEast"/> east, without the goods
    /// (for tests that only check the price).
    /// </summary>
    public PaymentPlan(int toWest, int toEast)
        : this(toWest, toEast, BoughtGoods.None, BoughtGoods.None)
    {
    }

    /// <summary>
    /// A plan paying <paramref name="toWest"/> coins west for <paramref name="boughtFromWest"/> and
    /// <paramref name="toEast"/> east for <paramref name="boughtFromEast"/>.
    /// </summary>
    public PaymentPlan(int toWest, int toEast, BoughtGoods boughtFromWest, BoughtGoods boughtFromEast)
    {
        ToWest = toWest;
        ToEast = toEast;
        BoughtFromWest = boughtFromWest;
        BoughtFromEast = boughtFromEast;
    }

    /// <summary>Coins paid to the west neighbour.</summary>
    public int ToWest { get; }

    /// <summary>Coins paid to the east neighbour.</summary>
    public int ToEast { get; }

    /// <summary>The goods bought from the west neighbour.</summary>
    public BoughtGoods BoughtFromWest { get; }

    /// <summary>The goods bought from the east neighbour.</summary>
    public BoughtGoods BoughtFromEast { get; }

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
            // default is the struct with every field zero: no coins, nothing bought.
        return default;
        }
    }

    /// <inheritdoc/>
    public bool Equals(PaymentPlan other)
    {
        return ToWest == other.ToWest && ToEast == other.ToEast;
    }

    /// <inheritdoc/>
    public override bool Equals(object? otherObject)
    {
        // `is PaymentPlan other` tests the object's type and, if it matches, names it `other` as a PaymentPlan.
        if (otherObject is PaymentPlan other)
        {
            return Equals(other);
        }
        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Equal plans must give the same hash, so it mixes the same two fields Equals compares; HashCode.Combine
        // mixes several values into one int.
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
