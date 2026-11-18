using System.Text.Json;
using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Common.Settings;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.Mcts;
using _7WondersGame.Agents.Mcts.Configuration;
using _7WondersGame.Agents.Mcts.Configuration.Enums;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Agents;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Games.Enums;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Randomness.Services;

namespace _7WondersGameTests.Agents.Mcts;

/// <summary>The configurable MCTS agent.</summary>
[TestClass]
public class MctsAgentTests
{
    /// <summary>
    /// An MctsAgent from the catalog, with the settings text as on the command line (":IterationBudget=40").
    /// </summary>
    internal static MctsAgent Mcts(string settings = "")
    {
        return (MctsAgent)AgentCatalog.Create(AgentSpec.Parse("MctsAgent" + settings));
    }

    private static Game NewGame(ulong seed, params IAgent[] agents)
    {
        var game = new Game(seed) { Agents = agents.ToList() };
        game.SetUpRandomGame();
        return game;
    }

    private static string Outcome(Game game)
    {
        return string.Join(" | ", game.Players.Select(PlayerOutcome));
    }

    private static string PlayerOutcome(PlayerState player)
    {
        return $"{player.VictoryPoints} {string.Join(",", player.BuiltCards.Select(card => card.Id))}";
    }

    // every value of every setting, each in a short game (the other settings at their defaults)
    [DataTestMethod]
    [DataRow("")]
    [DataRow(":HiddenInformation=PerfectInformation")]
    [DataRow(":TreeShape=Duct")]
    [DataRow(":TreeShape=Duct:BackpropagationRule=Paranoid")]
    [DataRow(":BackpropagationRule=Paranoid")]
    [DataRow(":BackpropagationRule=OwnScore")]
    [DataRow(":RewardKind=VictoryPointMargin:ExplorationConstant=15")]
    [DataRow(":RewardKind=RawVictoryPoints:ExplorationConstant=50:FinalMoveRule=HighestReward")]
    [DataRow(":ExplorationFormula=Smoothed")]
    [DataRow(":ExplorationFormula=Diminishing")]
    [DataRow(":FinalMoveRule=MostWins")]
    [DataRow(":RolloutPolicy=Heuristic")]
    [DataRow(":RolloutPolicy=Random")]
    [DataRow(":RolloutEnd=EraEnd")]
    [DataRow(":MovePruning=RandomCard")]
    [DataRow(":BudgetKind=Time:TimeBudgetMilliseconds=3")]
    [DataRow(":Parallelisation=Root:ParallelWorkerCount=3")]
    [DataRow(":Parallelisation=Root:ParallelWorkerCount=2:BudgetKind=Time:TimeBudgetMilliseconds=3")]
    [DataRow(":Parallelisation=Leaf:ParallelWorkerCount=3")]
    [DataRow(":Parallelisation=Tree:ParallelWorkerCount=3")]
    [DataRow(":Parallelisation=Tree:ParallelWorkerCount=3:VirtualLossVisits=1:TreeShape=Duct")]
    [DataRow(":Parallelisation=Tree:ParallelWorkerCount=2:BudgetKind=Time:TimeBudgetMilliseconds=3")]
    public void EverySetting_PlaysWholeGames(string settings)
    {
        foreach (int players in new[] { 3, 4 })
        {
            IAgent[] agents = new IAgent[players];
            for (int seat = 0; seat < players; seat++)
            {
                if (seat == 1)
                {
                    agents[seat] = Mcts(":IterationBudget=10" + settings);
                }
                else
                {
                    agents[seat] = new HeuristicAgent();
                }
            }

            Game game = NewGame(Seeding.GameSeed(5, players), agents);
            // a move the rules don't allow would throw
            game.Loop(record: true);
            Assert.AreEqual(Phase.GameOver, game.Phase);
            StringAssert.Contains(game.Results![1].StatsJson, "\"searches\":");
        }
    }

    [TestMethod]
    public void TheSameSeed_GivesTheSameGame()
    {
        Assert.AreEqual(PlayTwoSearchers(), PlayTwoSearchers());
    }

    private static string PlayTwoSearchers()
    {
        Game game = NewGame(77, new HeuristicAgent(), Mcts(":IterationBudget=15"),
                            Mcts(":IterationBudget=15:TreeShape=Duct"));
        game.Loop();
        return Outcome(game);
    }

