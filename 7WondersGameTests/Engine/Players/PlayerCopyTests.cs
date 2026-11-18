using System.Reflection;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Randomness.Services;

namespace _7WondersGameTests.Engine.Players;

/// <summary>
/// <see cref="PlayerState.CopyFrom"/> (the heuristic's reusable copy) must copy what
/// <see cref="PlayerState.Clone"/> copies.
/// </summary>
[TestClass]
public class PlayerCopyTests
{
    /// <summary>A game played some way into era 2 by heuristic players.</summary>
    private static Game MidGame()
    {
        var heuristicAgent = new HeuristicAI();
        var game = new Game(Seeding.GameSeed(77, 0));
        for (int seat = 0; seat < 4; seat++)
        {
            game.Agents.Add(new HeuristicAI());
        }

        game.InitGame();
        while (game.Era < 2 || game.Turn % Game.TurnsPerEra < 3)
        {
            // every pending player chooses from the same state, then all moves are submitted
            List<int> pendingSeats = new List<int>(game.PendingPlayers());
            List<Command> moves = new List<Command>();
            foreach (int seat in pendingSeats)
            {
                moves.Add(heuristicAgent.Choose(game.Players[seat], game.Phase, game.Era, game.DiscardedCards));
            }

            for (int pendingIndex = 0; pendingIndex < pendingSeats.Count; pendingIndex++)
            {
                game.Submit(pendingSeats[pendingIndex], moves[pendingIndex]);
            }
        }

        return game;
    }

    /// <summary>Everything about a player, as text.</summary>
    private static string Describe(PlayerState player)
    {
        string handCards = string.Join(",", player.HandCards.Select(card => card.Id));
        string playedCards = string.Join(",", player.PlayedCards.Select(card => card.Id));
        string resources = string.Join(",", player.Resources.Select(resource => $"{resource.Key}={resource.Value}"));
        return $"{player.Id} {player.Board.Id} {player.Board.Stage} | {handCards} | {playedCards} | " +
               $"{player.VictoryTokens} {player.DefeatTokens} {player.VictoryPoints} " +
               $"{player.Abilities} {player.FreeCardOnce} {player.DiscardFree} | " +
               $"{resources} | {player.PlayerWest.Id} {player.PlayerEast.Id}";
    }

    [TestMethod]
    public void EveryFieldIsKnown_SoCopyFromCopiesIt()
    {
        string[] fields = typeof(PlayerState)
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(field => field.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        string[] knownFields =
        {
            "<Board>k__BackingField",
            "<HandCards>k__BackingField",
            "<Id>k__BackingField",
            "<PlayedCards>k__BackingField",
            "<PlayerEast>k__BackingField",
            "<PlayerWest>k__BackingField",
            "Abilities",
            "DefeatTokens",
            "DiscardFree",
            "FreeCardOnce",
            "Resources",
            "VictoryPoints",
            "VictoryTokens",
        };
        CollectionAssert.AreEqual(
            knownFields,
            fields,
            "PlayerState has a new field: copy it in PlayerState.CopyFrom too (and check Clone), then list it here.");
    }

    [TestMethod]
    public void CopyFrom_UndoesEveryChange_AsAFreshClone()
    {
        Game game = MidGame();
        foreach (PlayerState player in game.Players)
        {
            string before = Describe(player);
            PlayerState scratch = player.Clone();

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

            scratch.CopyFrom(player);

            Assert.AreEqual(before, Describe(scratch), $"seat {player.Id}");
            Assert.AreEqual(before, Describe(player), "the source doesn't change");
            Assert.AreNotSame(player.HandCards, scratch.HandCards);
            Assert.AreNotSame(player.PlayedCards, scratch.PlayedCards);
            Assert.AreNotSame(player.Resources, scratch.Resources);
            Assert.AreNotSame(player.Board, scratch.Board);
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
