using System.Runtime.CompilerServices;
using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Agents.Heuristic;
using _7Wonders.Agents.Matches;
using _7Wonders.Agents.RandomChoice;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Randomness.Services;

namespace _7Wonders.Tests.Engine.Games;

/// <summary>
/// The golden master. 200 seeded games (40 for each player count, 3 to 7) played through
/// <see cref="Match.Play"/> with the heuristic and random agents, and their final state, are stored in
/// GoldenMaster/golden-master.txt. A refactoring step must reproduce it exactly. A deliberate rules
/// change re-records it in its own commit:
///     set GOLDEN_MASTER_RECORD=1, run this test, unset it, and commit the new file.
/// </summary>
[TestClass]
public class GoldenMasterTests
{
    /// <summary>The environment variable that, set to 1, records the golden master instead of checking it.</summary>
    public const string RecordEnvironmentVariable = "GOLDEN_MASTER_RECORD";
    private const ulong RunSeed = 0x60_1D_E4_3A_57E2;
    private const int GamesPerPlayerCount = 40;

    // the agents the line-ups are made of: HeuristicAgent and RandomAgent
    private const int AgentKindCount = 2;

    private static string MasterPath([CallerFilePath] string thisFile = "")
    {
        string? testFolder = Path.GetDirectoryName(thisFile);
        Assert.IsNotNull(testFolder);
        return Path.Combine(testFolder, "GoldenMaster", "golden-master.txt");
    }

    [TestMethod]
    public void Games_MatchTheGoldenMaster()
    {
        List<string> actual = PlayAll();
        string path = MasterPath();

        if (Environment.GetEnvironmentVariable(RecordEnvironmentVariable) == "1")
        {
            string? masterFolder = Path.GetDirectoryName(path);
            Assert.IsNotNull(masterFolder);
            Directory.CreateDirectory(masterFolder);
            File.WriteAllText(path, string.Join("\n", actual) + "\n");
            Assert.Inconclusive($"Recorded {actual.Count} lines to {path}. " +
                                $"Unset {RecordEnvironmentVariable} and commit the file.");
        }

        Assert.IsTrue(File.Exists(path), $"No golden master at {path}. Record it with {RecordEnvironmentVariable}=1.");
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
        List<string> lines = new();
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
        Game game = new(seed);
        game.SetUpRandomGame(players);

        // rotate the line-up so every agent plays every seat
        List<IAgent> agents = new(players);
        for (int seat = 0; seat < players; seat++)
        {
            int agentKind = (gameIndex + seat) % AgentKindCount;
            agents.Add(NewAgent(agentKind));
        }

        new Match(game, agents).Play();

        List<string> lines = new();
        string prefix = $"p{players} g{gameIndex} seed={seed}";
        foreach (PlayerState player in game.Players)
        {
            lines.Add($"{prefix} seat={player.Seat} {agents[player.Seat].Name} {player.Wonder.Id} " +
                      $"stage={player.Wonder.BuiltStageCount} " +
                      $"vp={player.VictoryPoints} coins={player.Resources[Resource.Coins]} " +
                      $"military=+{player.VictoryTokens}/-{player.DefeatTokens} " +
                      $"built={string.Join(",", player.BuiltCards.Select(card => card.Id))}");
        }

        lines.Add($"{prefix} discarded={string.Join(",", game.DiscardPile.Select(card => card.Id))}");
        return lines;
    }

    private static IAgent NewAgent(int agentKind)
    {
        switch (agentKind)
        {
            case 0:
                return new HeuristicAgent();
            case 1:
                return new RandomAgent();
            default:
                throw new ArgumentOutOfRangeException(nameof(agentKind));
        }
    }
}