    [TestMethod]
    public void ASearch_ChangesNothing()
    {
        Game game = NewGame(3, new HeuristicAgent(), new HeuristicAgent(), new HeuristicAgent());
        string before = Outcome(game) + AllHandsText(game);

        Move move = Mcts(":IterationBudget=50").Choose(new GameView(game, 0));

        CollectionAssert.Contains(game.LegalMoves(0), move);
        Assert.AreEqual(before, Outcome(game) + AllHandsText(game));
    }

    private static string AllHandsText(Game game)
    {
        return string.Join(",", game.Players.SelectMany(player => player.Hand).Select(card => card.Id));
    }

    [TestMethod]
    public void TheDeterminizedSearch_DoesntDependOnHiddenCards()
    {
        // two deals that differ only in what seat 0 can't see: the other hands and the later decks
        var deal = new Game(11) { Agents = { new HeuristicAgent(), new HeuristicAgent(), new HeuristicAgent() } };
        deal.SetUpRandomGame();
        List<int> wonders = deal.Players.Select(player => (int)player.Wonder.Id).ToList();
        List<int> era1 = CardIds(deal.Decks[0]);
        // the first seven cards (seat 0's hand) stay; the other two hands change places
        List<int> swapped = era1
            .Take(7)
            .Concat(era1.Skip(14).Take(7))
            .Concat(era1.Skip(7).Take(7))
            .ToList();
        List<int> era2Reversed = CardIds(deal.Decks[1].AsEnumerable().Reverse());
        List<int> era3Reversed = CardIds(deal.Decks[2].AsEnumerable().Reverse());
        var first = new[] { era1, CardIds(deal.Decks[1]), CardIds(deal.Decks[2]), wonders };
        var second = new[] { swapped, era2Reversed, era3Reversed, wonders };

        Assert.AreEqual(SearchFromDeal(first, ""), SearchFromDeal(second, ""));
        Assert.AreEqual(SearchFromDeal(first, ":TreeShape=Duct"), SearchFromDeal(second, ":TreeShape=Duct"));
    }

    private static List<int> CardIds(IEnumerable<Card> cards)
    {
        return cards.Select(card => (int)card.Id).ToList();
    }

    // seat 0's first choice in a game dealt from the dataset deal
    private static Move SearchFromDeal(List<int>[] dataset, string settings)
    {
        var game = new Game(11) { Agents = { new HeuristicAgent(), new HeuristicAgent(), new HeuristicAgent() } };
        game.SetUpGameFromDataset(dataset);
        return Mcts(":IterationBudget=200" + settings).Choose(new GameView(game, 0));
    }

    [TestMethod]
    public void Settings_AreLoggedAndReadBack()
    {
        MctsAgent agent = Mcts(":IterationBudget=50:BackpropagationRule=paranoid:ExplorationConstant=1.2" +
                               ":RandomRolloutMoveProbability=0.2");

        Assert.AreEqual(50, agent.Configuration.IterationBudget);
        Assert.AreEqual(MctsBackpropagationRule.Paranoid, agent.Configuration.BackpropagationRule);
        Assert.AreEqual(0.2, agent.Configuration.RandomRolloutMoveProbability);
        string json = ((IAgent)agent).ConfigurationJson();
        StringAssert.Contains(json, "\"BackpropagationRule\":\"Paranoid\"");
        StringAssert.Contains(json, "\"ExplorationConstant\":1.2");
        Assert.IsFalse(json.Contains("Searches") || json.Contains("_"), json);

        AgentSpec fromRow = AgentCatalog.FromResultRow("MctsAgent", json);
        Assert.AreEqual(json, AgentCatalog.Create(fromRow).ConfigurationJson());
        // every MctsConfiguration property is a setting, so equal settings JSON means an equal configuration
        Assert.AreEqual(((IAgent)new MctsAgent()).ConfigurationJson(), ((IAgent)Mcts()).ConfigurationJson(),
            "one agent's settings don't change the defaults");
    }

