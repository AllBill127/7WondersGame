using _7Wonders.Agents.Common.Entities;
using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Agents.Common.Settings;
using _7Wonders.Agents.Heuristic;
using _7Wonders.Cli.Agents;
using _7Wonders.Engine.Cards.Data;
using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Games.Enums;
using _7Wonders.Engine.Moves.Dtos;
using _7Wonders.Engine.Moves.Enums;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Randomness.Services;
using _7Wonders.Engine.Wonders.Entities;
using _7Wonders.Engine.Wonders.Enums;
using _7Wonders.Tests.TestHelpers;

namespace _7Wonders.Tests.Agents.Heuristic;

/// <summary>The heuristic agent and its weights, and the mistakes the old heuristic made.</summary>
[TestClass]
public class HeuristicAgentTests
{
    private static Card CardOf(CardId id)
    {
        return CardsDataset.GameCards[id];
    }

    /// <summary>Three players, seated in this order, each with its board's resource.</summary>
    private static List<PlayerState> Table(WonderId playerWonder, WonderId east, WonderId west)
    {
        // MockPlayers seats them in a ring: seat 0's east is seat 1, its west seat 2
        List<WonderBoardProgress> wonders = new()
        {
            new WonderBoardProgress(playerWonder),
            new WonderBoardProgress(east),
            new WonderBoardProgress(west),
        };
        List<PlayerState> players = MockHelper.MockPlayers(wonders);
        foreach (PlayerState player in players)
        {
            player.Resources[player.Wonder.Production] += 1;
        }

        return players;
    }

    /// <summary>
    /// Weights without denial and the era 1 civilian penalty, so a card's worth is just its points and bonuses.
    /// </summary>
    private static HeuristicAgent Plain()
    {
        HeuristicWeights weights = new() { NextPlayerDenialWeight = 0, CivilianCardBonusEra1 = 0 };
        return new HeuristicAgent { Weights = weights };
    }

