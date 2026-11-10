using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests.GameTests
{
    /// <summary>
    /// R5: the moves of one turn happen at the same time. Every move is priced against the turn-start state, so coins
    /// received this turn can't be spent this turn, and the order the seats are resolved in doesn't matter.
    /// </summary>
    [TestClass]
    public class SimultaneousResolutionTests
    {
        private static readonly Card Stockade = CardsDataset.GameCards[CardId.Stockade];   // 1 wood
        private static readonly Card Baths = CardsDataset.GameCards[CardId.Baths];         // 1 stone

        /// <summary>
        /// Three players, no resources or coins. Seat 0 builds Stockade with wood bought from seat 1 (its east);
        /// seat 1 builds Baths with stone bought from seat 2 (its east).
        /// </summary>
        private static Game TradingTurn(int seat0Coins, int seat1Coins)
        {
            Game game = new Game();
            game.Players = MockHelper.MockRandomChoiceAIPlayers(new List<Wonder> { new GizahA(), new GizahA(), new GizahA() });
            foreach (Player p in game.Players)
                p.Resources[Resource.Coins] = 0;
            game.Players[0].Resources[Resource.Coins] = seat0Coins;
            game.Players[1].Resources[Resource.Coins] = seat1Coins;
            game.Players[1].Resources[Resource.Wood] = 1;
            game.Players[2].Resources[Resource.Stone] = 1;
            game.Players[0].HandCards.Add(Stockade);
            game.Players[1].HandCards.Add(Baths);
            return game;
        }

        private static List<(Player, Command, List<Card>)> Moves(Game game, params int[] seats) =>
            seats.Select(s =>
            {
                Player p = game.Players[s];
                return (p, new Command(ActionType.Build, p.HandCards[0]), p.HandCards);
            }).ToList();

        private static int[] Coins(Game game) => game.Players.Select(p => p.Resources[Resource.Coins]).ToArray();

        [TestMethod]
        public void TradeIncomeCantBeSpentTheSameTurn()
        {
            // Seat 1 has no coins. Resolved seat by seat, seat 0's 2 coins would arrive first and pay for the stone.
            Game game = TradingTurn(seat0Coins: 2, seat1Coins: 0);

            Assert.ThrowsException<InvalidOperationException>(() => game.ResolveMoves(Moves(game, 0, 1)));

            // the turn is refused before anything changes
            CollectionAssert.AreEqual(new[] { 2, 0, 0 }, Coins(game));
            Assert.AreEqual(0, game.Players[0].PlayedCards.Count);
            CollectionAssert.Contains(game.Players[0].HandCards, Stockade);
        }

        [TestMethod]
        public void SubmittedMoves_AlsoRefuseIncomeSpentTheSameTurn()
        {
            Card lumberYard = CardsDataset.GameCards[CardId.LumberYard];

            // one seat at a time (Loop and the sequential tree): the turn resolves on the last move
            Game game = TradingTurn(seat0Coins: 2, seat1Coins: 0);
            game.Players[2].HandCards.Add(lumberYard);
            game.Submit(0, new Command(ActionType.Build, Stockade));
            game.Submit(1, new Command(ActionType.Build, Baths));
            Assert.ThrowsException<InvalidOperationException>(() => game.Submit(2, new Command(ActionType.Build, lumberYard)));

            // joint-move tree
            game = TradingTurn(seat0Coins: 2, seat1Coins: 0);
            game.Players[2].HandCards.Add(lumberYard);
            var joint = new List<Command>
            {
                new(ActionType.Build, Stockade), new(ActionType.Build, Baths), new(ActionType.Build, lumberYard),
            };
            Assert.ThrowsException<InvalidOperationException>(() => game.SubmitJointMove(joint));
        }

        [TestMethod]
        public void IncomeArrivesAfterEveryonePaid_InAnySeatOrder()
        {
            foreach (int[] order in new[] { new[] { 0, 1 }, new[] { 1, 0 } })
            {
                Game game = TradingTurn(seat0Coins: 2, seat1Coins: 2);
                game.ResolveMoves(Moves(game, order));

                // seat 0 paid 2 to seat 1, seat 1 paid 2 to seat 2
                CollectionAssert.AreEqual(new[] { 0, 2, 2 }, Coins(game), $"order {string.Join(",", order)}");
                CollectionAssert.Contains(game.Players[0].PlayedCards, Stockade);
                CollectionAssert.Contains(game.Players[1].PlayedCards, Baths);
            }
        }

        [TestMethod]
        public void VineyardCountsANeighboursBrownCardBuiltTheSameTurn_InAnySeatOrder()
        {
            foreach (int[] order in new[] { new[] { 0, 1 }, new[] { 1, 0 } })
            {
                Game game = new Game();
                game.Players = MockHelper.MockRandomChoiceAIPlayers(new List<Wonder> { new GizahA(), new GizahA(), new GizahA() });
                foreach (Player p in game.Players)
                    p.Resources[Resource.Coins] = 0;
                game.Players[0].HandCards.Add(CardsDataset.GameCards[CardId.Vineyard]);
                game.Players[1].HandCards.Add(CardsDataset.GameCards[CardId.LumberYard]);

                game.ResolveMoves(Moves(game, order));

                // 1 coin for seat 1's Lumber Yard, built at the same time
                Assert.AreEqual(1, game.Players[0].Resources[Resource.Coins], $"order {string.Join(",", order)}");
            }
        }
    }
}