    [TestMethod]
    public void Stats_CountEveryIteration()
    {
        Game game = NewGame(9, Mcts(":IterationBudget=25"), new HeuristicAgent(), new HeuristicAgent());
        game.Loop(record: true);

        var statistics = JsonDocument.Parse(game.Results![0].StatsJson).RootElement;
        int searches = statistics.GetProperty("searches").GetInt32();
        Assert.IsTrue(searches >= 15, game.Results[0].StatsJson);
        Assert.AreEqual(25L * searches, statistics.GetProperty("iterations").GetInt64());
        Assert.AreEqual("{}", game.Results[1].StatsJson);
        Assert.IsTrue(statistics.GetProperty("allocatedMb").GetDouble() > 0,
                      "it measures the memory its searches allocate");
    }

    // Root and Leaf give every worker its copy and stream before any thread starts, so threads change nothing
    [DataTestMethod]
    [DataRow("Root")]
    [DataRow("Leaf")]
    public void RootAndLeaf_PlayTheSameWithOrWithoutThreads(string variant)
    {
        string withThreads = PlayFourWorkers(variant, "");
        Assert.AreEqual(withThreads, PlayFourWorkers(variant, ""));
        Assert.AreEqual(withThreads, PlayFourWorkers(variant, ":ThreadLimit=1"));
    }

    private static string PlayFourWorkers(string variant, string threads)
    {
        MctsAgent searcher = Mcts($":IterationBudget=40:Parallelisation={variant}:ParallelWorkerCount=4{threads}");
        Game game = NewGame(31, searcher, new HeuristicAgent(), new HeuristicAgent());
        game.Loop();
        return Outcome(game);
    }

    [DataTestMethod]
    [DataRow("Root", 4)]
    [DataRow("Leaf", 4)]
    [DataRow("Tree", 4)]
    [DataRow("Root", 1)]
    public void AParallelSearch_MakesItsIterations(string variant, int workers)
    {
        MctsAgent searcher = Mcts($":IterationBudget=40:Parallelisation={variant}:ParallelWorkerCount={workers}");
        Game game = NewGame(9, searcher, new HeuristicAgent(), new HeuristicAgent());
        game.Loop(record: true);

        var statistics = JsonDocument.Parse(game.Results![0].StatsJson).RootElement;
        int searches = statistics.GetProperty("searches").GetInt32();
        Assert.AreEqual(40L * searches, statistics.GetProperty("iterations").GetInt64(), game.Results[0].StatsJson);
        // one worker is the plain search, with the plain stats
        Assert.AreEqual(workers > 1, statistics.TryGetProperty("workers", out _), game.Results[0].StatsJson);
    }

    [TestMethod]
    public void OneWorker_IsThePlainSearch()
    {
        string plain = PlayThirtyIterations("");
        Assert.AreEqual(plain, PlayThirtyIterations(":Parallelisation=Root:ParallelWorkerCount=1"));
        Assert.AreEqual(plain, PlayThirtyIterations(":Parallelisation=Tree:ParallelWorkerCount=1"));
    }

    private static string PlayThirtyIterations(string settings)
    {
        Game game = NewGame(44, Mcts(":IterationBudget=30" + settings), new HeuristicAgent(), new HeuristicAgent());
        game.Loop();
        return Outcome(game);
    }

    [TestMethod]
    public void OnlyATimeBudget_UsesTheClock()
    {
        Assert.IsFalse(((IAgent)Mcts(":IterationBudget=10")).UsesClock);
        Assert.IsTrue(((IAgent)Mcts(":BudgetKind=Time:TimeBudgetMilliseconds=5")).UsesClock);
        Assert.IsFalse(((IAgent)new HeuristicAgent()).UsesClock);
    }

    /// <summary>
    /// A sanity check (a sign error in the tree policy would fail it); strength is measured in experiments.
    /// </summary>
    [TestMethod]
    public void AgainstRandomPlayers_ItScoresMore()
    {
        var averageVictoryPoints = new double[3];
        for (int gameIndex = 0; gameIndex < 4; gameIndex++)
        {
            Game game = NewGame(Seeding.GameSeed(21, gameIndex), Mcts(":IterationBudget=30"), new RandomAgent(),
                                new RandomAgent());
            game.Loop();
            for (int seat = 0; seat < 3; seat++)
            {
                averageVictoryPoints[seat] += game.Players[seat].VictoryPoints / 4.0;
            }
        }

        double bestRandomAgent = Math.Max(averageVictoryPoints[1], averageVictoryPoints[2]);
        Assert.IsTrue(averageVictoryPoints[0] > bestRandomAgent + 5,
                      $"average VP {string.Join(", ", averageVictoryPoints)}");
    }
}
