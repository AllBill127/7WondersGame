using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using _7WondersGame.src.models;

namespace _7WondersGameTests.PlayerTests
{
    [TestClass]
    public class CopyGuildTests
    {

        [TestMethod]
        public void CopyGuild_EffectActive_And_Single_GuildCopyable_ReturnsTrue_PlayerPlayedCardsContainGuild()
        {
            // Arrange
            List<Wonder> wonders = new List<Wonder>()
            {
                new Wonder(WonderId.GizahA),
                new Wonder(WonderId.OlympiaB),
                new Wonder(WonderId.BabylonA),
            };
            List<PlayerState> players = MockHelper.MockPlayers(wonders);

            // set Babylon A stage 3 for CopyGuild effect
            players[1].Board.Stage = 3;

            // set cards for situation
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Workers]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.LumberYard]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.ClayPit]);

            // Test
            bool copyGuildSuccess = players[1].CopyGuild();

            // Evaluate
            Assert.IsTrue(copyGuildSuccess);
            bool containsGuild = players[1].PlayedCards.Contains(CardsDataset.GameCards[CardId.Workers]);
            Assert.IsTrue(containsGuild);
            bool noCardChange = players[0].PlayedCards.Contains(CardsDataset.GameCards[CardId.Workers]);
            Assert.IsTrue(noCardChange);
        }

        [TestMethod]
        public void CopyGuild_EffectActive_And_Multiple_GuildsCopyable_ReturnsTrue_PlayerPlayedCardsContainBestGuild()
        {
            // Arrange
            List<Wonder> wonder = new List<Wonder>()
            {
                new Wonder(WonderId.GizahA),
                new Wonder(WonderId.OlympiaB),
                new Wonder(WonderId.BabylonA),
            };
            List<PlayerState> players = MockHelper.MockPlayers(wonder);

            // set Babylon A stage 3 for CopyGuild effect
            players[1].Board.Stage = 3;

            // set cards for situation
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Workers]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.ClayPit]);

            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.Spies]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Stables]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Walls]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Circus]);

            // Test
            bool copyGuildSuccess = players[1].CopyGuild();

            // Evaluate
            Assert.IsTrue(copyGuildSuccess);
            bool containsGuild = players[1].PlayedCards.Contains(CardsDataset.GameCards[CardId.Spies]);
            Assert.IsTrue(containsGuild);
            bool noCardChangeP1 = players[0].PlayedCards.Contains(CardsDataset.GameCards[CardId.Workers]);
            Assert.IsTrue(noCardChangeP1);
            bool noCardChangeP2 = players[2].PlayedCards.Contains(CardsDataset.GameCards[CardId.Spies]);
            Assert.IsTrue(noCardChangeP2);
        }

        [TestMethod]
        public void CopyGuild_EffectActive_And_No_GuildCopyable_ReturnsFalse_PlayerPlayedCardsDoesntContainGuild()
        {
            // Arrange
            List<Wonder> wonder = new List<Wonder>()
            {
                new Wonder(WonderId.GizahA),
                new Wonder(WonderId.OlympiaB),
                new Wonder(WonderId.BabylonA),
            };
            List<PlayerState> players = MockHelper.MockPlayers(wonder);

            // set Babylon A stage 3 for CopyGuild effect
            players[1].Board.Stage = 3;

            // Expected values
            int expP1PlayedCardsLen = players[1].PlayedCards.Count;

            // Test
            bool copyGuildSuccess = players[1].CopyGuild();

            // Evaluate
            Assert.IsFalse(copyGuildSuccess);
            Assert.AreEqual(expP1PlayedCardsLen, players[1].PlayedCards.Count);
        }

        // ===== R4: score candidates with the real CalculateScore() =====
        // Seats: players[1] is Olympia B; its west neighbour is players[0], its east neighbour players[2].

        private static List<PlayerState> OlympiaBWithStage3()
        {
            List<PlayerState> players = MockHelper.MockPlayers(
                new List<Wonder> { new Wonder(WonderId.GizahA), new Wonder(WonderId.OlympiaB), new Wonder(WonderId.BabylonA) });
            players[1].Board.Stage = 3;
            return players;
        }

        [TestMethod]
        public void CopyGuild_Craftsmens_CountsTwoVPPerGreyCard()
        {
            var players = OlympiaBWithStage3();
            // Workers: 3 brown cards next door = 3 VP. Craftsmens: 2 grey cards x 2 = 4 VP.
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Workers]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.Craftsmens]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.LumberYard]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.StonePit]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.ClayPool]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Loom]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.Press]);

            Assert.IsTrue(players[1].CopyGuild());

            Assert.IsTrue(players[1].PlayedCards.Contains(CardsDataset.GameCards[CardId.Craftsmens]));
            Assert.IsFalse(players[1].PlayedCards.Contains(CardsDataset.GameCards[CardId.Workers]));
            Assert.AreEqual(4, players[1].CalculateGuildScore());
        }

        [TestMethod]
        public void CopyGuild_CopiedGuild_IsCountedByOwnShipowners()
        {
            var players = OlympiaBWithStage3();
            players[1].PlayedCards.Add(CardsDataset.GameCards[CardId.Shipowners]);
            // Spies is worth 0 VP on its own, but copying it still adds 1 VP through Shipowners.
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.Spies]);

            Assert.IsTrue(players[1].CopyGuild());

            // Shipowners: 0 brown + 0 grey + 2 guilds (own Shipowners and the copied Spies). Spies: 0 military next door.
            Assert.IsTrue(players[1].PlayedCards.Contains(CardsDataset.GameCards[CardId.Spies]));
            Assert.AreEqual(2, players[1].CalculateGuildScore());
        }

        [TestMethod]
        public void CopyGuild_Scientists_GivesWildcard_WhenWorthMost()
        {
            var players = OlympiaBWithStage3();
            // Own science: 2 gears, 2 compasses, 1 tablet = 16 VP; with a wildcard tablet = 26 VP (+10).
            players[1].Resources[Resource.Gear] = 2;
            players[1].Resources[Resource.Compass] = 2;
            players[1].Resources[Resource.Tablet] = 1;
            // Workers is worth 2 VP.
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Workers]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.LumberYard]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.StonePit]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.Scientists]);

            Assert.IsTrue(players[1].CopyGuild());

            Assert.IsTrue(players[1].PlayedCards.Contains(CardsDataset.GameCards[CardId.Scientists]));
            Assert.AreEqual(1, players[1].ScienceWildcards());
            Assert.AreEqual(26, players[1].CalculateScientificScore());
        }

        [TestMethod]
        public void CopyGuild_NotChosenScientists_GivesNoWildcard()
        {
            var players = OlympiaBWithStage3();
            // No science symbols, so Scientists is worth 1 VP; Workers is worth 2 VP.
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Scientists]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.Workers]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.LumberYard]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.StonePit]);

            Assert.IsTrue(players[1].CopyGuild());

            Assert.IsTrue(players[1].PlayedCards.Contains(CardsDataset.GameCards[CardId.Workers]));
            Assert.AreEqual(0, players[1].ScienceWildcards());
        }

        [TestMethod]
        public void CopyGuild_Tie_GoesToFirstCandidate_WestFirst()
        {
            var players = OlympiaBWithStage3();
            // Both worth 1 VP: Workers (1 brown card next door) at west, Traders (1 yellow card next door) at east.
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Workers]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.Traders]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.LumberYard]);
            players[2].PlayedCards.Add(CardsDataset.GameCards[CardId.Tavern]);

            Assert.IsTrue(players[1].CopyGuild());

            Assert.IsTrue(players[1].PlayedCards.Contains(CardsDataset.GameCards[CardId.Workers]));
            Assert.IsFalse(players[1].PlayedCards.Contains(CardsDataset.GameCards[CardId.Traders]));
        }

        [TestMethod]
        public void CopyGuild_GuildAlreadyOwned_IsNotCopiedAgain()
        {
            var players = OlympiaBWithStage3();
            players[1].PlayedCards.Add(CardsDataset.GameCards[CardId.Workers]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Workers]);
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.LumberYard]);

            Assert.IsFalse(players[1].CopyGuild());
            Assert.AreEqual(1, players[1].PlayedCards.Count);
        }

        [TestMethod]
        public void CopyGuild_LeavesVictoryPointsUnchanged()
        {
            var players = OlympiaBWithStage3();
            players[1].VictoryPoints = 42;
            players[0].PlayedCards.Add(CardsDataset.GameCards[CardId.Workers]);

            Assert.IsTrue(players[1].CopyGuild());
            Assert.AreEqual(42, players[1].VictoryPoints);
        }
    }
}
