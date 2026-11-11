using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Effects
{
    /// <summary>
    /// What a card or wonder stage does (E3). <see cref="OnBuild"/> runs once, when it is built;
    /// <see cref="EndGameVp"/> is its share of the final score. Effects are shared by every game, so they hold no state.
    /// </summary>
    public interface IEffect
    {
        void OnBuild(PlayerState p) { }
        int EndGameVp(PlayerState p) => 0;
        /// <summary>The lasting abilities it gives, if any.</summary>
        Ability Grants => Ability.None;
    }

    /// <summary>
    /// Lasting abilities a card or wonder stage gives. A card's go into <see cref="PlayerState.Abilities"/> when it is
    /// built; a wonder's are read from the board's stage (<see cref="Wonders.Wonder.Abilities"/>).
    /// Check them with <see cref="Rules.Has"/>.
    /// </summary>
    [Flags]
    public enum Ability
    {
        None = 0,
        /// <summary>East Trading Post: raw resources cost 1 coin from the east neighbour.</summary>
        RawCheapEast = 1 << 0,
        /// <summary>West Trading Post: raw resources cost 1 coin from the west neighbour.</summary>
        RawCheapWest = 1 << 1,
        /// <summary>Marketplace: manufactured goods cost 1 coin from both neighbours.</summary>
        ManufCheap = 1 << 2,
        /// <summary>Olympia B stage 1: raw resources cost 1 coin from both neighbours.</summary>
        WonderRawCheap = 1 << 3,
        /// <summary>Alexandria A stage 2, B stage 1: one raw resource of the player's choice each turn.</summary>
        AnyRaw = 1 << 4,
        /// <summary>Alexandria B stage 2: one manufactured good of the player's choice each turn.</summary>
        AnyManufactured = 1 << 5,
        /// <summary>Babylon B stage 2: also plays the 7th card of each era.</summary>
        PlaySeventh = 1 << 6,
        /// <summary>Olympia A stage 2: one card per era for free (the use is <see cref="PlayerState.FreeCardOnce"/>).</summary>
        FreeCardEachEra = 1 << 7,
        /// <summary>Olympia B stage 3: copies a neighbour's guild at the end of the game.</summary>
        CopyGuild = 1 << 8,
    }

    /// <summary>Whose city a "per card" or "per stage" effect counts.</summary>
    public enum Who
    {
        Self,
        Neighbours,
        /// <summary>The player and both neighbours.</summary>
        All,
    }

    /// <summary>Adds to a resource when built: production, coins or shields, or a science symbol.</summary>
    public sealed record Gain(Resource Resource, int Amount = 1) : IEffect
    {
        public void OnBuild(PlayerState p) => p.Resources[Resource] += Amount;
    }

    /// <summary>
    /// One unit of any of <paramref name="Choices"/> each turn, chosen when paying (see <see cref="PaymentPlanner"/>).
    /// Neighbours can buy it only if <paramref name="Sellable"/> (the brown cards, not Caravansery or Forum).
    /// </summary>
    public sealed record OneOf(bool Sellable, params Resource[] Choices) : IEffect
    {
        /// <summary>Bit i stands for the resource with value i.</summary>
        public int Mask => Choices.Aggregate(0, (mask, r) => mask | 1 << (int)r);
    }

    /// <summary>Coins when built.</summary>
    public sealed record Coins(int Amount) : IEffect
    {
        public void OnBuild(PlayerState p) => p.Resources[Resource.Coins] += Amount;
    }

    /// <summary>Coins when built, for each card of a type in <paramref name="Who"/>'s cities (built cards count).</summary>
    public sealed record CoinsPerCard(CardType Type, int Coins, Who Who) : IEffect
    {
        public void OnBuild(PlayerState p) => p.Resources[Resource.Coins] += Coins * Cities.CountCards(p, Who, Type);
    }

    /// <summary>Coins when built, for each of the player's built wonder stages.</summary>
    public sealed record CoinsPerWonderStage(int Coins) : IEffect
    {
        public void OnBuild(PlayerState p) => p.Resources[Resource.Coins] += Coins * p.Board.Stage;
    }

    /// <summary>Victory points at the end of the game.</summary>
    public sealed record Points(int Vp) : IEffect
    {
        public int EndGameVp(PlayerState p) => Vp;
    }

    /// <summary>Victory points for each card of the given types in <paramref name="Who"/>'s cities at the end.</summary>
    public sealed record PointsPerCard(int Vp, Who Who, params CardType[] Types) : IEffect
    {
        public int EndGameVp(PlayerState p) => Vp * Types.Sum(type => Cities.CountCards(p, Who, type));
    }

    /// <summary>Victory points for each built wonder stage in <paramref name="Who"/>'s cities at the end.</summary>
    public sealed record PointsPerWonderStage(int Vp, Who Who) : IEffect
    {
        public int EndGameVp(PlayerState p) => Vp * Cities.Count(p, Who, q => q.Board.Stage);
    }

    /// <summary>Strategists Guild: victory points for each defeat token the neighbours hold at the end.</summary>
    public sealed record PointsPerNeighbourDefeat(int Vp) : IEffect
    {
        public int EndGameVp(PlayerState p) => Vp * Cities.Count(p, Who.Neighbours, q => q.DefeatTokens);
    }

    /// <summary>A lasting ability (see <see cref="Ability"/>).</summary>
    public sealed record Grant(Ability Ability) : IEffect
    {
        public void OnBuild(PlayerState p) => p.Abilities |= Ability;
        public Ability Grants => Ability;
    }

    /// <summary>
    /// A science symbol of the player's choice at the end (Scientists Guild, Babylon). Scored in
    /// <see cref="Rules.CalculateScientificScore"/>, since science points don't add up card by card.
    /// </summary>
    public sealed record ScienceWildcard : IEffect;

    /// <summary>
    /// Olympia A stage 2: one card per era for free. The ability is on from this stage; the first use is available
    /// straight away, and <see cref="Game"/> re-arms it at the start of each era.
    /// </summary>
    public sealed record FreeCardEachEra : IEffect
    {
        public void OnBuild(PlayerState p)
        {
            p.Abilities |= Ability.FreeCardEachEra;
            p.FreeCardOnce = true;
        }

        public Ability Grants => Ability.FreeCardEachEra;
    }

    /// <summary>Halikarnassos: at the end of this turn, build one card from the discard pile for free.</summary>
    public sealed record BuildFromDiscardPile : IEffect
    {
        public void OnBuild(PlayerState p) => p.DiscardFree = true;
    }

    internal static class Cities
    {
        public static int CountCards(PlayerState p, Who who, CardType type) => Count(p, who, q => q.AmountOfType(type));

        public static int Count(PlayerState p, Who who, Func<PlayerState, int> count) => who switch
        {
            Who.Self => count(p),
            Who.Neighbours => count(p.PlayerEast) + count(p.PlayerWest),
            _ => count(p.PlayerEast) + count(p.PlayerWest) + count(p),
        };
    }
}
