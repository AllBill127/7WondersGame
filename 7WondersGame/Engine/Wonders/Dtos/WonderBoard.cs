using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Effects;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Wonders.Enums;

namespace _7WondersGame.Engine.Wonders.Dtos;

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
