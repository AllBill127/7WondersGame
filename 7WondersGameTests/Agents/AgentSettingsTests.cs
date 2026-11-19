using _7WondersGame.Agents.Common.Settings;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.Mcts;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Agents;

/// <summary>
/// Each agent's settings, by name: only the properties marked <see cref="SettingAttribute"/> (own, or in a
/// <see cref="SettingGroupAttribute"/> property). A setting that appears or disappears fails here first.
/// </summary>
[TestClass]
public class AgentSettingsTests
{
    private static string[] NamesOf(Type agentType)
    {
        return AgentSettings.Of(agentType).Select(setting => setting.Name).ToArray();
    }

    [TestMethod]
    public void MctsAgent_HasItsConfiguration()
    {
        string[] expected =
        {
            "BackpropagationRule", "BudgetKind", "ExplorationConstant", "ExplorationFormula", "FinalMoveRule",
            "HiddenInformation", "IterationBudget", "MovePruning", "ParallelWorkerCount", "Parallelisation",
            "RandomRolloutMoveProbability", "RewardKind", "RolloutEnd", "RolloutPolicy", "ThreadLimit",
            "TimeBudgetMilliseconds", "TreeShape", "VirtualLossVisits",
        };
        CollectionAssert.AreEqual(expected, NamesOf(typeof(MctsAgent)));
    }

    [TestMethod]
    public void HeuristicAgent_HasItsWeights()
    {
        string[] expected =
        {
            "BonusPerUsefulResourceChoice", "CheapRawMaterialTradePenalty", "CivilianCardBonusEra1",
            "CoinValueInVictoryPoints", "DiscardPenalty", "EarlyScienceCardBonus",
            "FewOwnManufacturedGoodsCardsThreshold", "FewOwnRawMaterialCardsThreshold", "FirstScienceCardBonusEra1",
            "FirstScienceCardBonusEra2", "KeepOlympiaFreeBuildValue", "MissingWonderResourceBonusEra1",
            "MissingWonderResourceBonusEra2", "NeighbourRawMaterialCardsForCheapTrade", "NextPlayerDenialWeight",
            "NoManufacturedGoodsNearbyBonusEra2", "PoorCoinThreshold", "PoorPlayerCoinGainBonus",
            "TradingDiscountBonus", "TradingDiscountBonusPerNeighbourCard", "WonderStageBonus",
        };
        CollectionAssert.AreEqual(expected, NamesOf(typeof(HeuristicAgent)));
    }

    [TestMethod]
    public void RandomAgent_HasNone()
    {
        Assert.AreEqual(0, NamesOf(typeof(RandomAgent)).Length);
    }

    [TestMethod]
    public void OnlyMarkedPropertiesAreSettings()
    {
        CollectionAssert.AreEqual(new[] { "Iterations", "Weight" }, NamesOf(typeof(SettingsTestAgent)));
    }
}
