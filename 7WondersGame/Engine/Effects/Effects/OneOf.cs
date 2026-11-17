using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Payment.Services;

namespace _7WondersGame.Engine.Effects.Effects;

/// <summary>
/// One unit of any of <paramref name="Choices"/> each turn, chosen when paying (see <see cref="PaymentPlanner"/>).
/// Neighbours can buy it only if <paramref name="Sellable"/> (the brown cards, not Caravansery or Forum).
/// </summary>
public sealed record OneOf(bool Sellable, params Resource[] Choices) : IEffect
{
    /// <summary>Bit i stands for the resource with value i.</summary>
    public int Mask => Choices.Aggregate(0, (mask, r) => mask | 1 << (int)r);
}
