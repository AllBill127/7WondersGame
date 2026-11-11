using System.Collections;

namespace _7WondersGame.src.models.Players
{
    /// <summary>
    /// A player's counts of each <see cref="Resource"/>: production, science symbols, coins and shields. Read and
    /// changed like a dictionary (<c>p.Resources[Resource.Coins] += 3</c>), but kept in an array, because a search
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

        public int this[Resource resource]
        {
            get => _counts[(int)resource];
            set => _counts[(int)resource] = value;
        }

        public int Count => Size;

        public ResourceCounts Clone()
        {
            var copy = new ResourceCounts();
            Array.Copy(_counts, copy._counts, Size);
            return copy;
        }

        public IEnumerator<KeyValuePair<Resource, int>> GetEnumerator()
        {
            foreach (Resource r in Listed)
                yield return new KeyValuePair<Resource, int>(r, _counts[(int)r]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
