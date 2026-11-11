using _7WondersGame.src.models.Effects;
using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Wonders
{
    /// <summary>One wonder stage: what it costs and what it does (E3).</summary>
    public sealed record WonderStage(IReadOnlyDictionary<Resource, int> Cost, params IEffect[] Effects);

    /// <summary>
    /// A player's wonder board: its fixed description from <see cref="WonderBoards"/> plus how many stages are built.
    /// </summary>
    public sealed class Wonder
    {
        /// <summary>Wonder boards, each with sides A and B: <see cref="WonderId"/> 0-6 are side A, 7-13 side B.</summary>
        public const int Boards = 7;

        /// <summary>The board of a wonder id, whatever the side (0-6).</summary>
        public static int BoardOf(int wonderId) => wonderId % Boards;

        private readonly WonderBoard _board;

        public Wonder(WonderId id)
        {
            _board = WonderBoards.All[id];
        }

        public WonderId Id => _board.Id;
        public string Name => _board.Name;
        public Resource Production => _board.Production;
        public IReadOnlyList<WonderStage> Stages => _board.Stages;
        public int StageCount => _board.Stages.Count;

        /// <summary>How many stages are built.</summary>
        public int Stage { get; set; } = 0;

        /// <summary>The most of each raw and manufactured resource that any one stage needs (used by the heuristics).</summary>
        public IReadOnlyDictionary<Resource, int> HighestCosts => _board.HighestCosts;

        /// <summary>What the next stage costs, or null when every stage is built.</summary>
        public IReadOnlyDictionary<Resource, int>? NextStageCost => Stage < StageCount ? _board.Stages[Stage].Cost : null;

        /// <summary>The lasting abilities of the built stages.</summary>
        public Ability Abilities => _board.AbilitiesAfter[Stage];

        /// <summary>Science symbols of the player's choice from the built stages (Babylon).</summary>
        public int ScienceWildcards => _board.ScienceWildcardsAfter[Stage];

        /// <summary>The victory points of the built stages.</summary>
        public int WonderPoints(PlayerState owner)
        {
            int vp = 0;
            for (int s = 0; s < Stage; s++)
                foreach (IEffect effect in _board.Stages[s].Effects)
                    vp += effect.EndGameVp(owner);
            return vp;
        }

        /// <summary>Only the stage counter changes during a game, so a shallow copy is a full copy.</summary>
        public object DeepCopy() => MemberwiseClone();

        /// <summary>
        /// Adds the next stage and applies its effects. The caller has already paid for it (see <see cref="Rules.BuildWonder"/>).
        /// </summary>
        /// <returns>false if every stage is already built</returns>
        public bool AddStage(PlayerState player)
        {
            if (Stage >= StageCount)
                return false;
            Stage++;
            foreach (IEffect effect in _board.Stages[Stage - 1].Effects)
                effect.OnBuild(player);
            return true;
        }

        public override string ToString() => Name;
    }

    /// <summary>A board's fixed description, with what its built stages add up to precomputed.</summary>
    public sealed class WonderBoard
    {
        public WonderId Id { get; }
        public string Name { get; }
        public Resource Production { get; }
        public IReadOnlyList<WonderStage> Stages { get; }
        public IReadOnlyDictionary<Resource, int> HighestCosts { get; }

        /// <summary>Indexed by the number of built stages.</summary>
        internal Ability[] AbilitiesAfter { get; }
        internal int[] ScienceWildcardsAfter { get; }

        public WonderBoard(WonderId id, string name, Resource production, params WonderStage[] stages)
        {
            Id = id;
            Name = name;
            Production = production;
            Stages = stages;

            var highest = new Dictionary<Resource, int>();
            for (Resource r = Resource.Wood; r <= Resource.Papyrus; r++)
                highest[r] = stages.Max(stage => stage.Cost.TryGetValue(r, out int n) ? n : 0);
            HighestCosts = highest;

            AbilitiesAfter = new Ability[stages.Length + 1];
            ScienceWildcardsAfter = new int[stages.Length + 1];
            for (int s = 0; s < stages.Length; s++)
            {
                AbilitiesAfter[s + 1] = AbilitiesAfter[s] | stages[s].Effects.Aggregate(Ability.None, (a, e) => a | e.Grants);
                ScienceWildcardsAfter[s + 1] = ScienceWildcardsAfter[s] + stages[s].Effects.OfType<ScienceWildcard>().Count();
            }
        }
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
