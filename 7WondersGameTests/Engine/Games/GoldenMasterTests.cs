using System.Runtime.CompilerServices;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Randomness.Services;

namespace _7WondersGameTests.Engine.Games;

/// <summary>
/// The golden master. 200 seeded games (40 for each player count, 3 to 7) played through
/// <see cref="Game.Loop"/> with the heuristic and random agents, and their final state, are stored in
/// GoldenMaster/golden-master.txt. A refactoring step must reproduce it exactly. A deliberate rules
/// change re-records it in its own commit:
///     set GOLDEN_MASTER_RECORD=1, run this test, unset it, and commit the new file.
/// </summary>
[TestClass]
public class GoldenMasterTests
{
    /// <summary>The environment variable that, set to 1, records the golden master instead of checking it.</summary>
    public const string RecordEnvVar = "GOLDEN_MASTER_RECORD";
    private const ulong RunSeed = 0x60_1D_E4_3A_57E2;
    private const int GamesPerPlayerCount = 40;

    // the agents the line-ups are made of: HeuristicAI and RandomChoiceAI
    private const int AgentKindCount = 2;

    private static string MasterPath([CallerFilePath] string thisFile = "")
    {
        return Path.Combine(Path.GetDirectoryName(thisFile)!, "GoldenMaster", "golden-master.txt");
    }

    [TestMethod]
    public void Games_MatchTheGoldenMaster()
    {
        List<string> actual = PlayAll();
        string path = MasterPath();

        if (Environment.GetEnvironmentVariable(RecordEnvVar) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Join("\n", actual) + "\n");
            Assert.Inconclusive($"Recorded {actual.Count} lines to {path}. Unset {RecordEnvVar} and commit the file.");
        }

        Assert.IsTrue(File.Exists(path), $"No golden master at {path}. Record it with {RecordEnvVar}=1.");
        string[] expected = File.ReadAllLines(path).Where(line => line.Length > 0).ToArray();

        for (int lineIndex = 0; lineIndex < Math.Min(expected.Length, actual.Count); lineIndex++)
        {
            if (expected[lineIndex] != actual[lineIndex])
            {
                Assert.Fail($"First difference at line {lineIndex + 1}:\n" +
                            $"expected: {expected[lineIndex]}\nactual:   {actual[lineIndex]}");
            }
        }

        Assert.AreEqual(expected.Length, actual.Count, "line count");
    }

    [TestMethod]
    public void Games_AreReproducible()
    {
        // the master is only useful if a game depends on nothing but its seed
        CollectionAssert.AreEqual(PlayGame(5, 3), PlayGame(5, 3));
    }

    private static List<string> PlayAll()
    {
        var lines = new List<string>();
        for (int players = 3; players <= 7; players++)
        {
            for (int gameIndex = 0; gameIndex < GamesPerPlayerCount; gameIndex++)
            {
                lines.AddRange(PlayGame(players, gameIndex));
            }
        }

        return lines;
    }

    /// <summary>One line per seat, then one for the discard pile.</summary>
    private static List<string> PlayGame(int players, int gameIndex)
    {
        ulong seed = Seeding.GameSeed(RunSeed, players * 1000 + gameIndex);
        var game = new Game(seed);

        // rotate the line-up so every agent plays every seat
        for (int seat = 0; seat < players; seat++)
        {
            int agentKind = (gameIndex + seat) % AgentKindCount;
            game.Agents.Add(NewAgent(agentKind));
        }

        game.InitGame();
        game.Loop(record: true);

        var lines = new List<string>();
        string prefix = $"p{players} g{gameIndex} seed={seed}";
        foreach (PlayerState player in game.Players)
        {
            lines.Add($"{prefix} seat={player.Id} {game.Agents[player.Id].Name} {player.Board.Id} " +
                      $"stage={player.Board.Stage} " +
                      $"vp={player.VictoryPoints} coins={player.Resources[Resource.Coins]} " +
                      $"military=+{player.VictoryTokens}/-{player.DefeatTokens} " +
                      $"built={string.Join(",", player.PlayedCards.Select(card => card.Id))}");
        }

        lines.Add($"{prefix} discarded={string.Join(",", game.DiscardedCards.Select(card => card.Id))}");
        return lines;
    }

    private static IAgent NewAgent(int agentKind)
    {
        switch (agentKind)
        {
            case 0:
                return new HeuristicAI();
            case 1:
                return new RandomChoiceAI();
            default:
                throw new ArgumentOutOfRangeException(nameof(agentKind));
        }
    }
}
