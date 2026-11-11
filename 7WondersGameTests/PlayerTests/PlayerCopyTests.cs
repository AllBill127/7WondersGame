using System.Reflection;
using _7WondersGame.src.models;
using _7WondersGame.src.models.Agents;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Effects;

namespace _7WondersGameTests.PlayerTests
{
    /// <summary>
    /// <see cref="PlayerState.CopyFrom"/> (the heuristic's reusable copy) must copy what <see cref="PlayerState.Clone"/> copies.
    /// </summary>
    [TestClass]
    public class PlayerCopyTests
    {
        /// <summary>A game played some way into era 2 by heuristic players.</summary>
        private static Game MidGame()
        {
            var ai = new HeuristicAI();
            var game = new Game(Seeding.GameSeed(77, 0)) { Agents = Enumerable.Range(0, 4).Select(_ => (IAgent)new HeuristicAI()).ToList() };
            game.InitGame();
            while (game.Era < 2 || game.Turn % Game.TurnsPerEra < 3)
            {
                var moves = game.PendingPlayers().Select(s => (s, ai.Choose(game.Players[s], game.Phase, game.Era, game.DiscardedCards))).ToList();
                foreach (var (seat, move) in moves)
                    game.Submit(seat, move);
            }
            return game;
        }

        /// <summary>Everything about a player, as text.</summary>
        private static string Describe(PlayerState p) =>
            $"{p.Id} {p.Board.Id} {p.Board.Stage} | {string.Join(",", p.HandCards.Select(c => c.Id))} | {string.Join(",", p.PlayedCards.Select(c => c.Id))} | " +
            $"{p.VictoryTokens} {p.DefeatTokens} {p.VictoryPoints} {p.Abilities} {p.FreeCardOnce} {p.DiscardFree} | " +
            $"{string.Join(",", p.Resources.Select(r => $"{r.Key}={r.Value}"))} | {p.PlayerWest.Id} {p.PlayerEast.Id}";

        [TestMethod]
        public void EveryFieldIsKnown_SoCopyFromCopiesIt()
        {
            string[] fields = typeof(PlayerState)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

            CollectionAssert.AreEqual(new[]
            {
                "<Board>k__BackingField", "<HandCards>k__BackingField", "<Id>k__BackingField", "<PlayedCards>k__BackingField",
                "<PlayerEast>k__BackingField", "<PlayerWest>k__BackingField",
                "Abilities", "DefeatTokens", "DiscardFree", "FreeCardOnce", "Resources", "VictoryPoints", "VictoryTokens",
            }, fields, "PlayerState has a new field: copy it in PlayerState.CopyFrom too (and check Clone), then list it here.");
        }

        [TestMethod]
        public void CopyFrom_UndoesEveryChange_AsAFreshClone()
        {
            Game game = MidGame();
            foreach (PlayerState p in game.Players)
            {
                string before = Describe(p);
                PlayerState scratch = p.Clone();

                // change everything a try can change
                scratch.PlayedCards.Add(scratch.HandCards[0]);
                scratch.HandCards.RemoveAt(0);
                scratch.Board.AddStage(scratch);
                scratch.Resources[Resource.Coins] += 5;
                scratch.Resources[Resource.Shields] += 2;
                scratch.VictoryTokens += 3;
                scratch.DefeatTokens += 1;
                scratch.VictoryPoints = 99;
                scratch.Abilities |= Ability.ManufCheap;
                scratch.FreeCardOnce = !scratch.FreeCardOnce;
                scratch.DiscardFree = !scratch.DiscardFree;
                scratch.PlayerWest = scratch;

                scratch.CopyFrom(p);

                Assert.AreEqual(before, Describe(scratch), $"seat {p.Id}");
                Assert.AreEqual(before, Describe(p), "the source doesn't change");
                Assert.AreNotSame(p.HandCards, scratch.HandCards);
                Assert.AreNotSame(p.PlayedCards, scratch.PlayedCards);
                Assert.AreNotSame(p.Resources, scratch.Resources);
                Assert.AreNotSame(p.Board, scratch.Board);
            }
        }

        [TestMethod]
        public void CopyFrom_OnlyCopiesTheSameSeat()
        {
            Game game = MidGame();
            PlayerState scratch = game.Players[0].Clone();
            Assert.ThrowsException<ArgumentException>(() => scratch.CopyFrom(game.Players[1]));
        }
    }
}
