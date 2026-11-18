using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.Heuristic;
using _7WondersGame.Agents.RandomChoice;
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

namespace _7WondersGameTests.Engine.Games;

/// <summary>
/// One turn state machine. Players decide through <see cref="Game.Submit"/> when <see cref="Game.PendingPlayers"/>
/// names them; the game moves on once everyone pending has decided.
/// </summary>
[TestClass]
public class TurnStateMachineTests
{
    // the line-ups are made of these agents: 0 is HeuristicAgent, 1 RandomAgent
    private const int HeuristicAndRandom = 2;
    private const int HeuristicOnly = 1;

    private static Card C(CardId id)
    {
        return CardsDataset.GameCards[id];
    }

    /// <summary>Three players on these wonders; each holds the given cards.</summary>
    private static Game ThreePlayers(WonderId seat0Wonder, WonderId seat1Wonder, WonderId seat2Wonder,
                                     params CardId[][] hands)
    {
        Game game = new Game(seed: 0);
        var wonders = new List<WonderBoardProgress>
        {
            new WonderBoardProgress(seat0Wonder),
            new WonderBoardProgress(seat1Wonder),
            new WonderBoardProgress(seat2Wonder),
        };
        game.Players = MockHelper.MockPlayers(wonders);
        for (int seat = 0; seat < hands.Length; seat++)
        {
            game.Players[seat].Hand.AddRange(hands[seat].Select(C));
        }

        return game;
    }

    private static Move Discard(CardId id)
    {
        return new Move(MoveAction.Discard, C(id));
    }

    private static Move Build(CardId id)
    {
        return new Move(MoveAction.Build, C(id));
    }

    [TestMethod]
    public void AMoveIsHeldUntilEveryoneHasChosen()
    {
        Game game = ThreePlayers(
            WonderId.GizahA, WonderId.GizahA, WonderId.GizahA,
            new[] { CardId.Baths },
            new[] { CardId.Walls },
            new[] { CardId.ClayPit });
        int coins = game.Players[0].Resources[Resource.Coins];

        game.Submit(0, Discard(CardId.Baths));

        Assert.AreEqual(Phase.ChooseMoves, game.Phase);
        CollectionAssert.AreEqual(new[] { 1, 2 }, game.PendingPlayers().ToArray());
        Assert.IsTrue(game.SomeMovesSubmitted);
        Assert.AreEqual(0, game.Turn);
        Assert.AreEqual(coins, game.Players[0].Resources[Resource.Coins]);
        CollectionAssert.Contains(game.Players[0].Hand, C(CardId.Baths));
    }

