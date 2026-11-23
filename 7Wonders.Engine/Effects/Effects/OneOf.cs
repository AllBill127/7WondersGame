using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Payment.Services;

namespace _7Wonders.Engine.Effects.Effects;

/// <summary>
/// One unit of any of <see cref="Choices"/> each turn, chosen when paying (see <see cref="PaymentPlanner"/>).
/// Neighbours can buy it only if <see cref="Sellable"/> (the brown cards, not Caravansery or Forum).
/// </summary>
public sealed class OneOf : IEffect
{
    /// <summary>An effect that produces one of <paramref name="choices"/> each turn.</summary>
    public OneOf(bool sellable, params Resource[] choices)
    {
        Sellable = sellable;
        Choices = choices;
    }

    /// <summary>Whether neighbours can buy the resource from it.</summary>
    public bool Sellable { get; }

    /// <summary>The resources it can produce, one per turn.</summary>
    public Resource[] Choices { get; }

    /// <summary>The choices as one number: bit i is 1 when the resource with value i is a choice.</summary>
    /// <remarks>
    /// <c>1 &lt;&lt; (int)choice</c> is 1 moved left by the resource's value, a number with only that resource's bit
    /// set (Ore = 1 gives 0b10, Clay = 2 gives 0b100); <c>|=</c> sets that bit in the mask. Clay or Ore gives
    /// 0b110 (6). The payment search tests and combines such masks with one <c>&amp;</c> or <c>|</c> instead of
    /// looping over lists.
    /// </remarks>
    public int Mask
    {
        get
        {
            int choiceMask = 0;
            foreach (Resource choice in Choices)
            {
                choiceMask |= 1 << (int)choice;
            }
            return choiceMask;
        }
    }
}
