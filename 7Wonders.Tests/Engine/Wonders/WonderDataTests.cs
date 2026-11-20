using _7Wonders.Engine.Cards.Data;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Payment.Dtos;
using _7Wonders.Engine.Payment.Services;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;
using _7Wonders.Engine.Wonders.Entities;
using _7Wonders.Engine.Wonders.Enums;
using _7Wonders.Tests.TestHelpers;

namespace _7Wonders.Tests.Engine.Wonders;

/// <summary>
/// Wonder board data checked against the 1st edition (2010) rulebook.
/// The rows are typed from the rulebook, not copied from the board table (WonderBoards).
/// </summary>
[TestClass]
public class WonderDataTests
{
    private static readonly Resource[] Raw = { Resource.Wood, Resource.Ore, Resource.Clay, Resource.Stone };
    private static readonly Resource[] Manufactured = { Resource.Loom, Resource.Glass, Resource.Papyrus };

    [DataTestMethod]
    [DataRow("GizahA", "Stone")]          [DataRow("GizahB", "Stone")]
    [DataRow("BabylonA", "Clay")]         [DataRow("BabylonB", "Clay")]
    [DataRow("OlympiaA", "Wood")]         [DataRow("OlympiaB", "Wood")]
    [DataRow("RhodosA", "Ore")]           [DataRow("RhodosB", "Ore")]
    [DataRow("EphesosA", "Papyrus")]      [DataRow("EphesosB", "Papyrus")]
    [DataRow("AlexandriaA", "Glass")]     [DataRow("AlexandriaB", "Glass")]
    [DataRow("HalikarnassosA", "Loom")]   [DataRow("HalikarnassosB", "Loom")]
    public void Board_ProducesRulebookResource(string wonder, string resource)
    {
        Assert.AreEqual(Enum.Parse<Resource>(resource), CreateWonder(wonder).Production);
    }

    [DataTestMethod]
    [DataRow("GizahA", 3)]          [DataRow("GizahB", 4)]
    [DataRow("BabylonA", 3)]        [DataRow("BabylonB", 3)]
    [DataRow("OlympiaA", 3)]        [DataRow("OlympiaB", 3)]
    [DataRow("RhodosA", 3)]         [DataRow("RhodosB", 2)]
    [DataRow("EphesosA", 3)]        [DataRow("EphesosB", 3)]
    [DataRow("AlexandriaA", 3)]     [DataRow("AlexandriaB", 3)]
    [DataRow("HalikarnassosA", 3)]  [DataRow("HalikarnassosB", 3)]
    public void Board_HasRulebookStageCount(string wonder, int stages)
    {
        PlayerState player = SetUpPlayer(wonder);
        for (int stageIndex = 0; stageIndex < stages; stageIndex++)
        {
            GiveResources(player, 10);
            Assert.IsTrue(player.Wonder.AddStage(player), $"stage {stageIndex + 1}");
        }

        GiveResources(player, 10);
        player.Resources[Resource.Coins] = 100;
        Assert.IsFalse(BuildRules.CanBuildWonderStage(player), "one stage too many");
    }

