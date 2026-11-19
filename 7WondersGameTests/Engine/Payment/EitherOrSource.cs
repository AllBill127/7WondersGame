using _7WondersGame.Engine.Cards.Enums;

namespace _7WondersGameTests.Engine.Payment;

/// <summary>An either/or source for <see cref="BruteForcePayment"/>: its owner and the resources it may give.</summary>
internal sealed class EitherOrSource
{
    /// <summary>A source of <paramref name="owner"/> that gives one of <paramref name="options"/>.</summary>
    public EitherOrSource(int owner, Resource[] options)
    {
        Owner = owner;
        Options = options;
    }

    /// <summary>The owner: the buyer, west or east (an index into the brute force's supply).</summary>
    public int Owner { get; }

    /// <summary>The resources the source may give, one at a time.</summary>
    public Resource[] Options { get; }
}
