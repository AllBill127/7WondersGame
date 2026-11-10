using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests.PlayerTests
{
    /// <summary>The heuristic tries cards without building them: the player must be unchanged afterwards.</summary>
    [TestClass]
    public class HeuristicSideEffectTests
    {
        [DataTestMethod]
        [DataRow(CardId.EastTradingPost)]
        [DataRow(CardId.WestTradingPost)]
        [DataRow(CardId.Marketplace)]
        public void ScoreAfterPlayingCard_TradingCard_GivesNoDiscount(CardId id)
        {
            List<PlayerState> players = MockHelper.MockPlayers(new List<Wonder> { new HalikarnassosB(), new GizahA(), new GizahA() });
            PlayerState p = players[0];

            // in era 2 or 3 a Halikarnassos player can meet an era 1 trading card in the discard pile
            p.ScoreAfterPlayingCard(3, CardsDataset.GameCards[id]);

            Assert.IsFalse(p.RawCheapEast || p.RawCheapWest || p.ManufCheap);
        }
    }
}
