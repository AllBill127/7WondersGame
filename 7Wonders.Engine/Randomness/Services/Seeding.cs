namespace _7Wonders.Engine.Randomness.Services;

/// <summary>
/// All randomness comes from here, so the same seed always gives the same games.
/// Seeds are mixed with SplitMix64, which is fixed and the same in every process
/// (unlike HashCode, which is randomised per process).
/// </summary>
public static class Seeding
{
    // SplitMix64's step: 2^64 divided by the golden ratio, an odd number whose bits look random. Adding it again and
    // again visits every 64-bit value once before repeating, so the n-th position is simply seed + n * Golden.
    private const ulong Golden = 0x9E3779B97F4A7C15UL;

    /// <summary>Stream of a game used for the shuffle, the wonder deal and discard pruning.</summary>
    public const ulong GameStream = 0;
    /// <summary>Seat s uses stream AgentStreamBase + s.</summary>
    public const ulong AgentStreamBase = 1;
    // 1UL << 32 is 1 moved 32 binary places to the left, that is 2^32 (4,294,967,296): far above any seat number,
    // so a copy's stream never equals a seat's stream.
    /// <summary>The n-th copy of a game (MCTS search) uses stream CopyStreamBase + n.</summary>
    public const ulong CopyStreamBase = 1UL << 32;

    /// <summary>The SplitMix64 output function.</summary>
    /// <remarks>
    /// Turns a number into one that looks random: numbers that differ in one bit give outputs that differ in about
    /// half their bits. Each line takes the value XOR itself shifted right (<c>value &gt;&gt; 30</c> moves every bit
    /// 30 places down, so the high bits are folded into the low ones; <c>^</c> sets a bit where exactly one of the
    /// two has it) and multiplies by a large odd constant (which spreads the low bits into the high ones). The
    /// shifts and constants are SplitMix64's published ones (Steele, Lea and Flood, 2014); this is a hash, not a
    /// secure one.
    /// </remarks>
    public static ulong Mix(ulong value)
    {
        // unchecked: the multiplications overflow 64 bits on purpose. The result wraps round (only the low 64 bits
        // are kept, as if counting modulo 2^64) instead of throwing, and the hash relies on that.
        unchecked
        {
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }

    /// <summary>The index-th output (counting from 0) of a SplitMix64 generator started at seed.</summary>
    public static ulong Nth(ulong seed, ulong index)
    {
        // A SplitMix64 generator adds Golden to its state before each output, so its (index + 1)-th state is
        // seed + (index + 1) * Golden, and any output can be computed directly without running the ones before it.
        // unchecked: the sum and product wrap round at 2^64, as the generator's do.
        unchecked
        {
            ulong position = index + 1;
            return Mix(seed + position * Golden);
        }
    }

    /// <summary>Seed of game number gameIndex in a run.</summary>
    public static ulong GameSeed(ulong runSeed, long gameIndex)
    {
        return Nth(runSeed, (ulong)gameIndex);
    }

    /// <summary>Seed of the random stream a game gives to the player in this seat.</summary>
    public static ulong AgentSeed(ulong gameSeed, int seat)
    {
        return Nth(gameSeed, AgentStreamBase + (ulong)seat);
    }

    /// <summary>The only place a Random is created.</summary>
    public static Random CreateRandom(ulong seed)
    {
        // Random takes a 32-bit int seed. seed >> 32 keeps the high 32 bits of the 64-bit seed (they are the
        // best-mixed ones); the cast to int reads them as a signed number, which unchecked allows to be negative
        // instead of throwing.
        unchecked
        {
            int randomSeed = (int)(seed >> 32);
            return new Random(randomSeed);
        }
    }
}
