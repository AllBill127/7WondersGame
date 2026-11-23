using _7Wonders.Engine.Cards.Enums;

namespace _7Wonders.Engine.Payment.Dtos;

/// <summary>
/// How many units of each good (<see cref="Resource.Wood"/> to <see cref="Resource.Papyrus"/>) a payment buys from
/// one neighbour. The counts are packed into one number, 4 bits a good (no cost asks for more than 15 of a good),
/// so a <see cref="PaymentPlan"/> carries them without allocating: plans are made thousands of times a second in a
/// search's rollouts.
/// </summary>
/// <remarks>
/// Good i (its <see cref="Resource"/> value) has bits 4i to 4i + 3 of the number. "2 Wood, 1 Loom" is
/// 2 &lt;&lt; 0 plus 1 &lt;&lt; 16 = 0x10002. A <c>readonly struct</c> is a value copied on assignment, so it
/// allocates nothing (see <see cref="PaymentPlan"/>).
/// </remarks>
public readonly struct BoughtGoods
{
    private const int GoodCount = (int)Resource.Papyrus + 1;
    private const int BitsPerGood = 4;
    private const int CountMask = 0b1111;

    private readonly int _packedCounts;

    private BoughtGoods(int packedCounts)
    {
        _packedCounts = packedCounts;
    }

    /// <summary>Nothing bought.</summary>
    public static BoughtGoods None
    {
        get
        {
            // default is the struct with every bit zero: nothing bought.
            return default;
        }
    }

    /// <summary>Whether nothing is bought.</summary>
    public bool IsNone
    {
        get
        {
            return _packedCounts == 0;
        }
    }

    /// <summary>The units of <paramref name="good"/> bought.</summary>
    public int CountOf(Resource good)
    {
        // `>>` moves the good's 4 bits down to the bottom; `& CountMask` (0b1111) keeps only those 4 bits.
        return (_packedCounts >> ((int)good * BitsPerGood)) & CountMask;
    }

    /// <summary>These goods and <paramref name="count"/> more units of <paramref name="good"/>.</summary>
    public BoughtGoods With(Resource good, int count)
    {
        // `<<` moves the count up to the good's 4 bits; adding it adds to that good's count (it never passes 15, so
        // it never spills into the next good's bits).
        return new BoughtGoods(_packedCounts + (count << ((int)good * BitsPerGood)));
    }

    /// <summary>The goods in words, in <see cref="Resource"/> order: "2 Ore, 1 Loom"; "nothing" if none.</summary>
    public override string ToString()
    {
        if (IsNone)
        {
            return "nothing";
        }
        List<string> parts = new();
        for (int goodIndex = 0; goodIndex < GoodCount; goodIndex++)
        {
            Resource good = (Resource)goodIndex;
            int count = CountOf(good);
            if (count > 0)
            {
                parts.Add($"{count} {good}");
            }
        }
        return string.Join(", ", parts);
    }
}
