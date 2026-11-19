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
        HeuristicAgent heuristicAgent = new();
        Game game = new(Seeding.GameSeed(77, 0));
        for (int seat = 0; seat < 4; seat++)
        {
            game.Agents.Add(new HeuristicAgent());
        }

        game.SetUpRandomGame();
        while (game.Era < 2 || game.Turn % Game.TurnsPerEra < 3)
        {
            // every pending player chooses from the same state, then all moves are submitted
            List<int> pendingSeats = new(game.PendingPlayers());
            List<Move> moves = new();
            foreach (int seat in pendingSeats)
            {
                moves.Add(heuristicAgent.Choose(game.Players[seat], game.Phase, game.Era, game.DiscardPile));
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
        string handCards = string.Join(",", player.Hand.Select(card => card.Id));
        string playedCards = string.Join(",", player.BuiltCards.Select(card => card.Id));
        string resources = string.Join(",", player.Resources.Select(resource => $"{resource.Key}={resource.Value}"));
        return $"{player.Seat} {player.Wonder.Id} {player.Wonder.BuiltStageCount} | {handCards} | {playedCards} | " +
               $"{player.VictoryTokens} {player.DefeatTokens} {player.VictoryPoints} " +
               $"{player.Abilities} {player.OlympiaFreeBuildAvailable} {player.HalikarnassosDiscardBuildPending} | " +
               $"{resources} | {player.WestNeighbour.Seat} {player.EastNeighbour.Seat}";
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
            "<BuiltCards>k__BackingField",
            "<EastNeighbour>k__BackingField",
            "<Hand>k__BackingField",
            "<Seat>k__BackingField",
            "<WestNeighbour>k__BackingField",
            "<Wonder>k__BackingField",
            "Abilities",
            "DefeatTokens",
            "HalikarnassosDiscardBuildPending",
            "OlympiaFreeBuildAvailable",
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
            scratch.BuiltCards.Add(scratch.Hand[0]);
            scratch.Hand.RemoveAt(0);
            scratch.Wonder.AddStage(scratch);
            scratch.Resources[Resource.Coins] += 5;
            scratch.Resources[Resource.Shields] += 2;
            scratch.VictoryTokens += 3;
            scratch.DefeatTokens += 1;
            scratch.VictoryPoints = 99;
            scratch.Abilities |= Ability.ManufacturedGoodsCheapFromBothNeighbours;
            scratch.OlympiaFreeBuildAvailable = !scratch.OlympiaFreeBuildAvailable;
            scratch.HalikarnassosDiscardBuildPending = !scratch.HalikarnassosDiscardBuildPending;
            scratch.WestNeighbour = scratch;

            scratch.CopyFrom(player);

            Assert.AreEqual(before, Describe(scratch), $"seat {player.Seat}");
            Assert.AreEqual(before, Describe(player), "the source doesn't change");
            Assert.AreNotSame(player.Hand, scratch.Hand);
            Assert.AreNotSame(player.BuiltCards, scratch.BuiltCards);
            Assert.AreNotSame(player.Resources, scratch.Resources);
            Assert.AreNotSame(player.Wonder, scratch.Wonder);
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
