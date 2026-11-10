using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    public abstract class Wonder
    {
        public WonderId Id { get; private set; }
        public string Name { get; private set; }
        public int Stage { get; set; } = 0;
        public int WonderPoints { get; set; } = 0;
        public Resource Production { get; private set; }

        /// <summary>The cost of each stage in building order. Shared by copies and never changed.</summary>
        private readonly IReadOnlyDictionary<Resource, int>[] stageCosts;

        /// <summary>The most of each raw and manufactured resource that any one stage needs (used by the heuristics).</summary>
        public IReadOnlyDictionary<Resource, int> HighestCosts { get; }

        protected Wonder(WonderId id, string name, Resource production, params IReadOnlyDictionary<Resource, int>[] stageCosts)
        {
            Id = id;
            Name = name;
            Production = production;
            this.stageCosts = stageCosts;

            var highest = new Dictionary<Resource, int>();
            for (Resource r = Resource.Wood; r <= Resource.Papyrus; r++)
                highest[r] = stageCosts.Max(cost => cost.TryGetValue(r, out int n) ? n : 0);
            HighestCosts = highest;
        }

        /// <summary>A stage cost, written in the order its resources are paid.</summary>
        protected static IReadOnlyDictionary<Resource, int> Cost(params (Resource Resource, int Amount)[] parts) =>
            parts.ToDictionary(p => p.Resource, p => p.Amount);

        public int StageCount => stageCosts.Length;

        /// <summary>What the next stage costs, or null when every stage is built.</summary>
        public IReadOnlyDictionary<Resource, int>? NextStageCost => Stage < stageCosts.Length ? stageCosts[Stage] : null;

        /// <summary>Only the stage counter and points change during a game, so a shallow copy is a full copy.</summary>
        public object DeepCopy() => MemberwiseClone();

        /// <summary>
        /// Adds the next stage and applies its effect. The caller has already paid for it (see <see cref="Player.BuildWonder"/>).
        /// </summary>
        /// <returns>false if every stage is already built</returns>
        public bool AddStage(Player player)
        {
            if (Stage >= stageCosts.Length)
                return false;
            Stage++;
            ApplyStageEffect(player, Stage);
            return true;
        }

        /// <summary>
        /// The immediate effect of the stage just built (1 = first stage): points, coins, shields or a flag.
        /// Effects read later from <see cref="Stage"/> (Babylon's science symbol, Alexandria's wildcards,
        /// Olympia B's guild copy) need nothing here.
        /// </summary>
        protected abstract void ApplyStageEffect(Player player, int stage);
    }

    public enum WonderId{
        // 0-6 side A
        GizahA,
        BabylonA,
        OlympiaA,
        RhodosA,
        EphesosA,
        AlexandriaA,
        HalikarnassosA,
        // 7-13 side B
        GizahB,
        BabylonB,
        OlympiaB,
        RhodosB,
        EphesosB,
        AlexandriaB,
        HalikarnassosB,
    };
}
