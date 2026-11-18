using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGame.Engine.Wonders.Enums;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Engine.Games;

/// <summary>
/// The moves of one turn happen at the same time. Every move is priced against the turn-start state, so coins
/// received this turn can't be spent this turn, and the order the seats are resolved in doesn't matter.
/// </summary>
[TestClass]
public class SimultaneousResolutionTests
{
    private static readonly Card Stockade = CardsDataset.GameCards[CardId.Stockade];   // 1 wood
    private static readonly Card Baths = CardsDataset.GameCards[CardId.Baths];         // 1 stone

    /// <summary>Three Gizah A players without coins.</summary>
    private static Game GameWithoutCoins()
    {
        Game game = new Game(seed: 0);
        List<WonderBoardProgress> wonders = new List<WonderBoardProgress>
        {
            new WonderBoardProgress(WonderId.GizahA),
            new WonderBoardProgress(WonderId.GizahA),
            new WonderBoardProgress(WonderId.GizahA)
        };
        game.Players = MockHelper.MockPlayers(wonders);
        foreach (PlayerState player in game.Players)
        {
            player.Resources[Resource.Coins] = 0;
        }

        return game;
    }

    /// <summary>
    /// Three players, no resources or coins. Seat 0 builds Stockade with wood bought from seat 1 (its east);
    /// seat 1 builds Baths with stone bought from seat 2 (its east).
    /// </summary>
    private static Game TradingTurn(int seat0Coins, int seat1Coins)
    {
        Game game = GameWithoutCoins();
        game.Players[0].Resources[Resource.Coins] = seat0Coins;
        game.Players[1].Resources[Resource.Coins] = seat1Coins;
        game.Players[1].Resources[Resource.Wood] = 1;
        game.Players[2].Resources[Resource.Stone] = 1;
        game.Players[0].Hand.Add(Stockade);
        game.Players[1].Hand.Add(Baths);
        return game;
    }

    /// <summary>Each of the seats builds the first card of its hand, listed in the given seat order.</summary>
    private static List<(PlayerState, Move, List<Card>)> Moves(Game game, params int[] seats)
    {
        List<(PlayerState, Move, List<Card>)> moves = new List<(PlayerState, Move, List<Card>)>();
        foreach (int seat in seats)
        {
            PlayerState player = game.Players[seat];
            moves.Add((player, new Move(MoveAction.Build, player.Hand[0]), player.Hand));
        }

        return moves;
    }

    private static int[] Coins(Game game)
    {
        return game.Players.Select(player => player.Resources[Resource.Coins]).ToArray();
    }

    [TestMethod]
    public void TradeIncomeCantBeSpentTheSameTurn()
    {
        // Seat 1 has no coins. Resolved seat by seat, seat 0's 2 coins would arrive first and pay for the stone.
        Game game = TradingTurn(seat0Coins: 2, seat1Coins: 0);

        Assert.ThrowsException<InvalidOperationException>(() => game.ResolveMoves(Moves(game, 0, 1)));

        // the turn is refused before anything changes
        CollectionAssert.AreEqual(new[] { 2, 0, 0 }, Coins(game));
        Assert.AreEqual(0, game.Players[0].BuiltCards.Count);
        CollectionAssert.Contains(game.Players[0].Hand, Stockade);
    }

    [TestMethod]
    public void SubmittedMoves_AlsoRefuseIncomeSpentTheSameTurn()
    {
        Card lumberYard = CardsDataset.GameCards[CardId.LumberYard];

        // one seat at a time: the turn resolves on the last move
        Game game = TradingTurn(seat0Coins: 2, seat1Coins: 0);
        game.Players[2].Hand.Add(lumberYard);
        game.Submit(0, new Move(MoveAction.Build, Stockade));
        game.Submit(1, new Move(MoveAction.Build, Baths));
        Move lastMove = new Move(MoveAction.Build, lumberYard);
        Assert.ThrowsException<InvalidOperationException>(() => game.Submit(2, lastMove));
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
            CollectionAssert.Contains(game.Players[0].BuiltCards, Stockade);
            CollectionAssert.Contains(game.Players[1].BuiltCards, Baths);
        }
    }

    [TestMethod]
    public void VineyardCountsANeighboursBrownCardBuiltTheSameTurn_InAnySeatOrder()
    {
        foreach (int[] order in new[] { new[] { 0, 1 }, new[] { 1, 0 } })
        {
            Game game = GameWithoutCoins();
            game.Players[0].Hand.Add(CardsDataset.GameCards[CardId.Vineyard]);
            game.Players[1].Hand.Add(CardsDataset.GameCards[CardId.LumberYard]);

            game.ResolveMoves(Moves(game, order));

            // 1 coin for seat 1's Lumber Yard, built at the same time
            Assert.AreEqual(1, game.Players[0].Resources[Resource.Coins], $"order {string.Join(",", order)}");
        }
    }
}
