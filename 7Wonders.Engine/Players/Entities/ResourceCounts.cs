using System.Collections;
using _7Wonders.Engine.Cards.Enums;

namespace _7Wonders.Engine.Players.Entities;

/// <summary>
/// A player's counts of each <see cref="Resource"/>: production, science symbols, coins and shields. Read and
/// changed like a dictionary (<c>player.Resources[Resource.Coins] += 3</c>), but kept in an array, because a search
/// copies players thousands of times a second and reads these counts all the time (a dictionary made each copy
/// and lookup several times dearer). Every resource is always there (0 if none).
/// </summary>
public sealed class ResourceCounts : IReadOnlyCollection<KeyValuePair<Resource, int>>
{
    private const int Size = (int)Resource.Shields + 1;

    /// <summary>The order the resources are listed in (the order the old dictionary listed them).</summary>
    private static readonly Resource[] Listed =
    {
        Resource.Wood, Resource.Stone, Resource.Ore, Resource.Clay, Resource.Glass, Resource.Loom, Resource.Papyrus,
        Resource.Gear, Resource.Compass, Resource.Tablet, Resource.Coins, Resource.Shields,
    };

    private readonly int[] _counts = new int[Size];

    /// <summary>The count of <paramref name="resource"/>.</summary>
    /// <remarks>
    /// An indexer: it lets the object be read and written with square brackets like an array. The resource's enum
    /// value is its position in the array (<c>(int)Resource.Coins</c>).
    /// </remarks>
    public int this[Resource resource]
    {
        get
        {
            return _counts[(int)resource];
        }
        set
        {
            _counts[(int)resource] = value;
        }
    }

    /// <summary>How many resources are counted (all of them, always).</summary>
    public int Count
    {
        get
        {
            return Size;
        }
    }

    /// <summary>Makes these counts equal to <paramref name="source"/>'s.</summary>
    public void CopyFrom(ResourceCounts source)
    {
        // Copies the numbers into this object's own array, so a reused copy (PlayerState.CopyFrom) allocates nothing.
        Array.Copy(source._counts, _counts, Size);
    }

    /// <summary>A copy of these counts.</summary>
    public ResourceCounts Clone()
    {
        ResourceCounts copy = new();
        Array.Copy(_counts, copy._counts, Size);
        return copy;
    }

    /// <summary>Each resource with its count, in the order of <see cref="Listed"/>.</summary>
    public IEnumerator<KeyValuePair<Resource, int>> GetEnumerator()
    {
        // An iterator: each `yield return` hands one pair to the foreach loop reading it and pauses here until the
        // loop asks for the next one. This is what lets `foreach (... in player.Resources)` work.
        foreach (Resource resource in Listed)
        {
            yield return new KeyValuePair<Resource, int>(resource, _counts[(int)resource]);
        }
    }

    // The non-generic version older code and interfaces ask for (the interface's name before the method means it is
    // only reached through IEnumerable); it hands back the generic one.
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
