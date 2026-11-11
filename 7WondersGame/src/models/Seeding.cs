namespace _7WondersGame.src.models
{
    /// <summary>
    /// All randomness comes from here, so the same seed always gives the same games (B1).
    /// Seeds are mixed with SplitMix64, which is fixed and the same in every process
    /// (unlike HashCode, which is randomised per process).
    /// </summary>
    public static class Seeding
    {
        private const ulong Golden = 0x9E3779B97F4A7C15UL;

        /// <summary>Stream of a game used for the shuffle, the wonder deal and discard pruning.</summary>
        public const ulong GameStream = 0;
        /// <summary>Seat s uses stream AgentStreamBase + s.</summary>
        public const ulong AgentStreamBase = 1;
        /// <summary>The n-th copy of a game (MCTS search) uses stream CopyStreamBase + n.</summary>
        public const ulong CopyStreamBase = 1UL << 32;

        /// <summary>The SplitMix64 output function.</summary>
        public static ulong Mix(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>The index-th output (counting from 0) of a SplitMix64 generator started at seed.</summary>
        public static ulong Nth(ulong seed, ulong index) => unchecked(Mix(seed + (index + 1) * Golden));

        /// <summary>Seed of game number gameIndex in a run.</summary>
        public static ulong GameSeed(ulong runSeed, long gameIndex) => Nth(runSeed, (ulong)gameIndex);

        /// <summary>Seed of the random stream a game gives to the player in this seat.</summary>
        public static ulong AgentSeed(ulong gameSeed, int seat) => Nth(gameSeed, AgentStreamBase + (ulong)seat);

        /// <summary>The only place a Random is created.</summary>
        public static Random CreateRandom(ulong seed) => new Random(unchecked((int)(seed >> 32)));
    }
}
