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

    /// <summary>Bit i stands for the resource with value i.</summary>
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