    // Columns: wonder, stage, cost, VP, coins, shields, other effect.
    [DataTestMethod]
    [DataRow("GizahA", 1, "2 Stone",            3, 0, 0, "")]
    [DataRow("GizahA", 2, "3 Wood",             5, 0, 0, "")]
    [DataRow("GizahA", 3, "4 Stone",            7, 0, 0, "")]
    [DataRow("GizahB", 1, "2 Wood",             3, 0, 0, "")]
    [DataRow("GizahB", 2, "3 Stone",            5, 0, 0, "")]
    [DataRow("GizahB", 3, "3 Clay",             5, 0, 0, "")]
    [DataRow("GizahB", 4, "4 Stone, Papyrus",   7, 0, 0, "")]
    [DataRow("BabylonA", 1, "2 Clay",           3, 0, 0, "")]
    [DataRow("BabylonA", 2, "3 Wood",           0, 0, 0, "ScienceWildcard")]
    [DataRow("BabylonA", 3, "4 Clay",           7, 0, 0, "")]
    [DataRow("BabylonB", 1, "Clay, Loom",       3, 0, 0, "")]
    [DataRow("BabylonB", 2, "2 Wood, Glass",    0, 0, 0, "PlaySeventhCard")]
    [DataRow("BabylonB", 3, "3 Clay, Papyrus",  0, 0, 0, "ScienceWildcard")]
    [DataRow("OlympiaA", 1, "2 Wood",           3, 0, 0, "")]
    [DataRow("OlympiaA", 2, "2 Stone",          0, 0, 0, "FreeBuildOncePerAge")]
    [DataRow("OlympiaA", 3, "2 Ore",            7, 0, 0, "")]
    [DataRow("OlympiaB", 1, "2 Wood",           0, 0, 0, "RawTradingDiscount")]
    [DataRow("OlympiaB", 2, "2 Stone",          5, 0, 0, "")]
    [DataRow("OlympiaB", 3, "2 Ore, Loom",      0, 0, 0, "CopyGuild")]
    [DataRow("RhodosA", 1, "2 Wood",            3, 0, 0, "")]
    [DataRow("RhodosA", 2, "3 Clay",            0, 0, 2, "")]
    [DataRow("RhodosA", 3, "4 Ore",             7, 0, 0, "")]
    [DataRow("RhodosB", 1, "3 Stone",           3, 3, 1, "")]
    [DataRow("RhodosB", 2, "4 Ore",             4, 4, 1, "")]
    [DataRow("EphesosA", 1, "2 Stone",          3, 0, 0, "")]
    [DataRow("EphesosA", 2, "2 Wood",           0, 9, 0, "")]
    [DataRow("EphesosA", 3, "2 Papyrus",        7, 0, 0, "")]
    [DataRow("EphesosB", 1, "2 Stone",          2, 4, 0, "")]
    [DataRow("EphesosB", 2, "2 Wood",           3, 4, 0, "")]
    [DataRow("EphesosB", 3, "Papyrus, Loom, Glass", 5, 4, 0, "")]
    [DataRow("AlexandriaA", 1, "2 Stone",       3, 0, 0, "")]
    [DataRow("AlexandriaA", 2, "2 Ore",         0, 0, 0, "RawWildcard")]
    [DataRow("AlexandriaA", 3, "2 Glass",       7, 0, 0, "")]
    [DataRow("AlexandriaB", 1, "2 Clay",        0, 0, 0, "RawWildcard")]
    [DataRow("AlexandriaB", 2, "2 Wood",        0, 0, 0, "ManufacturedWildcard")]
    [DataRow("AlexandriaB", 3, "3 Stone",       7, 0, 0, "")]
    [DataRow("HalikarnassosA", 1, "2 Clay",     3, 0, 0, "")]
    [DataRow("HalikarnassosA", 2, "3 Ore",      0, 0, 0, "BuildFromDiscard")]
    [DataRow("HalikarnassosA", 3, "2 Loom",     7, 0, 0, "")]
    [DataRow("HalikarnassosB", 1, "2 Ore",      2, 0, 0, "BuildFromDiscard")]
    [DataRow("HalikarnassosB", 2, "3 Clay",     1, 0, 0, "BuildFromDiscard")]
    [DataRow("HalikarnassosB", 3, "Glass, Papyrus, Loom", 0, 0, 0, "BuildFromDiscard")]
    public void Stage_MatchesRulebook(
        string wonder,
        int stage,
        string cost,
        int victoryPoints,
        int coins,
        int shields,
        string effect)
    {
        PlayerState player = SetUpPlayer(wonder);
        for (int earlierStage = 1; earlierStage < stage; earlierStage++)
        {
            GiveResources(player, 10);
            Assert.IsTrue(player.Wonder.AddStage(player), $"building earlier stage {earlierStage}");
        }

        int builtStages = stage - 1;
        Dictionary<Resource, int> stageCost = ParseCost(cost);

        CheckExactlyTheCostIsEnough(player, wonder, builtStages, stageCost);

        // Build it and check the rewards.
        SetResources(player, stageCost);
        int victoryPointsBefore = player.Wonder.WonderPoints(player);
        int shieldsBefore = player.Resources[Resource.Shields];
        int scienceBefore = Scoring.CalculateScientificScore(player);
        Ability abilitiesBefore = player.Wonder.Abilities;
        player.FreeBuildAvailable = false;
        player.DiscardPileBuildPending = false;

        Assert.IsTrue(player.Wonder.AddStage(player), "build stage");
        Assert.AreEqual(stage, player.Wonder.BuiltStageCount, "stage number");
        Assert.AreEqual(victoryPoints, player.Wonder.WonderPoints(player) - victoryPointsBefore, "VP");
        Assert.AreEqual(coins, player.Resources[Resource.Coins], "coins");
        Assert.AreEqual(shields, player.Resources[Resource.Shields] - shieldsBefore, "shields");

        Ability gained = player.Wonder.Abilities & ~abilitiesBefore;
        CheckEffect(player, effect, gained, scienceBefore);
    }

    // ===== helpers =====

    /// <summary>
    /// Exactly the rulebook cost is enough (no coins, so nothing can be bought), and one less of any resource is
    /// not, unless the board's own wildcard covers it.
    /// </summary>
    private static void CheckExactlyTheCostIsEnough(
        PlayerState player,
        string wonder,
        int builtStages,
        Dictionary<Resource, int> stageCost)
    {
        SetResources(player, stageCost);
        Assert.IsTrue(BuildRules.CanBuildWonderStage(player), "exact cost should be enough");

        foreach (Resource resource in stageCost.Keys)
        {
            if (WildcardCovers(resource, wonder, builtStages))
            {
                continue;
            }

            Dictionary<Resource, int> lessOne = new(stageCost);
            lessOne[resource]--;
            SetResources(player, lessOne);
            Assert.IsFalse(BuildRules.CanBuildWonderStage(player), $"one {resource} short should not be enough");
        }
    }

