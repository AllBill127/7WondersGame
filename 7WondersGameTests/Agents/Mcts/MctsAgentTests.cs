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
    /// <summary>An MctsAgent from the catalog, with the settings text as on the command line (":n=40").</summary>
    internal static MctsAgent Mcts(string settings = "")
    {
        return (MctsAgent)AgentCatalog.Create(AgentSpec.Parse("MctsAgent" + settings));
    }

    private static Game NewGame(ulong seed, params IAgent[] agents)
    {
        var game = new Game(seed) { Agents = agents.ToList() };
        game.InitGame();
        return game;
    }

    private static string Outcome(Game game)
    {
        return string.Join(" | ", game.Players.Select(PlayerOutcome));
    }

    private static string PlayerOutcome(PlayerState player)
    {
        return $"{player.VictoryPoints} {string.Join(",", player.PlayedCards.Select(card => card.Id))}";
    }

    // every value of every setting, each in a short game (the other settings at their defaults)
    [DataTestMethod]
    [DataRow("")]
    [DataRow(":Information=PerfectInformation")]
    [DataRow(":Tree=Duct")]
    [DataRow(":Tree=Duct:Backup=Paranoid")]
    [DataRow(":Backup=Paranoid")]
    [DataRow(":Backup=OwnScore")]
    [DataRow(":Reward=VpMargin:C=15")]
    [DataRow(":Reward=RawVp:C=50:FinalMove=HighestReward")]
    [DataRow(":Exploration=Smoothed")]
    [DataRow(":Exploration=Diminishing")]
    [DataRow(":FinalMove=MostWins")]
    [DataRow(":Rollout=Heuristic")]
    [DataRow(":Rollout=Random")]
    [DataRow(":RolloutEnd=EraEnd")]
    [DataRow(":Pruning=RandomCard")]
    [DataRow(":Budget=Time:TimeMs=3")]
    [DataRow(":Parallel=Root:Workers=3")]
    [DataRow(":Parallel=Root:Workers=2:Budget=Time:TimeMs=3")]
    [DataRow(":Parallel=Leaf:Workers=3")]
    [DataRow(":Parallel=Tree:Workers=3")]
    [DataRow(":Parallel=Tree:Workers=3:VirtualLoss=1:Tree=Duct")]
    [DataRow(":Parallel=Tree:Workers=2:Budget=Time:TimeMs=3")]
    public void EverySetting_PlaysWholeGames(string settings)
    {
        foreach (int players in new[] { 3, 4 })
        {
            IAgent[] agents = new IAgent[players];
            for (int seat = 0; seat < players; seat++)
            {
                if (seat == 1)
                {
                    agents[seat] = Mcts(":n=10" + settings);
                }
                else
                {
                    agents[seat] = new HeuristicAI();
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
        Game game = NewGame(77, new HeuristicAI(), Mcts(":n=15"), Mcts(":n=15:Tree=Duct"));
        game.Loop();
        return Outcome(game);
    }

    [TestMethod]
    public void ASearch_ChangesNothing()
    {
        Game game = NewGame(3, new HeuristicAI(), new HeuristicAI(), new HeuristicAI());
        string before = Outcome(game) + AllHandsText(game);

        Command move = Mcts(":n=50").Choose(new GameView(game, 0));

        CollectionAssert.Contains(game.LegalMoves(0), move);
        Assert.AreEqual(before, Outcome(game) + AllHandsText(game));
    }

    private static string AllHandsText(Game game)
    {
        return string.Join(",", game.Players.SelectMany(player => player.HandCards).Select(card => card.Id));
    }

    [TestMethod]
    public void TheDeterminizedSearch_DoesntDependOnHiddenCards()
    {
        // two deals that differ only in what seat 0 can't see: the other hands and the later decks
        var deal = new Game(11) { Agents = { new HeuristicAI(), new HeuristicAI(), new HeuristicAI() } };
        deal.InitGame();
        List<int> wonders = deal.Players.Select(player => (int)player.Board.Id).ToList();
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
        Assert.AreEqual(SearchFromDeal(first, ":Tree=Duct"), SearchFromDeal(second, ":Tree=Duct"));
    }

    private static List<int> CardIds(IEnumerable<Card> cards)
    {
        return cards.Select(card => (int)card.Id).ToList();
    }

    // seat 0's first choice in a game dealt from the dataset deal
    private static Command SearchFromDeal(List<int>[] dataset, string settings)
    {
        var game = new Game(11) { Agents = { new HeuristicAI(), new HeuristicAI(), new HeuristicAI() } };
        game.InitGameNotRandom(dataset);
        return Mcts(":n=200" + settings).Choose(new GameView(game, 0));
    }

    [TestMethod]
    public void Settings_AreLoggedAndReadBack()
    {
        MctsAgent agent = Mcts(":n=50:backup=paranoid:C=1.2:eps=0.2");

        Assert.AreEqual(50, agent.Config.Iterations);
        Assert.AreEqual(MctsBackup.Paranoid, agent.Config.Backup);
        Assert.AreEqual(0.2, agent.Config.Epsilon);
        string json = ((IAgent)agent).ConfigJson();
        StringAssert.Contains(json, "\"Backup\":\"Paranoid\"");
        StringAssert.Contains(json, "\"C\":1.2");
        Assert.IsFalse(json.Contains("Searches") || json.Contains("_"), json);

        AgentSpec fromRow = AgentCatalog.FromResultRow("MctsAgent", json);
        Assert.AreEqual(json, AgentCatalog.Create(fromRow).ConfigJson());
        // every MctsConfig property is a setting, so equal settings JSON means an equal configuration
        Assert.AreEqual(((IAgent)new MctsAgent()).ConfigJson(), ((IAgent)Mcts()).ConfigJson(),
            "one agent's settings don't change the defaults");
    }

    [TestMethod]
    public void Stats_CountEveryIteration()
    {
        Game game = NewGame(9, Mcts(":n=25"), new HeuristicAI(), new HeuristicAI());
        game.Loop(record: true);

        var stats = JsonDocument.Parse(game.Results![0].StatsJson).RootElement;
        int searches = stats.GetProperty("searches").GetInt32();
        Assert.IsTrue(searches >= 15, game.Results[0].StatsJson);
        Assert.AreEqual(25L * searches, stats.GetProperty("iterations").GetInt64());
        Assert.AreEqual("{}", game.Results[1].StatsJson);
        Assert.IsTrue(stats.GetProperty("allocatedMb").GetDouble() > 0, "it measures the memory its searches allocate");
    }

    // Root and Leaf give every worker its copy and stream before any thread starts, so threads change nothing
    [DataTestMethod]
    [DataRow("Root")]
    [DataRow("Leaf")]
    public void RootAndLeaf_PlayTheSameWithOrWithoutThreads(string variant)
    {
        string withThreads = PlayFourWorkers(variant, "");
        Assert.AreEqual(withThreads, PlayFourWorkers(variant, ""));
        Assert.AreEqual(withThreads, PlayFourWorkers(variant, ":Threads=1"));
    }

    private static string PlayFourWorkers(string variant, string threads)
    {
        MctsAgent searcher = Mcts($":n=40:Parallel={variant}:Workers=4{threads}");
        Game game = NewGame(31, searcher, new HeuristicAI(), new HeuristicAI());
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
        MctsAgent searcher = Mcts($":n=40:Parallel={variant}:Workers={workers}");
        Game game = NewGame(9, searcher, new HeuristicAI(), new HeuristicAI());
        game.Loop(record: true);

        var stats = JsonDocument.Parse(game.Results![0].StatsJson).RootElement;
        int searches = stats.GetProperty("searches").GetInt32();
        Assert.AreEqual(40L * searches, stats.GetProperty("iterations").GetInt64(), game.Results[0].StatsJson);
        // one worker is the plain search, with the plain stats
        Assert.AreEqual(workers > 1, stats.TryGetProperty("workers", out _), game.Results[0].StatsJson);
    }

    [TestMethod]
    public void OneWorker_IsThePlainSearch()
    {
        string plain = PlayThirtyIterations("");
        Assert.AreEqual(plain, PlayThirtyIterations(":Parallel=Root:Workers=1"));
        Assert.AreEqual(plain, PlayThirtyIterations(":Parallel=Tree:Workers=1"));
    }

    private static string PlayThirtyIterations(string settings)
    {
        Game game = NewGame(44, Mcts(":n=30" + settings), new HeuristicAI(), new HeuristicAI());
        game.Loop();
        return Outcome(game);
    }

    [TestMethod]
    public void OnlyATimeBudget_UsesTheClock()
    {
        Assert.IsFalse(((IAgent)Mcts(":n=10")).UsesClock);
        Assert.IsTrue(((IAgent)Mcts(":Budget=Time:TimeMs=5")).UsesClock);
        Assert.IsFalse(((IAgent)new HeuristicAI()).UsesClock);
    }

    /// <summary>
    /// A sanity check (a sign error in the tree policy would fail it); strength is measured in experiments.
    /// </summary>
    [TestMethod]
    public void AgainstRandomPlayers_ItScoresMore()
    {
        var vp = new double[3];
        for (int g = 0; g < 4; g++)
        {
            Game game = NewGame(Seeding.GameSeed(21, g), Mcts(":n=30"), new RandomChoiceAI(), new RandomChoiceAI());
            game.Loop();
            for (int s = 0; s < 3; s++)
            {
                vp[s] += game.Players[s].VictoryPoints / 4.0;
            }
        }

        Assert.IsTrue(vp[0] > Math.Max(vp[1], vp[2]) + 5, $"average VP {string.Join(", ", vp)}");
    }
}
