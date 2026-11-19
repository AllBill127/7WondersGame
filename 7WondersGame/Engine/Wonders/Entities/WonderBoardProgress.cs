using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Effects.Interfaces;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;
using _7WondersGame.Engine.Wonders.Data;
using _7WondersGame.Engine.Wonders.Dtos;
using _7WondersGame.Engine.Wonders.Enums;

namespace _7WondersGame.Engine.Wonders.Entities;

/// <summary>
/// A player's wonder board: its fixed description from <see cref="WonderBoards"/> plus how many stages are built.
/// </summary>
public sealed class WonderBoardProgress
{
    /// <summary>Wonder boards, each with sides A and B: <see cref="WonderId"/> 0-6 are side A, 7-13 side B.</summary>
    public const int BoardCount = 7;

    /// <summary>The board of a wonder id, whatever the side (0-6).</summary>
    public static int BoardOf(int wonderId)
    {
        return wonderId % BoardCount;
    }

    private readonly WonderBoard _board;

    /// <summary>The board <paramref name="id"/> with no stage built.</summary>
    public WonderBoardProgress(WonderId id)
    {
        _board = WonderBoards.All[id];
    }

    /// <summary>Which board and side this is.</summary>
    public WonderId Id
    {
        get
        {
            return _board.Id;
        }
    }

    /// <summary>The board's name with its side, e.g. "Gizah A".</summary>
    public string Name
    {
        get
        {
            return _board.Name;
        }
    }

    /// <summary>The resource the board produces from the start.</summary>
    public Resource Production
    {
        get
        {
            return _board.Production;
        }
    }

    /// <summary>The board's stages in building order.</summary>
    public IReadOnlyList<WonderStage> Stages
    {
        get
        {
            return _board.Stages;
        }
    }

    /// <summary>How many stages the board has.</summary>
    public int StageCount
    {
        get
        {
            return _board.Stages.Count;
        }
    }

    /// <summary>How many stages are built.</summary>
    public int BuiltStageCount { get; set; } = 0;

    /// <summary>
    /// The most of each raw and manufactured resource that any one stage needs (used by the heuristics).
    /// </summary>
    public IReadOnlyDictionary<Resource, int> HighestCosts
    {
        get
        {
            return _board.HighestCosts;
        }
    }

    /// <summary>What the next stage costs, or null when every stage is built.</summary>
    public IReadOnlyDictionary<Resource, int>? NextStageCost
    {
        get
        {
            if (BuiltStageCount >= StageCount)
            {
                return null;
            }
            return _board.Stages[BuiltStageCount].Cost;
        }
    }

    /// <summary>The lasting abilities of the built stages.</summary>
    public Ability Abilities
    {
        get
        {
            return _board.AbilitiesAfter[BuiltStageCount];
        }
    }

    /// <summary>Science symbols of the player's choice from the built stages (Babylon).</summary>
    public int ScienceWildcards
    {
        get
        {
            return _board.ScienceWildcardsAfter[BuiltStageCount];
        }
    }

    /// <summary>The victory points of the built stages.</summary>
    public int WonderPoints(PlayerState owner)
    {
        int victoryPoints = 0;
        for (int stageIndex = 0; stageIndex < BuiltStageCount; stageIndex++)
        {
            foreach (IEffect effect in _board.Stages[stageIndex].Effects)
            {
                victoryPoints += effect.EndGameVictoryPoints(owner);
            }
        }
        return victoryPoints;
    }

    /// <summary>Only the stage counter changes during a game, so a shallow copy is a full copy.</summary>
    public WonderBoardProgress Clone()
    {
        return (WonderBoardProgress)MemberwiseClone();
    }

    /// <summary>
    /// Adds the next stage and applies its effects. The caller has already paid for it (see
    /// <see cref="MoveResolution.Place"/>).
    /// </summary>
    /// <returns>false if every stage is already built</returns>
    public bool AddStage(PlayerState player)
    {
        if (BuiltStageCount >= StageCount)
        {
            return false;
        }

        WonderStage builtStage = _board.Stages[BuiltStageCount];
        BuiltStageCount++;
        foreach (IEffect effect in builtStage.Effects)
        {
            effect.OnBuild(player);
        }
        return true;
    }

    /// <summary>The board's name.</summary>
    public override string ToString()
    {
        return Name;
    }
}