    private static Move ChooseEra1(HeuristicAgent agent, PlayerState player, params CardId[] hand)
    {
        player.Hand.AddRange(hand.Select(CardOf));
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
            Game game = new(Seeding.GameSeed(77, players * 10 + gameIndex));
            game.SetUpRandomGame(players);
            HeuristicAgent agent = new();

            while (game.Phase != Phase.GameOver)
            {
                Dictionary<int, Move> movesBySeat = new();
                foreach (int seat in game.PendingPlayers())
                {
                    string before = Snapshot(game);
                    Move fromView = agent.Choose(new GameView(game, seat));
                    // the same choice made on the game's own state must not change it
                    Move fromState = agent.Choose(game.Players[seat], game.Phase, game.Era, game.DiscardPile);

                    Assert.AreEqual(fromView, fromState, $"seat {seat}, turn {game.Turn}");
                    Assert.AreEqual(before, Snapshot(game), $"seat {seat}, turn {game.Turn}");
                    CollectionAssert.Contains(game.LegalMoves(seat), fromView, $"seat {seat}, turn {game.Turn}");
                    movesBySeat.Add(seat, fromView);
                }

                foreach (KeyValuePair<int, Move> seatMove in movesBySeat)
                {
                    game.Submit(seatMove.Key, seatMove.Value);
                }
            }
        }
    }

    private static string Snapshot(Game game)
    {
        return string.Join(" | ", game.Players.Select(PlayerSnapshot)) + $" discarded={game.DiscardPile.Count}";
    }

    private static string PlayerSnapshot(PlayerState player)
    {
        string resources = string.Join(",", player.Resources.Select(resource => resource.Value));
        string hand = string.Join(",", player.Hand.Select(card => card.Id));
        string builtCards = string.Join(",", player.BuiltCards.Select(card => card.Id));
        return $"{player.Seat}: {resources} {player.Wonder.BuiltStageCount} {player.Abilities} " +
               $"{player.OlympiaFreeBuildAvailable} {player.HalikarnassosDiscardBuildPending} " +
               $"{player.VictoryTokens}/{player.DefeatTokens} " +
               $"[{hand}] [{builtCards}]";
    }

    // =========== settings ===========

    [TestMethod]
    public void Weights_AreSettings_LoggedAndReadBack()
    {
        AgentSpecification specification =
            AgentSpecification.Parse("HeuristicAgent:CoinValueInVictoryPoints=0.2:NextPlayerDenialWeight=0");
        HeuristicAgent agent = (HeuristicAgent)AgentCatalog.Create(specification);

        Assert.AreEqual(0.2, agent.Weights.CoinValueInVictoryPoints);
        Assert.AreEqual(0.0, agent.Weights.NextPlayerDenialWeight);
        StringAssert.Contains(((IAgent)agent).ConfigurationJson(), "\"CoinValueInVictoryPoints\":0.2");

        // a results row rebuilds the same agent
        AgentSpecification fromRow = AgentCatalog.FromResultRow("HeuristicAgent", ((IAgent)agent).ConfigurationJson());
        Assert.AreEqual(((IAgent)agent).ConfigurationJson(), AgentCatalog.Create(fromRow).ConfigurationJson());
    }

    [TestMethod]
    public void SettingOneAgentsWeights_LeavesTheDefaultsAlone()
    {
        AgentCatalog.Create(AgentSpecification.Parse("HeuristicAgent:CoinValueInVictoryPoints=0.2"));

        HeuristicAgent fresh = (HeuristicAgent)AgentCatalog.Create(AgentSpecification.Parse("HeuristicAgent"));
        // every weight is a setting, so equal settings JSON means equal weights
        Assert.AreEqual(((IAgent)new HeuristicAgent()).ConfigurationJson(), ((IAgent)fresh).ConfigurationJson());
        Assert.AreNotEqual(0.2, fresh.Weights.CoinValueInVictoryPoints);
    }

    // =========== the old heuristic's mistakes ===========

    [TestMethod]
    public void Marketplace_CountsEachNeighboursOwnBoard()
    {
        HeuristicWeights weights = new();
        // Gizah makes stone; Alexandria glass and Halikarnassos loom, which the Marketplace makes cheaper
        PlayerState player = Table(WonderId.GizahA, WonderId.AlexandriaA, WonderId.HalikarnassosA)[0];
        PlayerState rawNeighbours = Table(WonderId.GizahA, WonderId.RhodosA, WonderId.OlympiaA)[0];

        double withGoods = new HeuristicValues(player, 1, weights).Gain(CardOf(CardId.Marketplace));
        double withoutGoods = new HeuristicValues(rawNeighbours, 1, weights).Gain(CardOf(CardId.Marketplace));

        Assert.AreEqual(weights.TradingDiscountBonus + 2 * weights.TradingDiscountBonusPerNeighbourCard, withGoods);
        Assert.AreEqual(0, withoutGoods);
    }

    [TestMethod]
    public void OfTwoCardsWorthTheSame_TheCheaperIsBuilt()
    {
        // Baths and Pawnshop are both 3 points; Baths needs stone, bought from the east neighbour (Gizah)
        PlayerState player = Table(WonderId.RhodosA, WonderId.GizahA, WonderId.EphesosA)[0];

        Move move = ChooseEra1(Plain(), player, CardId.Baths, CardId.Pawnshop);

        Assert.AreEqual(new Move(MoveAction.Build, CardOf(CardId.Pawnshop)), move);
    }

    [TestMethod]
    public void AWonderStage_BuriesTheCardWorthLeast()
    {
        // Gizah A's first stage needs 2 stone, which the player has: 3 points, more than any card here
        PlayerState player = Table(WonderId.GizahA, WonderId.RhodosA, WonderId.EphesosA)[0];
        player.Resources[Resource.Stone] = 2;

        // Baths 3 points, Altar 2, Clay Pool nothing (Gizah A needs no clay)
        Move move = ChooseEra1(Plain(), player, CardId.Baths, CardId.Altar, CardId.ClayPool);

        Assert.AreEqual(new Move(MoveAction.BuildWonderStage, CardOf(CardId.ClayPool)), move);
    }

    [TestMethod]
    public void OlympiasFreeCard_IsKeptWhileACardCanBeBought()
    {
        PlayerState player = OlympiaWithFreeCard();

        // Baths needs stone, which nobody here makes: only the free card could build it
        Move move = ChooseEra1(Plain(), player, CardId.Altar, CardId.Baths, CardId.Theater);

        Assert.AreEqual(new Move(MoveAction.Build, CardOf(CardId.Altar)), move);
    }

    [TestMethod]
    public void OlympiasFreeCard_IsUsedOnTheErasLastChoice()
    {
        PlayerState player = OlympiaWithFreeCard();

        Move move = ChooseEra1(Plain(), player, CardId.Altar, CardId.Baths);

        Assert.AreEqual(new Move(MoveAction.BuildWithOlympiaFreeBuild, CardOf(CardId.Baths)), move);
    }

    private static PlayerState OlympiaWithFreeCard()
    {
        PlayerState player = Table(WonderId.OlympiaA, WonderId.RhodosA, WonderId.EphesosA)[0];
        player.Wonder.BuiltStageCount = 2;
        player.OlympiaFreeBuildAvailable = true;
        return player;
    }

    [TestMethod]
    public void TheDiscardPileBuild_TakesTheCardWorthMost()
    {
        // the board doesn't matter for the choice; Gizah A needs no clay, so Clay Pool is worth nothing
        PlayerState player = Table(WonderId.GizahA, WonderId.RhodosA, WonderId.EphesosA)[0];
        List<Card> pile = new() { CardOf(CardId.Altar), CardOf(CardId.Pawnshop), CardOf(CardId.ClayPool) };

        Move move = Plain().Choose(player, Phase.DiscardPileBuild, 1, pile);

        Assert.AreEqual(new Move(MoveAction.Build, CardOf(CardId.Pawnshop)), move);
    }
}