    [TestMethod]
    public void TheLastMoveInResolvesTheTurn()
    {
        Game game = ThreePlayers(
            WonderId.GizahA, WonderId.GizahA, WonderId.GizahA,
            new[] { CardId.Baths, CardId.Altar },
            new[] { CardId.Walls, CardId.Altar },
            new[] { CardId.ClayPit, CardId.Altar });
        int coins = game.Players[0].Resources[Resource.Coins];

        game.Submit(2, Discard(CardId.ClayPit));
        game.Submit(0, Discard(CardId.Baths));
        game.Submit(1, Discard(CardId.Walls));

        Assert.AreEqual(1, game.Turn);
        Assert.AreEqual(Phase.ChooseMoves, game.Phase);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, game.PendingPlayers().ToArray());
        Assert.IsFalse(game.SomeMovesSubmitted);
        Assert.AreEqual(coins + 3, game.Players[0].Resources[Resource.Coins]);
        Assert.AreEqual(3, game.DiscardPile.Count);
    }

    [TestMethod]
    public void ASeatWithNothingToDecide_IsRefused()
    {
        Game game = ThreePlayers(
            WonderId.GizahA, WonderId.GizahA, WonderId.GizahA,
            new[] { CardId.Baths },
            new[] { CardId.Walls },
            new[] { CardId.ClayPit });
        game.Submit(0, Discard(CardId.Baths));

        Assert.ThrowsException<InvalidOperationException>(() => game.Submit(0, Discard(CardId.Baths)));
        Assert.ThrowsException<InvalidOperationException>(() => game.Submit(3, Discard(CardId.Baths)));
    }

    [TestMethod]
    public void ARefusedTurn_LeavesTheLastMoveOpen()
    {
        Game game = ThreePlayers(
            WonderId.GizahA, WonderId.GizahA, WonderId.GizahA,
            new[] { CardId.Baths },
            new[] { CardId.Walls },
            new[] { CardId.ClayPit });
        game.Players[2].Resources[Resource.Coins] = 0;
        game.Submit(0, Discard(CardId.Baths));
        game.Submit(1, Discard(CardId.Walls));

        // Clay Pit costs a coin
        Assert.ThrowsException<InvalidOperationException>(() => game.Submit(2, Build(CardId.ClayPit)));

        CollectionAssert.AreEqual(new[] { 2 }, game.PendingPlayers().ToArray());
        game.Submit(2, Discard(CardId.ClayPit));
        Assert.AreEqual(1, game.Turn);
    }

    [TestMethod]
    public void TheLastTurnOfAnEra_EndsTheEra()
    {
        Game game = ThreePlayers(
            WonderId.GizahA, WonderId.GizahA, WonderId.GizahA,
            new[] { CardId.Baths, CardId.Altar },
            new[] { CardId.Walls, CardId.Altar },
            new[] { CardId.ClayPit, CardId.Altar });
        game.Turn = 5;
        game.CreateDecks();

        game.Submit(0, Discard(CardId.Baths));
        game.Submit(1, Discard(CardId.Walls));
        game.Submit(2, Discard(CardId.ClayPit));

        Assert.AreEqual(7, game.Turn);
        Assert.AreEqual(2, game.Era);
        Assert.AreEqual(Phase.ChooseMoves, game.Phase);
        Assert.AreEqual(7, game.Players[0].Hand.Count);
        // the three discards and the three last cards
        Assert.AreEqual(6, game.DiscardPile.Count);
    }

    [TestMethod]
    public void BabylonB_PlaysTheSeventhCard_BeforeTheEraEnds()
    {
        Game game = ThreePlayers(
            WonderId.BabylonB, WonderId.GizahA, WonderId.GizahA,
            new[] { CardId.Baths, CardId.LumberYard },
            new[] { CardId.Walls, CardId.Altar },
            new[] { CardId.ClayPit, CardId.Altar });
        game.Players[0].Wonder.BuiltStageCount = 2;   // its 2nd stage plays the 7th card
        game.Turn = 5;
        game.CreateDecks();

        game.Submit(0, Discard(CardId.Baths));
        game.Submit(1, Discard(CardId.Walls));
        game.Submit(2, Discard(CardId.ClayPit));

        Assert.AreEqual(Phase.SeventhCard, game.Phase);
        Assert.AreEqual(6, game.Turn);
        CollectionAssert.AreEqual(new[] { 0 }, game.PendingPlayers().ToArray());
        CollectionAssert.Contains(game.LegalMoves(0), Build(CardId.LumberYard));

        game.Submit(0, Build(CardId.LumberYard));

        CollectionAssert.Contains(game.Players[0].BuiltCards, C(CardId.LumberYard));
        Assert.AreEqual(7, game.Turn);
        Assert.AreEqual(2, game.Era);
        Assert.AreEqual(Phase.ChooseMoves, game.Phase);
    }

    /// <summary>
    /// Halikarnassos A builds its 2nd stage on turn 0; the others build free cards, so nothing is discarded.
    /// </summary>
    private static Game HalikarnassosBuildsStage2()
    {
        Game game = ThreePlayers(
            WonderId.GizahA, WonderId.HalikarnassosA, WonderId.GizahA,
            new[] { CardId.LumberYard, CardId.Altar },
            new[] { CardId.Baths, CardId.Altar },
            new[] { CardId.StonePit, CardId.Altar });
        game.Players[1].Wonder.BuiltStageCount = 1;
        game.Players[1].Resources[Resource.Ore] = 3;
        return game;
    }

    private static void PlayTurn0(Game game)
    {
        game.Submit(0, Build(CardId.LumberYard));
        game.Submit(1, new Move(MoveAction.BuildWonderStage, C(CardId.Baths)));
        game.Submit(2, Build(CardId.StonePit));
    }

    [TestMethod]
    public void Halikarnassos_EmptyDiscardPile_CanOnlySkip()
    {
        Game game = HalikarnassosBuildsStage2();
        PlayTurn0(game);

        Assert.AreEqual(Phase.DiscardPileBuild, game.Phase);
        CollectionAssert.AreEqual(new[] { 1 }, game.PendingPlayers().ToArray());
        CollectionAssert.AreEqual(new[] { new Move(MoveAction.Skip, null) }, game.LegalMoves(1));

        game.Submit(1, new Move(MoveAction.Skip, null));

        Assert.IsFalse(game.Players[1].HalikarnassosDiscardBuildPending);
        Assert.AreEqual(Phase.ChooseMoves, game.Phase);
        Assert.AreEqual(1, game.Turn);
    }

    [TestMethod]
    public void Halikarnassos_BuildsACardItCantAfford_FromThePile()
    {
        Game game = HalikarnassosBuildsStage2();
        game.DiscardPile.Add(C(CardId.Palace));
        PlayTurn0(game);

        Assert.AreEqual(Phase.DiscardPileBuild, game.Phase);
        CollectionAssert.Contains(game.LegalMoves(1), Build(CardId.Palace));

        game.Submit(1, Build(CardId.Palace));

        Assert.IsFalse(game.Players[1].HalikarnassosDiscardBuildPending);
        CollectionAssert.Contains(game.Players[1].BuiltCards, C(CardId.Palace));
        CollectionAssert.DoesNotContain(game.DiscardPile, C(CardId.Palace));
        Assert.AreEqual(Phase.ChooseMoves, game.Phase);
    }

    // ================= Loop =================

    [TestMethod]
    public void Loop_NoAgentSeesAnotherChoice()
    {
        var spies = new List<Spy>();
        var game = new Game(Seeding.GameSeed(7, 0));
        for (int seat = 0; seat < 4; seat++)
        {
            Spy spy = new Spy { Game = game };
            spies.Add(spy);
            game.Agents.Add(spy);
        }

        game.SetUpRandomGame();
        game.Loop(record: true);

        Assert.AreEqual(Phase.GameOver, game.Phase);
        Assert.IsFalse(spies.Any(spy => spy.SawAnotherChoice));
        Assert.IsFalse(spies.Any(spy => spy.SawLiveState), "agents get a copy of the state");
    }

    // the line-up rotates by game, so every agent plays every seat
    private static Game NewGame(int players, int gameIndex, int agentKindCount)
    {
        var game = new Game(Seeding.GameSeed(0xE1, players * 1000 + gameIndex));
        for (int seat = 0; seat < players; seat++)
        {
            int agentKind = (gameIndex + seat) % agentKindCount;
            game.Agents.Add(NewAgent(agentKind));
        }

        game.SetUpRandomGame();
        return game;
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

    private static List<string> Final(Game game)
    {
        List<string> lines = game.Players.Select(FinalStateOf).ToList();
        lines.Add($"discarded={string.Join(",", game.DiscardPile.Select(card => card.Id))}");
        return lines;
    }

    private static string FinalStateOf(PlayerState player)
    {
        return $"seat={player.Seat} {player.Wonder.Id} stage={player.Wonder.BuiltStageCount} " +
               $"vp={player.VictoryPoints} " +
               $"coins={player.Resources[Resource.Coins]} military=+{player.VictoryTokens}/-{player.DefeatTokens} " +
               $"built={string.Join(",", player.BuiltCards.Select(card => card.Id))}";
    }

    /// <summary>The agent's decision, asked the way <see cref="Game.Loop"/> asks it.</summary>
    private static Move Ask(Game game, int seat)
    {
        return game.Agents[seat].Choose(new GameView(game, seat));
    }

    /// <summary>
    /// Plays the game one decision at a time, last seat first, each submitted as soon as it is made.
    /// With <paramref name="copyEachStep"/>, every decision is made and submitted on a fresh copy, the way a search
    /// tree steps.
    /// </summary>
    private static Game StepThrough(Game game, bool copyEachStep, HashSet<Phase> phasesSeen)
    {
        IReadOnlyList<int> pending = game.PendingPlayers();
        while (pending.Count > 0)
        {
            phasesSeen.Add(game.Phase);
            if (copyEachStep)
            {
                game = game.DeepCopy();
            }

            int lastPendingIndex = pending.Count - 1;
            int seat = pending[lastPendingIndex];
            game.Submit(seat, Ask(game, seat));
            pending = game.PendingPlayers();
        }

        game.Loop(record: true);   // only scores: nothing is pending
        return game;
    }

    [TestMethod]
    public void SubmittingOneByOne_InReverseSeatOrder_MatchesLoop()
    {
        var phasesSeen = new HashSet<Phase>();
        for (int players = 3; players <= 7; players++)
        {
            for (int gameIndex = 0; gameIndex < 16; gameIndex++)
            {
                Game looped = NewGame(players, gameIndex, HeuristicAndRandom);
                looped.Loop(record: true);
                Game toStep = NewGame(players, gameIndex, HeuristicAndRandom);
                Game stepped = StepThrough(toStep, copyEachStep: false, phasesSeen);

                CollectionAssert.AreEqual(Final(looped), Final(stepped), $"{players} players, game {gameIndex}");
            }
        }

        CollectionAssert.IsSubsetOf(new[] { Phase.SeventhCard, Phase.DiscardPileBuild }, phasesSeen.ToList(),
            "the games should include Babylon's 7th card and a Halikarnassos build");
    }

    [TestMethod]
    public void SteppingThroughCopies_MatchesLoop()
    {
        // HeuristicAgent makes no random draws, so its choices don't depend on the copy's seed
        var phasesSeen = new HashSet<Phase>();
        for (int players = 3; players <= 7; players++)
        {
            for (int gameIndex = 0; gameIndex < 8; gameIndex++)
            {
                Game looped = NewGame(players, gameIndex, HeuristicOnly);
                looped.Loop(record: true);
                Game stepped = StepThrough(NewGame(players, gameIndex, HeuristicOnly), copyEachStep: true, phasesSeen);

                CollectionAssert.AreEqual(Final(looped), Final(stepped), $"{players} players, game {gameIndex}");
            }
        }
    }
}
