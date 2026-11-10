using _7WondersGame.src.models;
using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGameTests.GameTests
{
    /// <summary>
    /// E1: one turn state machine. Players decide through <see cref="Game.Submit"/> when <see cref="Game.PendingPlayers"/>
    /// names them; the game moves on once everyone pending has decided.
    /// </summary>
    [TestClass]
    public class TurnStateMachineTests
    {
        private static Card C(CardId id) => CardsDataset.GameCards[id];

        /// <summary>Three players; each holds the given cards.</summary>
        private static Game ThreePlayers(Wonder w0, Wonder w1, Wonder w2, params CardId[][] hands)
        {
            Game game = new Game();
            game.Players = MockHelper.MockRandomChoiceAIPlayers(new List<Wonder> { w0, w1, w2 });
            for (int seat = 0; seat < hands.Length; seat++)
                game.Players[seat].HandCards.AddRange(hands[seat].Select(C));
            return game;
        }

        private static Command Discard(CardId id) => new(ActionType.Discard, C(id));
        private static Command Build(CardId id) => new(ActionType.Build, C(id));

        [TestMethod]
        public void AMoveIsHeldUntilEveryoneHasChosen()
        {
            Game game = ThreePlayers(new GizahA(), new GizahA(), new GizahA(),
                new[] { CardId.Baths }, new[] { CardId.Walls }, new[] { CardId.ClayPit });
            int coins = game.Players[0].Resources[Resource.Coins];

            game.Submit(0, Discard(CardId.Baths));

            Assert.AreEqual(Phase.ChooseMoves, game.Phase);
            CollectionAssert.AreEqual(new[] { 1, 2 }, game.PendingPlayers().ToArray());
            Assert.IsTrue(game.MovesWaiting);
            Assert.AreEqual(0, game.Turn);
            Assert.AreEqual(coins, game.Players[0].Resources[Resource.Coins]);
            CollectionAssert.Contains(game.Players[0].HandCards, C(CardId.Baths));
        }

        [TestMethod]
        public void TheLastMoveInResolvesTheTurn()
        {
            Game game = ThreePlayers(new GizahA(), new GizahA(), new GizahA(),
                new[] { CardId.Baths, CardId.Altar }, new[] { CardId.Walls, CardId.Altar }, new[] { CardId.ClayPit, CardId.Altar });
            int coins = game.Players[0].Resources[Resource.Coins];

            game.Submit(2, Discard(CardId.ClayPit));
            game.Submit(0, Discard(CardId.Baths));
            game.Submit(1, Discard(CardId.Walls));

            Assert.AreEqual(1, game.Turn);
            Assert.AreEqual(Phase.ChooseMoves, game.Phase);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, game.PendingPlayers().ToArray());
            Assert.IsFalse(game.MovesWaiting);
            Assert.AreEqual(coins + 3, game.Players[0].Resources[Resource.Coins]);
            Assert.AreEqual(3, game.DiscardedCards.Count);
        }

        [TestMethod]
        public void ASeatWithNothingToDecide_IsRefused()
        {
            Game game = ThreePlayers(new GizahA(), new GizahA(), new GizahA(),
                new[] { CardId.Baths }, new[] { CardId.Walls }, new[] { CardId.ClayPit });
            game.Submit(0, Discard(CardId.Baths));

            Assert.ThrowsException<InvalidOperationException>(() => game.Submit(0, Discard(CardId.Baths)));
            Assert.ThrowsException<InvalidOperationException>(() => game.Submit(3, Discard(CardId.Baths)));
        }

        [TestMethod]
        public void ARefusedTurn_LeavesTheLastMoveOpen()
        {
            Game game = ThreePlayers(new GizahA(), new GizahA(), new GizahA(),
                new[] { CardId.Baths }, new[] { CardId.Walls }, new[] { CardId.ClayPit });
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
            Game game = ThreePlayers(new GizahA(), new GizahA(), new GizahA(),
                new[] { CardId.Baths, CardId.Altar }, new[] { CardId.Walls, CardId.Altar }, new[] { CardId.ClayPit, CardId.Altar });
            game.Turn = 5;
            game.CreateDecks();

            game.Submit(0, Discard(CardId.Baths));
            game.Submit(1, Discard(CardId.Walls));
            game.Submit(2, Discard(CardId.ClayPit));

            Assert.AreEqual(7, game.Turn);
            Assert.AreEqual(2, game.Era);
            Assert.AreEqual(Phase.ChooseMoves, game.Phase);
            Assert.AreEqual(7, game.Players[0].HandCards.Count);
            // the three discards and the three last cards
            Assert.AreEqual(6, game.DiscardedCards.Count);
        }

        [TestMethod]
        public void BabylonB_PlaysTheSeventhCard_BeforeTheEraEnds()
        {
            Game game = ThreePlayers(new BabylonB(), new GizahA(), new GizahA(),
                new[] { CardId.Baths, CardId.LumberYard }, new[] { CardId.Walls, CardId.Altar }, new[] { CardId.ClayPit, CardId.Altar });
            game.Players[0].PlaySeventh = true;
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

            CollectionAssert.Contains(game.Players[0].PlayedCards, C(CardId.LumberYard));
            Assert.AreEqual(7, game.Turn);
            Assert.AreEqual(2, game.Era);
            Assert.AreEqual(Phase.ChooseMoves, game.Phase);
        }

        /// <summary>Halikarnassos A builds its 2nd stage on turn 0; the others build free cards, so nothing is discarded.</summary>
        private static Game HalikarnassosBuildsStage2()
        {
            Game game = ThreePlayers(new GizahA(), new HalikarnassosA(), new GizahA(),
                new[] { CardId.LumberYard, CardId.Altar }, new[] { CardId.Baths, CardId.Altar }, new[] { CardId.StonePit, CardId.Altar });
            game.Players[1].Board.Stage = 1;
            game.Players[1].Resources[Resource.Ore] = 3;
            return game;
        }

        private static void PlayTurn0(Game game)
        {
            game.Submit(0, Build(CardId.LumberYard));
            game.Submit(1, new Command(ActionType.BuildWonder, C(CardId.Baths)));
            game.Submit(2, Build(CardId.StonePit));
        }

        [TestMethod]
        public void Halikarnassos_EmptyDiscardPile_CanOnlySkip()
        {
            Game game = HalikarnassosBuildsStage2();
            PlayTurn0(game);

            Assert.AreEqual(Phase.DiscardBuild, game.Phase);
            CollectionAssert.AreEqual(new[] { 1 }, game.PendingPlayers().ToArray());
            CollectionAssert.AreEqual(new[] { new Command(ActionType.Skip, null) }, game.LegalMoves(1));

            game.Submit(1, new Command(ActionType.Skip, null));

            Assert.IsFalse(game.Players[1].DiscardFree);
            Assert.AreEqual(Phase.ChooseMoves, game.Phase);
            Assert.AreEqual(1, game.Turn);
        }

        [TestMethod]
        public void Halikarnassos_BuildsACardItCantAfford_FromThePile()
        {
            Game game = HalikarnassosBuildsStage2();
            game.DiscardedCards.Add(C(CardId.Palace));
            PlayTurn0(game);

            Assert.AreEqual(Phase.DiscardBuild, game.Phase);
            CollectionAssert.Contains(game.LegalMoves(1), Build(CardId.Palace));

            game.Submit(1, Build(CardId.Palace));

            Assert.IsFalse(game.Players[1].DiscardFree);
            CollectionAssert.Contains(game.Players[1].PlayedCards, C(CardId.Palace));
            CollectionAssert.DoesNotContain(game.DiscardedCards, C(CardId.Palace));
            Assert.AreEqual(Phase.ChooseMoves, game.Phase);
        }

        // ================= Loop =================

        /// <summary>Records what it could see of the other players' choices whenever it was asked to choose.</summary>
        private sealed class Spy : RandomChoiceAI
        {
            public bool SawAnotherChoice;
            public Spy(int id) : base(id) { }

            public override void ChooseMoveCommand(Game g)
            {
                SawAnotherChoice |= g.MovesWaiting || g.PlayerCommands.Where((c, seat) => seat != Id && c != null).Any();
                base.ChooseMoveCommand(g);
            }
        }

        [TestMethod]
        public void Loop_NoAgentSeesAnotherChoice()
        {
            var spies = Enumerable.Range(0, 4).Select(seat => new Spy(seat)).ToList();
            var game = new Game(Seeding.GameSeed(7, 0)) { Players = spies.Cast<Player>().ToList() };
            game.InitGame();
            game.Loop(record: true);

            Assert.AreEqual(Phase.GameOver, game.Phase);
            Assert.IsFalse(spies.Any(s => s.SawAnotherChoice));
        }

        private static readonly Func<int, Player>[] Agents =
        {
            id => new DeterministicAI(id),
            id => new RandomChoiceAI(id),
            id => new PriorityWonderRandomChoiceAI(id),
            id => new PriorityStructureRandomChoiceAI(id),
        };

        private static Game NewGame(int players, int k, Func<int, Player>[] agents)
        {
            var game = new Game(Seeding.GameSeed(0xE1, players * 1000 + k))
            {
                Players = Enumerable.Range(0, players).Select(seat => agents[(k + seat) % agents.Length](seat)).ToList(),
            };
            game.InitGame();
            return game;
        }

        private static List<string> Final(Game game) =>
            game.Players.Select(p => $"seat={p.Id} {p.Board.Id} stage={p.Board.Stage} vp={p.VictoryPoints} " +
                                     $"coins={p.Resources[Resource.Coins]} military=+{p.VictoryTokens}/-{p.DefeatTokens} " +
                                     $"built={string.Join(",", p.PlayedCards.Select(c => c.Id))}")
                .Append($"discarded={string.Join(",", game.DiscardedCards.Select(c => c.Id))}")
                .ToList();

        /// <summary>The agent's decision, asked the way <see cref="Game.Loop"/> asks it.</summary>
        private static Command Ask(Game game, int seat)
        {
            Player p = game.Players[seat];
            p.PlayableCards = game.Phase == Phase.DiscardBuild
                ? p.GetPlayableCards(game.DiscardedCards, playAllFree: true)
                : p.GetPlayableCards(p.HandCards);
            p.CheckCanBuildWonder();
            if (game.Phase == Phase.DiscardBuild)
                return p.ChooseMoveCard(game, ActionType.Build, p.PlayableCards) is { } card
                    ? new Command(ActionType.Build, card)
                    : new Command(ActionType.Skip, null);
            p.ChooseMoveCommand(game);
            return game.PlayerCommands[seat];
        }

        /// <summary>
        /// Plays the game one decision at a time, last seat first, each submitted as soon as it is made.
        /// With <paramref name="copyEachStep"/>, every decision is made and submitted on a fresh copy, the way an MCTS tree steps.
        /// </summary>
        private static Game StepThrough(Game game, bool copyEachStep, HashSet<Phase> phasesSeen)
        {
            while (game.PendingPlayers() is { Count: > 0 } pending)
            {
                phasesSeen.Add(game.Phase);
                if (copyEachStep)
                    game = game.DeepCopy();
                int seat = pending[^1];
                game.Submit(seat, Ask(game, seat));
            }
            game.Loop(record: true);   // only scores: nothing is pending
            return game;
        }

        [TestMethod]
        public void SubmittingOneByOne_InReverseSeatOrder_MatchesLoop()
        {
            var phasesSeen = new HashSet<Phase>();
            for (int players = 3; players <= 7; players++)
                for (int k = 0; k < 16; k++)
                {
                    Game looped = NewGame(players, k, Agents);
                    looped.Loop(record: true);
                    Game stepped = StepThrough(NewGame(players, k, Agents), copyEachStep: false, phasesSeen);

                    CollectionAssert.AreEqual(Final(looped), Final(stepped), $"{players} players, game {k}");
                }
            CollectionAssert.IsSubsetOf(new[] { Phase.SeventhCard, Phase.DiscardBuild }, phasesSeen.ToList(),
                "the games should include Babylon's 7th card and a Halikarnassos build");
        }

        [TestMethod]
        public void SteppingThroughCopies_MatchesLoop()
        {
            // DeterministicAI makes no random draws, so its choices don't depend on the copy's seed
            Func<int, Player>[] deterministic = { id => new DeterministicAI(id) };
            var phasesSeen = new HashSet<Phase>();
            for (int players = 3; players <= 7; players++)
                for (int k = 0; k < 8; k++)
                {
                    Game looped = NewGame(players, k, deterministic);
                    looped.Loop(record: true);
                    Game stepped = StepThrough(NewGame(players, k, deterministic), copyEachStep: true, phasesSeen);

                    CollectionAssert.AreEqual(Final(looped), Final(stepped), $"{players} players, game {k}");
                }
        }
    }
}