    // Alexandria A's stage 2 makes any raw resource; Alexandria B's stage 1 any raw, its stage 2 any manufactured
    private static bool WildcardCovers(Resource resource, string wonder, int builtStages)
    {
        bool anyRaw = wonder == "AlexandriaA" && builtStages >= 2 || wonder == "AlexandriaB" && builtStages >= 1;
        bool anyManufactured = wonder == "AlexandriaB" && builtStages >= 2;
        return Raw.Contains(resource) && anyRaw || Manufactured.Contains(resource) && anyManufactured;
    }

    /// <summary>The stage gave exactly the <paramref name="effect"/> named in the row, and nothing else.</summary>
    private static void CheckEffect(PlayerState player, string effect, Ability gained, int scienceBefore)
    {
        bool freeBuild = player.FreeBuildAvailable && gained.HasFlag(Ability.FreeBuildEachEra);
        int expectedScienceGain = 0;
        if (effect == "ScienceWildcard")
        {
            expectedScienceGain = 1;
        }

        Assert.AreEqual(effect == "PlaySeventhCard", gained.HasFlag(Ability.PlaySeventhCard), "play seventh card");
        Assert.AreEqual(effect == "FreeBuildOncePerAge", freeBuild, "free build once per age");
        Assert.AreEqual(effect == "BuildFromDiscard", player.DiscardPileBuildPending, "build from discard");
        Assert.AreEqual(effect == "RawTradingDiscount", gained.HasFlag(Ability.RawMaterialsCheapFromBothNeighbours),
                        "raw trading discount");
        Assert.AreEqual(effect == "RawWildcard", gained.HasFlag(Ability.AnyRawMaterialEachTurn), "raw wildcard");
        Assert.AreEqual(
            effect == "ManufacturedWildcard",
            gained.HasFlag(Ability.AnyManufacturedGoodEachTurn),
            "manufactured wildcard");
        Assert.AreEqual(effect == "CopyGuild", gained.HasFlag(Ability.CopyGuild), "copy a guild");
        Assert.AreEqual(
            expectedScienceGain,
            Scoring.CalculateScientificScore(player) - scienceBefore,
            "science wildcard");

        if (effect == "RawWildcard")
        {
            CheckWildcardGivesEach(player, Raw);
        }

        if (effect == "ManufacturedWildcard")
        {
            CheckWildcardGivesEach(player, Manufactured);
        }

        if (effect == "CopyGuild")
        {
            // The east neighbour's Workers Guild is worth 1 VP per brown card next door.
            player.EastNeighbour.BuiltCards.Add(CardsDataset.GameCards[CardId.Workers]);
            player.WestNeighbour.BuiltCards.Add(CardsDataset.GameCards[CardId.LumberYard]);
            Assert.IsTrue(GuildCopying.CopyBestNeighbourGuild(player), "copy a neighbour's guild");
        }
    }

    private static void CheckWildcardGivesEach(PlayerState player, Resource[] covered)
    {
        foreach (Resource resource in covered)
        {
            SetResources(player, new Dictionary<Resource, int>());
            Dictionary<Resource, int> oneOfIt = new() { { resource, 1 } };
            Assert.AreEqual(PaymentPlan.Free, PaymentPlanner.Plan(player, oneOfIt), $"wildcard gives {resource}");
        }
    }

    private static WonderBoardProgress CreateWonder(string name)
    {
        return new WonderBoardProgress(Enum.Parse<WonderId>(name));
    }

    private static PlayerState SetUpPlayer(string wonder)
    {
        List<WonderBoardProgress> wonders = new()
        {
            CreateWonder(wonder),
            new WonderBoardProgress(WonderId.GizahA),
            new WonderBoardProgress(WonderId.GizahA)
        };
        List<PlayerState> players = MockHelper.MockPlayers(wonders);
        foreach (PlayerState player in players)
        {
            player.Resources[Resource.Coins] = 0;
        }

        return players[0];
    }

    private static void GiveResources(PlayerState player, int amount)
    {
        foreach (Resource resource in Raw.Concat(Manufactured))
        {
            player.Resources[resource] = amount;
        }
    }

    /// <summary>Sets raw and manufactured resources to exactly these amounts, and coins to 0.</summary>
    private static void SetResources(PlayerState player, Dictionary<Resource, int> amounts)
    {
        GiveResources(player, 0);
        foreach (KeyValuePair<Resource, int> kvp in amounts)
        {
            player.Resources[kvp.Key] = kvp.Value;
        }

        player.Resources[Resource.Coins] = 0;
    }

    // "2 Wood" or "Glass": an amount (1 if left out), then the resource
    private static Dictionary<Resource, int> ParseCost(string cost)
    {
        Dictionary<Resource, int> result = new();
        foreach (string part in cost.Split(", "))
        {
            string[] words = part.Split(' ');
            int amount = 1;
            if (words.Length == 2)
            {
                amount = int.Parse(words[0]);
            }

            string resourceName = words.Last();
            result[Enum.Parse<Resource>(resourceName)] = amount;
        }

        return result;
    }
}
