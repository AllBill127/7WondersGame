using _7WondersGame.src.cli;
using _7WondersGame.src.models;
using _7WondersGame.src.models.Agents;
using _7WondersGame.src.models.Agents.Mcts;
using _7WondersGame.src.models.Players;

namespace _7WondersGameTests.GameTests
{
    /// <summary>Step 6 (E + D1): the configurable MCTS agent.</summary>
    [TestClass]
    public class MctsAgentTests
    {
        private static MctsAgent Mcts(string settings = "") =>
            (MctsAgent)AgentCatalog.Create(AgentSpec.Parse("MctsAgent" + settings));

        private static Game NewGame(ulong seed, params IAgent[] agents)
        {
            var game = new Game(seed) { Agents = agents.ToList() };
            game.InitGame();
            return game;
        }

        private static string Outcome(Game game) =>
            string.Join(" | ", game.Players.Select(p => $"{p.VictoryPoints} {string.Join(",", p.PlayedCards.Select(c => c.Id))}"));

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
        public void EverySetting_PlaysWholeGames(string settings)
        {
            foreach (int players in new[] { 3, 4 })
            {
                IAgent[] agents = Enumerable.Range(0, players)
                    .Select(s => s == 1 ? Mcts(":n=10" + settings) : (IAgent)new HeuristicAI()).ToArray();
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
            string Play()
            {
                Game game = NewGame(77, new HeuristicAI(), Mcts(":n=15"), Mcts(":n=15:Tree=Duct"));
                game.Loop();
                return Outcome(game);
            }
            Assert.AreEqual(Play(), Play());
        }

        [TestMethod]
        public void ASearch_ChangesNothing()
        {
            Game game = NewGame(3, new HeuristicAI(), new HeuristicAI(), new HeuristicAI());
            string before = Outcome(game) + string.Join(",", game.Players.SelectMany(p => p.HandCards).Select(c => c.Id));

            Command move = Mcts(":n=50").Choose(new GameView(game, 0));

            CollectionAssert.Contains(game.LegalMoves(0), move);
            Assert.AreEqual(before, Outcome(game) + string.Join(",", game.Players.SelectMany(p => p.HandCards).Select(c => c.Id)));
        }

        [TestMethod]
        public void TheDeterminizedSearch_DoesntDependOnHiddenCards()
        {
            // two deals that differ only in what seat 0 can't see: the other hands and the later decks
            var deal = new Game(11) { Agents = { new HeuristicAI(), new HeuristicAI(), new HeuristicAI() } };
            deal.InitGame();
            List<int> Ids(IEnumerable<Card> cards) => cards.Select(c => (int)c.Id).ToList();
            List<int> wonders = deal.Players.Select(p => (int)p.Board.Id).ToList();
            List<int> era1 = Ids(deal.Decks[0]);
            List<int> swapped = era1.Take(7).Concat(era1.Skip(14).Take(7)).Concat(era1.Skip(7).Take(7)).ToList();
            var first = new[] { era1, Ids(deal.Decks[1]), Ids(deal.Decks[2]), wonders };
            var second = new[] { swapped, Ids(deal.Decks[1].AsEnumerable().Reverse()), Ids(deal.Decks[2].AsEnumerable().Reverse()), wonders };

            Command Search(List<int>[] dataset, string settings)
            {
                var game = new Game(11) { Agents = { new HeuristicAI(), new HeuristicAI(), new HeuristicAI() } };
                game.InitGameNotRandom(dataset);
                return Mcts(":n=200" + settings).Choose(new GameView(game, 0));
            }

            Assert.AreEqual(Search(first, ""), Search(second, ""));
            Assert.AreEqual(Search(first, ":Tree=Duct"), Search(second, ":Tree=Duct"));
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
            Assert.AreEqual(new MctsConfig(), Mcts().Config, "one agent's settings don't change the defaults");
        }

        [TestMethod]
        public void Stats_CountEveryIteration()
        {
            Game game = NewGame(9, Mcts(":n=25"), new HeuristicAI(), new HeuristicAI());
            game.Loop(record: true);

            var stats = System.Text.Json.JsonDocument.Parse(game.Results![0].StatsJson).RootElement;
            int searches = stats.GetProperty("searches").GetInt32();
            Assert.IsTrue(searches >= 15, game.Results[0].StatsJson);
            Assert.AreEqual(25L * searches, stats.GetProperty("iterations").GetInt64());
            Assert.AreEqual("{}", game.Results[1].StatsJson);
        }

        /// <summary>A sanity check (a sign error in the tree policy would fail it); strength is measured in experiments.</summary>
        [TestMethod]
        public void AgainstRandomPlayers_ItScoresMore()
        {
            var vp = new double[3];
            for (int g = 0; g < 4; g++)
            {
                Game game = NewGame(Seeding.GameSeed(21, g), Mcts(":n=30"), new RandomChoiceAI(), new RandomChoiceAI());
                game.Loop();
                for (int s = 0; s < 3; s++)
                    vp[s] += game.Players[s].VictoryPoints / 4.0;
            }
            Assert.IsTrue(vp[0] > Math.Max(vp[1], vp[2]) + 5, $"average VP {string.Join(", ", vp)}");
        }
    }
}
