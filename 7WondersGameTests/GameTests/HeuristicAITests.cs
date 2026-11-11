using _7WondersGame.src.cli;
using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests.GameTests
{
    /// <summary>G: the new heuristic agent and its weights (the R10 fixes).</summary>
    [TestClass]
    public class HeuristicAITests
    {
        private static Card C(CardId id) => CardsDataset.GameCards[id];

        /// <summary>Three players, seated in this order, each with its board's resource.</summary>
        private static List<PlayerState> Table(WonderId me, WonderId east, WonderId west)
        {
            // MockPlayers seats them in a ring: seat 0's east is seat 1, its west seat 2
            List<PlayerState> players = MockHelper.MockPlayers(new List<Wonder> { new Wonder(me), new Wonder(east), new Wonder(west) });
            foreach (PlayerState p in players)
                p.Resources[p.Board.Production] += 1;
            return players;
        }

        /// <summary>Weights without denial and the era 1 civilian penalty, so a card's worth is just its points and bonuses.</summary>
        private static HeuristicAI Plain() => new() { Weights = new HeuristicWeights { Denial = 0, CivilianEra1 = 0 } };

        private static Command ChooseEra1(HeuristicAI agent, PlayerState me, params CardId[] hand)
        {
            me.HandCards.AddRange(hand.Select(C));
            return agent.Choose(me, Phase.ChooseMoves, 1, new List<Card>());
        }

        // =========== whole games ===========

        [DataTestMethod]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(7)]
        public void EveryChoice_IsLegal_AndChangesNothing(int players)
        {
            for (int g = 0; g < 3; g++)
            {
                var game = new Game(Seeding.GameSeed(77, players * 10 + g))
                {
                    Agents = Enumerable.Range(0, players).Select(_ => (IAgent)new HeuristicAI()).ToList(),
                };
                game.InitGame();

                while (game.Phase != Phase.GameOver)
                {
                    var moves = new List<(int Seat, Command Move)>();
                    foreach (int seat in game.PendingPlayers())
                    {
                        string before = Snapshot(game);
                        var agent = (HeuristicAI)game.Agents[seat];
                        Command fromView = agent.Choose(new GameView(game, seat));
                        // the same choice made on the game's own state must not change it
                        Command fromState = agent.Choose(game.Players[seat], game.Phase, game.Era, game.DiscardedCards);

                        Assert.AreEqual(fromView, fromState, $"seat {seat}, turn {game.Turn}");
                        Assert.AreEqual(before, Snapshot(game), $"seat {seat}, turn {game.Turn}");
                        CollectionAssert.Contains(game.LegalMoves(seat), fromView, $"seat {seat}, turn {game.Turn}");
                        moves.Add((seat, fromView));
                    }
                    foreach (var (seat, move) in moves)
                        game.Submit(seat, move);
                }
            }
        }

        private static string Snapshot(Game game) =>
            string.Join(" | ", game.Players.Select(p =>
                $"{p.Id}: {string.Join(",", p.Resources.Select(r => r.Value))} {p.Board.Stage} {p.Abilities} " +
                $"{p.FreeCardOnce} {p.DiscardFree} {p.VictoryTokens}/{p.DefeatTokens} " +
                $"[{string.Join(",", p.HandCards.Select(c => c.Id))}] [{string.Join(",", p.PlayedCards.Select(c => c.Id))}]"))
            + $" discarded={game.DiscardedCards.Count}";

        // =========== settings ===========

        [TestMethod]
        public void Weights_AreSettings_LoggedAndReadBack()
        {
            var agent = (HeuristicAI)AgentCatalog.Create(AgentSpec.Parse("HeuristicAI:CoinValue=0.2:denial=0"));

            Assert.AreEqual(0.2, agent.Weights.CoinValue);
            Assert.AreEqual(0.0, agent.Weights.Denial);
            StringAssert.Contains(((IAgent)agent).ConfigJson(), "\"CoinValue\":0.2");

            // a results row rebuilds the same agent
            AgentSpec fromRow = AgentCatalog.FromResultRow("HeuristicAI", ((IAgent)agent).ConfigJson());
            Assert.AreEqual(((IAgent)agent).ConfigJson(), AgentCatalog.Create(fromRow).ConfigJson());
        }

        [TestMethod]
        public void SettingOneAgentsWeights_LeavesTheDefaultsAlone()
        {
            AgentCatalog.Create(AgentSpec.Parse("HeuristicAI:CoinValue=0.2"));

            var fresh = (HeuristicAI)AgentCatalog.Create(AgentSpec.Parse("HeuristicAI"));
            Assert.AreEqual(new HeuristicWeights(), fresh.Weights);
            Assert.AreNotEqual(0.2, fresh.Weights.CoinValue);
        }

        // =========== R10 fixes ===========

        [TestMethod]
        public void Marketplace_CountsEachNeighboursOwnBoard()
        {
            var weights = new HeuristicWeights();
            // Gizah makes stone; Alexandria glass and Halikarnassos loom, which the Marketplace makes cheaper
            PlayerState me = Table(WonderId.GizahA, WonderId.AlexandriaA, WonderId.HalikarnassosA)[0];
            PlayerState rawNeighbours = Table(WonderId.GizahA, WonderId.RhodosA, WonderId.OlympiaA)[0];

            double withGoods = new HeuristicValues(me, 1, weights).Gain(C(CardId.Marketplace));
            double withoutGoods = new HeuristicValues(rawNeighbours, 1, weights).Gain(C(CardId.Marketplace));

            Assert.AreEqual(weights.TradeDiscount + 2 * weights.TradeDiscountPerCard, withGoods);
            Assert.AreEqual(0, withoutGoods);
        }

        [TestMethod]
        public void OfTwoCardsWorthTheSame_TheCheaperIsBuilt()
        {
            // Baths and Pawnshop are both 3 points; Baths needs stone, bought from the east neighbour (Gizah)
            PlayerState me = Table(WonderId.RhodosA, WonderId.GizahA, WonderId.EphesosA)[0];

            Command move = ChooseEra1(Plain(), me, CardId.Baths, CardId.Pawnshop);

            Assert.AreEqual(new Command(ActionType.Build, C(CardId.Pawnshop)), move);
        }

        [TestMethod]
        public void AWonderStage_BuriesTheCardWorthLeast()
        {
            // Gizah A's first stage needs 2 stone, which the player has: 3 points, more than any card here
            PlayerState me = Table(WonderId.GizahA, WonderId.RhodosA, WonderId.EphesosA)[0];
            me.Resources[Resource.Stone] = 2;

            // Baths 3 points, Altar 2, Clay Pool nothing (Gizah A needs no clay)
            Command move = ChooseEra1(Plain(), me, CardId.Baths, CardId.Altar, CardId.ClayPool);

            Assert.AreEqual(new Command(ActionType.BuildWonder, C(CardId.ClayPool)), move);
        }

        [TestMethod]
        public void OlympiasFreeCard_IsKeptWhileACardCanBeBought()
        {
            PlayerState me = OlympiaWithFreeCard();

            // Baths needs stone, which nobody here makes: only the free card could build it
            Command move = ChooseEra1(Plain(), me, CardId.Altar, CardId.Baths, CardId.Theater);

            Assert.AreEqual(new Command(ActionType.Build, C(CardId.Altar)), move);
        }

        [TestMethod]
        public void OlympiasFreeCard_IsUsedOnTheErasLastChoice()
        {
            PlayerState me = OlympiaWithFreeCard();

            Command move = ChooseEra1(Plain(), me, CardId.Altar, CardId.Baths);

            Assert.AreEqual(new Command(ActionType.BuildFreeOnce, C(CardId.Baths)), move);
        }

        private static PlayerState OlympiaWithFreeCard()
        {
            PlayerState me = Table(WonderId.OlympiaA, WonderId.RhodosA, WonderId.EphesosA)[0];
            me.Board.Stage = 2;
            me.FreeCardOnce = true;
            return me;
        }

        [TestMethod]
        public void TheDiscardPileBuild_TakesTheCardWorthMost()
        {
            // the board doesn't matter for the choice; Gizah A needs no clay, so Clay Pool is worth nothing
            PlayerState me = Table(WonderId.GizahA, WonderId.RhodosA, WonderId.EphesosA)[0];
            var pile = new List<Card> { C(CardId.Altar), C(CardId.Pawnshop), C(CardId.ClayPool) };

            Command move = Plain().Choose(me, Phase.DiscardBuild, 1, pile);

            Assert.AreEqual(new Command(ActionType.Build, C(CardId.Pawnshop)), move);
        }
    }
}
