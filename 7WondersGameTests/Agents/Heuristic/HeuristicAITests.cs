using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Common.Settings;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Cli.Agents;
using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Games.Enums;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Randomness.Services;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGame.Engine.Wonders.Enums;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Agents.Heuristic;

/// <summary>The heuristic agent and its weights, and the mistakes the old heuristic made.</summary>
[TestClass]
public class HeuristicAITests
{
    private static Card C(CardId id)
    {
        return CardsDataset.GameCards[id];
    }

    /// <summary>Three players, seated in this order, each with its board's resource.</summary>
    private static List<PlayerState> Table(WonderId playerWonder, WonderId east, WonderId west)
    {
        // MockPlayers seats them in a ring: seat 0's east is seat 1, its west seat 2
        List<Wonder> wonders = new List<Wonder> { new Wonder(playerWonder), new Wonder(east), new Wonder(west) };
        List<PlayerState> players = MockHelper.MockPlayers(wonders);
        foreach (PlayerState player in players)
        {
            player.Resources[player.Board.Production] += 1;
        }

        return players;
    }

    /// <summary>
    /// Weights without denial and the era 1 civilian penalty, so a card's worth is just its points and bonuses.
    /// </summary>
    private static HeuristicAI Plain()
    {
        return new HeuristicAI { Weights = new HeuristicWeights { Denial = 0, CivilianEra1 = 0 } };
    }

    private static Command ChooseEra1(HeuristicAI agent, PlayerState player, params CardId[] hand)
    {
        player.HandCards.AddRange(hand.Select(C));
        return agent.Choose(player, Phase.ChooseMoves, 1, new List<Card>());
    }

    // =========== whole games ===========

    [DataTestMethod]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(7)]
    public void EveryChoice_IsLegal_AndChangesNothing(int players)
    {
        for (int gameIndex = 0; gameIndex < 3; gameIndex++)
        {
            var game = new Game(Seeding.GameSeed(77, players * 10 + gameIndex));
            for (int seat = 0; seat < players; seat++)
            {
                game.Agents.Add(new HeuristicAI());
            }

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
                {
                    game.Submit(seat, move);
                }
            }
        }
    }

    private static string Snapshot(Game game)
    {
        return string.Join(" | ", game.Players.Select(PlayerSnapshot)) + $" discarded={game.DiscardedCards.Count}";
    }

    private static string PlayerSnapshot(PlayerState player)
    {
        string resources = string.Join(",", player.Resources.Select(resource => resource.Value));
        string handCards = string.Join(",", player.HandCards.Select(card => card.Id));
        string playedCards = string.Join(",", player.PlayedCards.Select(card => card.Id));
        return $"{player.Id}: {resources} {player.Board.Stage} {player.Abilities} " +
               $"{player.FreeCardOnce} {player.DiscardFree} {player.VictoryTokens}/{player.DefeatTokens} " +
               $"[{handCards}] [{playedCards}]";
    }

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
        // every weight is a setting, so equal settings JSON means equal weights
        Assert.AreEqual(((IAgent)new HeuristicAI()).ConfigJson(), ((IAgent)fresh).ConfigJson());
        Assert.AreNotEqual(0.2, fresh.Weights.CoinValue);
    }

    // =========== the old heuristic's mistakes ===========

    [TestMethod]
    public void Marketplace_CountsEachNeighboursOwnBoard()
    {
        var weights = new HeuristicWeights();
        // Gizah makes stone; Alexandria glass and Halikarnassos loom, which the Marketplace makes cheaper
        PlayerState player = Table(WonderId.GizahA, WonderId.AlexandriaA, WonderId.HalikarnassosA)[0];
        PlayerState rawNeighbours = Table(WonderId.GizahA, WonderId.RhodosA, WonderId.OlympiaA)[0];

        double withGoods = new HeuristicValues(player, 1, weights).Gain(C(CardId.Marketplace));
        double withoutGoods = new HeuristicValues(rawNeighbours, 1, weights).Gain(C(CardId.Marketplace));

        Assert.AreEqual(weights.TradeDiscount + 2 * weights.TradeDiscountPerCard, withGoods);
        Assert.AreEqual(0, withoutGoods);
    }

    [TestMethod]
    public void OfTwoCardsWorthTheSame_TheCheaperIsBuilt()
    {
        // Baths and Pawnshop are both 3 points; Baths needs stone, bought from the east neighbour (Gizah)
        PlayerState player = Table(WonderId.RhodosA, WonderId.GizahA, WonderId.EphesosA)[0];

        Command move = ChooseEra1(Plain(), player, CardId.Baths, CardId.Pawnshop);

        Assert.AreEqual(new Command(ActionType.Build, C(CardId.Pawnshop)), move);
    }

    [TestMethod]
    public void AWonderStage_BuriesTheCardWorthLeast()
    {
        // Gizah A's first stage needs 2 stone, which the player has: 3 points, more than any card here
        PlayerState player = Table(WonderId.GizahA, WonderId.RhodosA, WonderId.EphesosA)[0];
        player.Resources[Resource.Stone] = 2;

        // Baths 3 points, Altar 2, Clay Pool nothing (Gizah A needs no clay)
        Command move = ChooseEra1(Plain(), player, CardId.Baths, CardId.Altar, CardId.ClayPool);

        Assert.AreEqual(new Command(ActionType.BuildWonder, C(CardId.ClayPool)), move);
    }

    [TestMethod]
    public void OlympiasFreeCard_IsKeptWhileACardCanBeBought()
    {
        PlayerState player = OlympiaWithFreeCard();

        // Baths needs stone, which nobody here makes: only the free card could build it
        Command move = ChooseEra1(Plain(), player, CardId.Altar, CardId.Baths, CardId.Theater);

        Assert.AreEqual(new Command(ActionType.Build, C(CardId.Altar)), move);
    }

    [TestMethod]
    public void OlympiasFreeCard_IsUsedOnTheErasLastChoice()
    {
        PlayerState player = OlympiaWithFreeCard();

        Command move = ChooseEra1(Plain(), player, CardId.Altar, CardId.Baths);

        Assert.AreEqual(new Command(ActionType.BuildFreeOnce, C(CardId.Baths)), move);
    }

    private static PlayerState OlympiaWithFreeCard()
    {
        PlayerState player = Table(WonderId.OlympiaA, WonderId.RhodosA, WonderId.EphesosA)[0];
        player.Board.Stage = 2;
        player.FreeCardOnce = true;
        return player;
    }

    [TestMethod]
    public void TheDiscardPileBuild_TakesTheCardWorthMost()
    {
        // the board doesn't matter for the choice; Gizah A needs no clay, so Clay Pool is worth nothing
        PlayerState player = Table(WonderId.GizahA, WonderId.RhodosA, WonderId.EphesosA)[0];
        var pile = new List<Card> { C(CardId.Altar), C(CardId.Pawnshop), C(CardId.ClayPool) };

        Command move = Plain().Choose(player, Phase.DiscardBuild, 1, pile);

        Assert.AreEqual(new Command(ActionType.Build, C(CardId.Pawnshop)), move);
    }
}
