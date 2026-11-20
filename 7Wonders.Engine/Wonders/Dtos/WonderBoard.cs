using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Effects;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Wonders.Enums;

namespace _7Wonders.Engine.Wonders.Dtos;

/// <summary>A board's fixed description, with what its built stages add up to precomputed.</summary>
internal sealed class WonderBoard
{
    /// <summary>Which board and side this is.</summary>
    public WonderId Id { get; }

    /// <summary>The board's name with its side, e.g. "Gizah A".</summary>
    public string Name { get; }

    /// <summary>The resource the board produces from the start.</summary>
    public Resource Production { get; }

    /// <summary>The stages in building order.</summary>
    public IReadOnlyList<WonderStage> Stages { get; }

    /// <summary>The most of each raw and manufactured resource that any one stage needs.</summary>
    public IReadOnlyDictionary<Resource, int> HighestCosts { get; }

    /// <summary>The lasting abilities of the built stages, indexed by the number of built stages.</summary>
    internal Ability[] AbilitiesAfter { get; }

    /// <summary>The science wildcards of the built stages, indexed by the number of built stages.</summary>
    internal int[] ScienceWildcardsAfter { get; }

    /// <summary>The board <paramref name="id"/> with its <paramref name="stages"/> in building order.</summary>
    public WonderBoard(WonderId id, string name, Resource production, params WonderStage[] stages)
    {
        Id = id;
        Name = name;
        Production = production;
        Stages = stages;

        Dictionary<Resource, int> highest = new();
        for (Resource resource = Resource.Wood; resource <= Resource.Papyrus; resource++)
        {
            highest[resource] = HighestCostOf(stages, resource);
        }
        HighestCosts = highest;

        // After stageIndex + 1 stages: what the stages before it gave plus what this stage gives.
        int builtStageCounts = stages.Length + 1; // 0 to every stage built
        AbilitiesAfter = new Ability[builtStageCounts];
        ScienceWildcardsAfter = new int[builtStageCounts];
        for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
        {
            int builtStages = stageIndex + 1;
            WonderStage stage = stages[stageIndex];
            stage.Wonder = id;
            stage.Number = builtStages;
            AbilitiesAfter[builtStages] = AbilitiesAfter[stageIndex] | AbilitiesGrantedBy(stage);
            ScienceWildcardsAfter[builtStages] = ScienceWildcardsAfter[stageIndex] + ScienceWildcardsIn(stage);
        }
    }

    /// <summary>
    /// The most of <paramref name="resource"/> any one of <paramref name="stages"/> costs (0 if none).
    /// </summary>
    private static int HighestCostOf(WonderStage[] stages, Resource resource)
    {
        int highest = 0;
        foreach (WonderStage stage in stages)
        {
            if (stage.Cost.TryGetValue(resource, out int amount) && amount > highest)
            {
                highest = amount;
            }
        }
        return highest;
    }

    /// <summary>Every lasting ability the stage's effects give.</summary>
    private static Ability AbilitiesGrantedBy(WonderStage stage)
    {
        Ability abilities = Ability.None;
        foreach (IEffect effect in stage.Effects)
        {
            abilities |= effect.Grants;
        }
        return abilities;
    }

    /// <summary>How many science symbols of the player's choice the stage gives.</summary>
    private static int ScienceWildcardsIn(WonderStage stage)
    {
        int wildcards = 0;
        foreach (IEffect effect in stage.Effects)
        {
            if (effect is ScienceWildcard)
            {
                wildcards++;
            }
        }
        return wildcards;
    }
}
