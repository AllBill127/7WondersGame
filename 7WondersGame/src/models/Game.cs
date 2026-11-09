using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using Serilog;

namespace _7WondersGame.src.models
{
    public class Game
    {
        public List<Player> Players { get; set; }
        int NumPlayers { get; set; } = 3;
        public int Era { get; set; } = 1;
        // 0, 7 , 14, 21 are new era turns (0 being the start of the game)
        public int Turn { get; set; } = 0;
        public List<Card>[] Decks { get; set; } 
        public List<Card> DiscardedCards { get; set; }

        // game loop data
        public Command[] PlayerCommands { get; set; }
        public bool[] PlayersReady { get; set; }

        // randomness (B1): everything random in a game comes from its seed
        public ulong Seed { get; private set; }
        private readonly Random _rng;
        private readonly Dictionary<int, Random> _agentRngs = new();
        private ulong _copyCount = 0;

        /// <summary>
        /// A game with seed 0. Use <see cref="Game(ulong)"/> when the game should differ from others.
        /// </summary>
        public Game() : this(0) { }

        public Game(ulong seed)
        {
            Seed = seed;
            _rng = Seeding.CreateRandom(Seeding.Nth(seed, Seeding.GameStream));

            Players = new();
            Decks = new List<Card>[3];
            for (int i = 0; i < Decks.Length; ++i)
                Decks[i] = new List<Card>();
            DiscardedCards = new();

            // init with default size; changes in InitGame()
            PlayerCommands = new Command[NumPlayers];
            PlayersReady = new bool[NumPlayers];
        }

        /// <summary>
        /// The random stream for the player in this seat. Agents draw all their random choices from it.
        /// </summary>
        public Random AgentRandom(int seat)
        {
            if (!_agentRngs.TryGetValue(seat, out Random? rng))
            {
                rng = Seeding.CreateRandom(Seeding.AgentSeed(Seed, seat));
                _agentRngs[seat] = rng;
            }
            return rng;
        }

        /// <summary>
        /// Copies the game state. The copy gets its own seed (derived from this game's seed and a copy counter),
        /// so searching in copies never uses up this game's random streams.
        /// </summary>
        public Game DeepCopy()
        {
            Game copy = new Game(Seeding.Nth(Seed, Seeding.CopyStreamBase + ++_copyCount));

            // fully copy players
            // copy player data
            foreach (Player p in Players)
                copy.Players.Add((Player)p.DeepCopy());
            // set player neighbors
            copy.NumPlayers = this.NumPlayers;
            copy.SetPlayerNeighbors();

            copy.Era = this.Era;
            copy.Turn = this.Turn;

            // copy decks
            copy.Decks[0] = this.Decks[0].Select(e => e).ToList();
            copy.Decks[1] = this.Decks[1].Select(e => e).ToList();
            copy.Decks[2] = this.Decks[2].Select(e => e).ToList();

            copy.DiscardedCards = this.DiscardedCards.Select(e => e).ToList();

            copy.PlayerCommands = this.PlayerCommands.Select(e => e).ToArray();
            copy.PlayersReady = this.PlayersReady.Select(e => e).ToArray();

            return copy;
        }


        // ============ Game creation related ============
        /// <summary>
        /// Setup initial game data <br/>
        /// - Game Players must be initiated and set before calling this function
        /// </summary>
        public void InitGame()
        {
            NumPlayers = Players.Count;
            PlayerCommands = new Command[NumPlayers];
            PlayersReady = new bool[NumPlayers];

            SetPlayerNeighbors();

            CreateDecks();
            GiveCards();
            GiveWonders();
        }


        /// <summary>
        /// Setup initial game data with game dataset containing decks card ids and wonder ids in specific shuffled order<br/>
        /// - Game Players must be initiated and set before calling this function
        /// </summary>
        public void InitGameNotRandom(List<int>[] gameDataset)
        {
            NumPlayers = Players.Count;
            PlayerCommands = new Command[NumPlayers];
            PlayersReady = new bool[NumPlayers];

            SetPlayerNeighbors();


            for (int i = 0; i < 3; i++)
            {
                foreach (var cardId in gameDataset[i])
                {
                    Decks[i].Add(CardsDataset.GetInstance(Players.Count).GameCards[(CardId)cardId]);
                }
            }
            GiveCards();

            for (int i = 0; i < Players.Count; i++)
            {
                AssignWonder(Players[i], gameDataset[3][i]);
            }
        }

        public void SetPlayerNeighbors()
        {
            // set player neighbors
            for (int i = 0; i < NumPlayers; i++)
            {
                if (i == 0)
                {
                    Players[i].PlayerWest = Players[NumPlayers - 1];
                    Players[i].PlayerEast = Players[i + 1];
                }
                else if (i == NumPlayers - 1)
                {
                    Players[i].PlayerWest = Players[i - 1];
                    Players[i].PlayerEast = Players[0];
                }
                else
                {
                    Players[i].PlayerWest = Players[i - 1];
                    Players[i].PlayerEast = Players[i + 1];
                }
            }
        }

        public void CreateDecks()
        {
            Random rng = _rng;

            // Add cards to decks
            CardsDataset cardsDS = CardsDataset.GetInstance(NumPlayers);
            foreach (var card in cardsDS.Cards)
            {
                for (int i = 0; i < card.Value.NumCardsInGame; i++)
                {
                    Decks[card.Value.Era - 1].Add(card.Value);
                }
            }

            // Add guild cards to Era 3 deck
            var guildCardsDict = cardsDS.GuildCards;
            var guildCards = guildCardsDict.Values.OrderBy(a => rng.Next()).ToList();

            for (int i = 0; i < NumPlayers + 2; i++)
            {
                Decks[2].Add(guildCards[i]);
            }

            // Shuffle decks
            Decks[0] = Decks[0].OrderBy(a => rng.Next()).ToList();
            Decks[1] = Decks[1].OrderBy(a => rng.Next()).ToList();
            Decks[2] = Decks[2].OrderBy(a => rng.Next()).ToList();
        }

        private void GiveCards()
        {
            List<Card> cards = new List<Card>();
            int cardIdx = 0;

            foreach (Player player in Players)
            {
                cards.Clear();
                for (int i = 0; i < 7; i++)
                {
                    cards.Add(Decks[Era - 1][cardIdx++]);
                }

                player.HandCards = cards.Select(e => e).ToList();
            }
        }

        private Wonder CreateWonders(int wonderId)
        {
            Wonder wonder = null;
            switch (wonderId)
            {
                case 0:
                    wonder = new GizahA();
                    break;
                case 1:
                    wonder = new BabylonA();
                    break;
                case 2:
                    wonder = new OlympiaA();
                    break;
                case 3:
                    wonder = new RhodosA();
                    break;
                case 4:
                    wonder = new EphesosA();
                    break;
                case 5:
                    wonder = new AlexandriaA();
                    break;
                case 6:
                    wonder = new HalikarnassosA();
                    break;
                case 7:
                    wonder = new GizahB();
                    break;
                case 8:
                    wonder = new BabylonB();
                    break;
                case 9:
                    wonder = new OlympiaB();
                    break;
                case 10:
                    wonder = new RhodosB();
                    break;
                case 11:
                    wonder = new EphesosB();
                    break;
                case 12:
                    wonder = new AlexandriaB();
                    break;
                case 13:
                    wonder = new HalikarnassosB();
                    break;
                default:
                    break;
            }

            return wonder;
        }

        /// <summary>
        /// Give a player a wonder board and its starting resource. Used by both setup paths.
        /// </summary>
        private void AssignWonder(Player player, int wonderId)
        {
            player.Board = CreateWonders(wonderId);
            player.Resources[player.Board.Production] += 1;
        }

        private void GiveWonders()
        {
            Random rng = _rng;
            bool[] wonder_availability = new bool[7];
            for (int i = 0; i < 7; i++)
                wonder_availability[i] = true;

            foreach (Player player in Players)
            {
                while (true)
                {
                    int n = rng.Next(14);
                    if (wonder_availability[n % 7])
                    {
                        AssignWonder(player, n);
                        wonder_availability[n % 7] = false;
                        break;
                    }
                }
            }
        }



        // ============ Game running related ============
        /// <summary>
        /// Check if all players are ready
        /// </summary>
        /// <returns>
        /// true if all players are ready; false otherwise
        /// </returns>
        private bool ArePlayersReady()
        {
            bool allReady = true;
            for (int i = 0; i < Players.Count; i++)
            {
                allReady = allReady && PlayersReady[i];
            }
            return allReady;
        }

        void NextTurn(bool verbose = false)
        {
            Turn++;

            // TRY COMPUTE Babylon PlaySeventh effect
            // * Turns 6 13 and 20 -> last card in hand card.
            if (Turn == 6 || Turn == 13 || Turn == 20)
            {
                foreach (Player player in Players)
                {
                    if (player.PlaySeventh)
                        // NOTE:
                        //          player can play seventh Era card so we need to process 1 default move.
                        //          1. calculate players new playable cards,
                        //          2. read this players command and process it
                        ExtraLoop(player, playSeventh: true, verbose: verbose);
                }
                Turn++;
            }

            // discard last card in hand if turn is 7 14 and 21
            if (Turn % 7 == 0)
            {
                // transfer the remaining card in each player's hand to the discarded card list
                foreach (Player player in Players)
                {
                    List<Card> cards = player.HandCards;
                    // cards must be size 1!!
                    for (int i = 0; i < cards.Count; i++)
                        DiscardedCards.Add(cards[i]);
                }
            }

            // TRY COMPUTE Halikarnassos DiscardFree effect
            // this is done after all players have completed their turn
            // and if last turn in Era - all last cards are discarded and BabylonA2 effect computed
            foreach (Player player in Players)
            {
                WonderId id = player.Board.Id;
                int stage = player.Board.Stage;

                // hard coded fix for DiscardFree flag going bonkers
                // CAUSE:   not initiating new players when looping over many games keeps their flags up after game end
                // FIX:     hard coded 
                // NOTE:    no more neede as this fix is simple and implemented
                if (player.DiscardFree && id == WonderId.HalikarnassosA && stage != 2)
                { 
                    player.DiscardFree = false;
                }

                if ((id == WonderId.HalikarnassosA && stage == 2 ||
                    id == WonderId.HalikarnassosB && stage >= 1) &&
                    player.DiscardFree)
                    // NOTE:
                    //          player can play card from discarded cards for free.
                    //          1. calculate players new playable cards from discardedCards
                    //          2. read this players command and process it
                    ExtraLoop(player, discardFree: true, verbose: verbose);
            }


            // if end of an era (turns 7 14 and 21)
            // calculate battle tokens, reset OlympiaA2 effect if needed, give new Era cards to players
            if (Turn % 7 == 0)
            {
                foreach (Player player in Players)
                {
                    // reset Olympia A stage 2 effect flag
                    if (player.Board.Id == WonderId.OlympiaA && player.Board.Stage >= 2)
                        player.FreeCardOnce = true;
                    else
                        player.FreeCardOnce = false;
                    // calculate Victory and Defeat tokens 
                    player.Battle(Era);
                }

                Era++;
                Log.Debug("-----------------------------------------------------------------\n");
                if (Era < 4)
                    Log.Debug("Nova era: {era}", Era);
                else
                {
                    Log.Debug("End of game");
                    return;
                }

                // give new Era cards to player 
                GiveCards();
            }
            else
            {
                RotateHandCards();
            }
        }

        public void RotateHandCards()
        {
            Player p,
                p1,
                neighbor;
            p1 = p = Players.First();
            List<Card> neighbor_deck,
                player_deck = p1.HandCards;

            bool clockwise = (Era == 1 || Era == 3);
            do
            {
                // Get the neighbor of player p who will receive the cards
                // Keep his deck from being overwritten and lost
                // The neighbor receives the cards from player p
                // The neighbor becomes the player p
                // Continue until you reach the first player again
                neighbor = clockwise ? p.PlayerWest : p.PlayerEast;
                // backup neighbor card list
                neighbor_deck = neighbor.HandCards;
                // change card list references
                neighbor.HandCards = player_deck;
                // go to next player
                p = neighbor;
                // set new list reference that is the neighbors hand card list copy
                player_deck = neighbor_deck;
            } while (p1 != p);
        }

        /// <summary>
        /// Final results of the last <see cref="Loop(bool)"/> run with record = true; null otherwise.
        /// </summary>
        public List<SeatResult>? Results { get; private set; }

        /// <summary>
        /// Plays the game to the end and scores it.
        /// </summary>
        /// <param name="record">true for a real game (not an MCTS rollout): log progress and keep <see cref="Results"/>.</param>
        /// <returns>Each seat's final VP.</returns>
        public Dictionary<int, int> Loop(bool record = false)
        {
            while (Turn < 21)
            {
                Log.Debug("\n::: PREPARE FOR TURN {Turn} :::", Turn);
                // update pre turn game data for players
                foreach (Player player in Players)
                {
                    // set PlayableCards
                    player.PlayableCards = player.GetPlayableCards(player.HandCards);
                    // set CanBuildWonder
                    player.CheckCanBuildWonder();
                }

                Log.Debug("\n::: TURN {Turn} :::", Turn);

                // Print Playable Cards for each player.
                foreach (Player player in Players)
                {
                    Log.Debug("> Playable cards for player {PlayerId}:", player.Id);
                    foreach (Card card in player.PlayableCards)
                        Log.Debug("  ({PlayerId}) {CardName}", player.Id, card.Name);

                    if (player.CanBuildWonder)
                        Log.Debug("> Player {PlayerId} CAN build a wonder stage! ", player.Id);
                    else
                        Log.Debug("> Player {PlayerId} CANNOT build a wonder stage! ", player.Id);
                }

                // make a turn
                Log.Debug("<Waiting for players ready...>");
                while (!ArePlayersReady())
                {
                    foreach (Player player in Players)
                        player.ChooseMoveCommand(this);
                }

                // handle command for each player
                foreach (Player player in Players)
                {
                    Command playerCommand = PlayerCommands[player.Id];

                    // handle command for player
                    HandlePlayerCommand(player, playerCommand, player.HandCards);

                    // reset PlayersReady status for player
                    PlayersReady[player.Id] = false;
                }

                // Moves the game to the next turn, processes DiscardFree and PlaySeventh effects
                NextTurn(record);
                if (record)
                    Log.Information("Turn {turn} complete", Turn);
            }

            //calculate scores
            foreach (Player player in Players)
            {
                // Copies a neighbor guild before scoring if the player has the ability to do so.
                player.CopyGuild(); 
                player.CalculateScore();
                Log.Debug("results Player {PlayerId} score: {PlayerScore}", player.Id, player.VictoryPoints);
            }

            // keep the results before the game memory is released
            if (record)
                Results = GameResult.ForPlayers(Players);

            var scores = new Dictionary<int, int>();
            Players.ForEach(p => scores.Add(p.Id, p.VictoryPoints));
            ReleaseGameMemory();
            return scores;
        }

        public void ExtraLoop(Player player, bool playSeventh = false, bool discardFree = false, bool verbose = false)
        {
            // update pre turn game data for player
            // set the list of cards from which to play based on effect flag
            List<Card> cardsList;
            if (playSeventh)
                cardsList = player.HandCards;
            else if (discardFree)
                cardsList = DiscardedCards;
            else
                return;
            // set PlayableCards depending on effect
            // - DiscardFree effect allows to build free so we only check if No duplicate card
            if (discardFree && !playSeventh)
                player.PlayableCards = player.GetPlayableCards(cardsList, true);
            // - PlaySeventh effect allows any default action so we check default PlayableCards
            if (playSeventh && !discardFree)
                player.PlayableCards = player.GetPlayableCards(cardsList);
            // set CanBuildWonder
            player.CheckCanBuildWonder();

            Log.Debug("\n::: EXTRA TURN {playSeventh}{discardFree} :::", playSeventh? "PlaySeventh" : "", discardFree? "DiscardFree" : "");

            // Print Playable Cards for player.
            Log.Debug("> Playable cards for player {PlayerId}:", player.Id);
            foreach (Card card in player.PlayableCards)
                Log.Debug("  ({PlayerId}) {CardName}", player.Id, card.Name);

            if (player.CanBuildWonder)
                Log.Debug("> Player {PlayerId} CAN build a wonder stage! ", player.Id);
            else
                Log.Debug("> Player {PlayerId} CANNOT build a wonder stage! ", player.Id);

            // depending on effect flag make different moves
            if (playSeventh && !discardFree)
            {
                // make a normal turn
                Log.Debug("<Waiting for player ready...>");
                player.ChooseMoveCommand(this);
                Command playerCommand = PlayerCommands[player.Id];

                // handle command for player
                HandlePlayerCommand(player, playerCommand, cardsList);
                // reset PlayersReady status for player
                PlayersReady[player.Id] = false;
            }
            else if (discardFree && !playSeventh)
            {
                // choose a card from playable cards
                Card? cardPlayed = player.ChooseMoveCard(this, "build_structure", player.PlayableCards);

                if(cardPlayed != null)
                {
                    // build the card for free
                    if (player.BuildDiscardFree(cardPlayed, cardsList))
                    {
                        Log.Debug("<BuildDiscardFree OK>");
                    }
                    else
                    {
                        Log.Debug("<BuildDiscardFree NOK>");
                        Log.Error("SHOULD NOT HAPPEN");
                    }
                }
                else
                {
                    Log.Debug("No cards in Discarded Cards list");
                    // disable discardFree effect even if no card were played
                    player.DiscardFree = false;
                }
            }
            if (verbose)
                Log.Information("Turn {turn} (Player:{pId} discard:{discardFree} | seventh:{playSeventh}) complete", Turn, player.Id, discardFree, playSeventh);
        }

        public void HandlePlayerCommand(Player player, Command playerCommand, List<Card> cardsList)
        {
            string command = playerCommand.Subcommand;
            Card? cardPlayed = playerCommand.Argument;

            if (!command.Equals("skip_move") && cardPlayed != null)
            {
                if (command.Equals("build_structure"))
                {
                    if (player.BuildStructure(cardPlayed, cardsList, false))
                    {
                        Log.Debug("<BuildStructure OK>");
                    }
                    else
                    {
                        Log.Debug("<BuildStructure NOK>");
                        Log.Error("SHOULD NOT HAPPEN");
                        command = "discard";
                    }

                }
                else if (command.Equals("build_hand_free"))
                {
                    if (player.BuildHandFree(cardPlayed, cardsList))
                    {
                        Log.Debug("<BuildHandFree OK>");
                    }
                    else
                    {
                        Log.Debug("<BuildHandFree NOK>");
                        Log.Error("SHOULD NOT HAPPEN");
                        command = "discard";
                    }

                }
                else if (command.Equals("build_wonder"))
                {
                    if (player.BuildWonder(cardPlayed, cardsList))
                    {
                        Log.Debug("<BuildWonder OK>");
                    }
                    else
                    {
                        Log.Debug("<BuildWonder NOK>");
                        Log.Error("SHOULD NOT HAPPEN");
                        command = "discard";
                    }
                }

                if (command.Equals("discard"))
                {
                    player.Discard(cardPlayed); //Gives player 3 coins.
                    Log.Debug("<Discard OK>");
                    DiscardedCards.Add(cardPlayed);
                }
            }
            else
                Log.Debug("Skipping move - no playable card found");
        }



        // ================= MCTS related =================
        public List<Command> GetLegalMoves(int playerId, bool playSeventh = false, bool discardFree = false)
        {
            List<string> actionOpt = new List<string> { "build_structure", "build_hand_free", "build_wonder", "discard", "skip_move" };
            List<Command> legalMoves = new();
            Player player = Players[playerId];

            // update player playable cards and wonder 
            player.PlayableCards = player.GetPlayableCards(player.HandCards);
            player.CheckCanBuildWonder();

            // get possible actions for general situation
            if (!playSeventh && !discardFree)
            {
                // add build structure moves with playable cards
                if (player.PlayableCards.Count > 0)
                {
                    foreach (Card c in player.PlayableCards)
                        legalMoves.Add(new Command(actionOpt[0], c));
                }
                // add build wonder moves with any of hand cards
                if (player.CanBuildWonder)
                {
                    foreach (Card c in player.HandCards)
                        legalMoves.Add(new Command(actionOpt[2], c));
                }
                // add build hand free moves with all playable cards when all hand cards are free
                if (player.FreeCardOnce)
                {
                    player.PlayableCards = player.GetPlayableCards(player.HandCards, playAllFree: true);
                    foreach (Card c in player.PlayableCards)
                        legalMoves.Add(new Command(actionOpt[1], c));
                }

                // add discard moves
                foreach (Card c in player.HandCards)
                    legalMoves.Add(new Command(actionOpt[3], c));
            }
            // get possible actions for PlaySeventh effect situation
            else if (playSeventh && !discardFree)
            {
                // add build structure moves with playable cards
                if (player.PlayableCards.Count > 0)
                {
                    foreach (Card c in player.PlayableCards)
                        legalMoves.Add(new Command(actionOpt[0], c));
                }
                // add build wonder moves with any of hand cards
                if (player.CanBuildWonder)
                {
                    foreach (Card c in player.HandCards)
                        legalMoves.Add(new Command(actionOpt[2], c));
                }

                // add discard moves
                foreach (Card c in player.HandCards)
                    legalMoves.Add(new Command(actionOpt[3], c));
            }
            // get legal build moves for DiscardFree effect situation
            else if (discardFree && player.DiscardFree && !playSeventh)
            {
                // add build structure moves with playable cards from games discarded cards
                player.PlayableCards = player.GetPlayableCards(DiscardedCards);

                if (player.PlayableCards.Count > 0)
                {
                    foreach (Card c in player.PlayableCards)
                        legalMoves.Add(new Command(actionOpt[0], c));
                }
            }

            // add single skip move when no moves available (should only happen on extra loop for FreeCardOnce or DiscardFree)
            if (legalMoves.Count < 1)
                legalMoves.Add(new Command(actionOpt[4], null));

            // group duplicate moves (occurs when duplicate cards in hand)
            List<Command> groupedLegalMoves = legalMoves
                .GroupBy(move => move)
                .Select(group => group.Key)
                .ToList();

            return groupedLegalMoves;
        }


        public List<Command> GetLegalMovesPrunedDiscards(int playerId, bool playSeventh = false, bool discardFree = false)
        {
            Random rng = _rng;
            int discardCardIdx;
            List<string> actionOpt = new List<string> { "build_structure", "build_hand_free", "build_wonder", "discard", "skip_move" };
            List<Command> legalMoves = new();
            Player player = Players[playerId];

            // update player playable cards and wonder 
            player.PlayableCards = player.GetPlayableCards(player.HandCards);
            player.CheckCanBuildWonder();

            // get possible actions for general situation
            if (!playSeventh && !discardFree)
            {
                // add build structure moves with playable cards
                if (player.PlayableCards.Count > 0)
                {
                    foreach (Card c in player.PlayableCards)
                        legalMoves.Add(new Command(actionOpt[0], c));
                }
                // add build wonder moves with any of hand cards
                if (player.CanBuildWonder)
                {
                    // add single random discard for wonder building if available
                    if (player.HandCards.Count > 0)
                    {
                        discardCardIdx = rng.Next(player.HandCards.Count);
                        legalMoves.Add(new Command(actionOpt[2], player.HandCards[discardCardIdx]));
                    }
                }
                // add build hand free moves with all playable cards when all hand cards are free
                if (player.FreeCardOnce)
                {
                    player.PlayableCards = player.GetPlayableCards(player.HandCards, playAllFree: true);
                    foreach (Card c in player.PlayableCards)
                        legalMoves.Add(new Command(actionOpt[1], c));
                }

                // add single random discard move
                if (player.HandCards.Count > 0)
                {
                    discardCardIdx = rng.Next(player.HandCards.Count);
                    legalMoves.Add(new Command(actionOpt[3], player.HandCards[discardCardIdx]));
                }
            }
            // get possible actions for PlaySeventh effect situation
            else if (playSeventh && !discardFree)
            {
                // add build structure moves with playable cards
                if (player.PlayableCards.Count > 0)
                {
                    foreach (Card c in player.PlayableCards)
                        legalMoves.Add(new Command(actionOpt[0], c));
                }
                // add build wonder moves with any of hand cards
                if (player.CanBuildWonder)
                {
                    // add single random discard for wonder building if available
                    if (player.HandCards.Count > 0)
                    {
                        discardCardIdx = rng.Next(player.HandCards.Count);
                        legalMoves.Add(new Command(actionOpt[2], player.HandCards[discardCardIdx]));
                    }
                }

                // add single random discard move
                if (player.HandCards.Count > 0)
                {
                    discardCardIdx = rng.Next(player.HandCards.Count);
                    legalMoves.Add(new Command(actionOpt[3], player.HandCards[discardCardIdx]));
                }
            }
            // get legal build moves for DiscardFree effect situation
            else if (discardFree && player.DiscardFree && !playSeventh)
            {
                // add build structure moves with playable cards from games discarded cards
                player.PlayableCards = player.GetPlayableCards(DiscardedCards);

                if (player.PlayableCards.Count > 0)
                {
                    foreach (Card c in player.PlayableCards)
                        legalMoves.Add(new Command(actionOpt[0], c));
                }
            }

            // add single skip move when no moves available (should only happen on extra loop for FreeCardOnce or DiscardFree)
            if (legalMoves.Count < 1)
                legalMoves.Add(new Command(actionOpt[4], null));

            // group duplicate moves (occurs when duplicate cards in hand)
            List<Command> groupedLegalMoves = legalMoves
                .GroupBy(move => move)
                .Select(group => group.Key)
                .ToList();

            return groupedLegalMoves;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="playerMove"></param>
        /// <returns>-1 when game is finished</returns>
        public int ApplyMove(int playerId, Command playerMove)
        {
            int newPlayer = -1;

            // if not all players are ready then simply set move and player ready
            if (PlayersReady.Contains(false))
            {
                PlayerCommands[playerId] = playerMove;
                PlayersReady[playerId] = true;
            }

            // if all players ready apply their moves
            if (!PlayersReady.Contains(false))
            {
                // handle command for each player
                foreach (Player p in Players)
                {
                    Command playerCommand = PlayerCommands[p.Id];

                    // handle command for player
                    HandlePlayerCommand(p, playerCommand, p.HandCards);

                    // reset PlayersReady status for player
                    PlayersReady[p.Id] = false;
                }

                Turn++;
            }

            // if on the last turn playSeventh effect active for some player and the player hasnt chosen a move return current state and set new player to the PlaySeventh player
            var playSeventhPlayer = Players.FirstOrDefault(p => p.PlaySeventh == true);
            if (playSeventhPlayer != null)
            {
                if ((Turn == 6 || Turn == 13 || Turn == 20) &&
                    PlayersReady[playSeventhPlayer.Id] == false)
                {
                    return playSeventhPlayer.Id;
                }
            }
            // increment turn even if playSeventh effect not active but 6, 13, 20 turn (technically a 7th turn which is considered an extra turn)
            else if (Turn == 6 || Turn == 13 || Turn == 20)
                Turn++;

            // apply PlaySeventh extra move if current player from mcts expansion has this effect and its time to play it out
            if (Turn == 6 || Turn == 13 || Turn == 20)
            {
                if (PlayersReady[playerId] == true && 
                    Players[playerId].PlaySeventh == true)
                {
                    // handle command for player
                    HandlePlayerCommand(Players[playerId], playerMove, Players[playerId].HandCards);

                    // reset PlayersReady status for player
                    PlayersReady[playerId] = false;

                    Turn++;
                }
            }

            // discard last card in hand if turn is 7 14 and 21 and all palyer commands were handled (PlayersReady all false)
            if (Turn % 7 == 0 && Turn != 0 && !PlayersReady.Contains(true))
            {
                // transfer the remaining card in each player's hand to the discarded card list
                foreach (Player p in Players)
                {
                    List<Card> cards = p.HandCards;
                    // cards must be size 1!!
                    for (int i = 0; i < cards.Count; i++)
                        DiscardedCards.Add(cards[i]);
                }
            }

            // apply DiscrdFree extra move if current player from mcts expansion has this effect and its time to play it out
            if (PlayersReady[playerId] == true &&
                Players[playerId].DiscardFree == true)
            {
                Card? cardPlayed = playerMove.Argument;
                if (cardPlayed != null)
                {
                    // build the card for free
                    if (Players[playerId].BuildDiscardFree(cardPlayed, DiscardedCards))
                    {
                        Log.Debug("<BuildDiscardFree OK>");
                    }
                    else
                    {
                        Log.Debug("<BuildDiscardFree NOK>");
                        Log.Error("SHOULD NOT HAPPEN");
                    }
                }
                else
                {
                    Log.Debug("No cards in Discarded Cards list");
                    // disable discardFree effect even if no card were played
                    Players[playerId].DiscardFree = false;
                }

                // reset player ready status
                PlayersReady[playerId] = false;
            }

            // if at the end of a turn discardFree effect active for some player and the player hasnt chosen a move return current state and set new player to the DiscardFree player
            var discardFreePlayer = Players.FirstOrDefault(p => p.DiscardFree == true);
            if (discardFreePlayer is not null &&
                //Turn % 7 == 0 && Turn != 0 &&
                PlayersReady[discardFreePlayer.Id] == false)
            {
                return discardFreePlayer.Id;
            }

            // if end of an era (turns 7 14 and 21) and all player moves were handled (PlayersReady all false)
            // calculate battle tokens, reset OlympiaA2 effect if needed, give new Era cards to players
            if (Turn % 7 == 0 && Turn != 0 && !PlayersReady.Contains(true))
            {
                foreach (Player p in Players)
                {
                    // reset Olympia A stage 2 effect flag
                    if (p.Board.Id == WonderId.OlympiaA && p.Board.Stage >= 2)
                        p.FreeCardOnce = true;
                    else
                        p.FreeCardOnce = false;
                    // calculate Victory and Defeat tokens 
                    p.Battle(Era);
                }

                Era++;
                if (Era >= 4)
                {
                    // return next player in turn cycle as null because the game has ended
                    return -1;
                }

                // give new Era cards to players 
                GiveCards();
            }
            // rotate hand cards if turn and extra turn is complete (at this point no players have chosen a move)
            else if (!PlayersReady.Contains(true))
            {
                RotateHandCards();
            }

            // set new player to first so the next turn cycle can be explored
            // NOTE:    not sure if this is valid because this should be reset to root node player, but the turn cycle will still be completed so at that point we should calculate the uct
            if (Turn % 7 == 0 && Turn != 0 && !PlayersReady.Contains(true))
            {
                newPlayer = Players[0].Id;
            }
            // return next player in turn cycle 
            else
            {
                int newPlayerIdx = playerId - 1 < 0 ? Players.Count - 1 : playerId - 1;
                // deep copy player without his neighbors
                // neighbors arent used with this variable so it isnt neccessary
                newPlayer = Players[newPlayerIdx].Id;
            }
            
            return newPlayer;
        }

        public Game ApplyJointMove(List<Command> playersJointMove)
        {
            var discardFreePlayer = Players.FirstOrDefault(p => p.DiscardFree == true);

            // set other player moves and this players chosen move
            foreach (Player player in Players)
            {
                PlayerCommands[player.Id] = playersJointMove[player.Id];
                PlayersReady[player.Id] = true;
            }

            // handle command for each player
            foreach (Player p in Players)
            {
                Command playerCommand = PlayerCommands[p.Id];

                // handle command for player
                if ((Turn == 6 || Turn == 13 || Turn == 20) &&
                    p.PlaySeventh)
                {
                    HandlePlayerCommand(p, playerCommand, p.HandCards);
                }
                // apply DiscrdFree extra move if current player from mcts expansion has this effect and its time to play it out
                else if (discardFreePlayer != null &&
                    discardFreePlayer.DiscardFree == true &&
                    p.Id == discardFreePlayer.Id)
                {
                    Card? cardPlayed = playerCommand.Argument;
                    if (cardPlayed != null)
                    {
                        // build the card for free
                        if (Players[discardFreePlayer.Id].BuildDiscardFree(cardPlayed, DiscardedCards))
                        {
                            Log.Debug("<BuildDiscardFree OK>");
                        }
                        else
                        {
                            Log.Debug("<BuildDiscardFree NOK>");
                            Log.Error("SHOULD NOT HAPPEN");
                        }
                    }
                    else
                    {
                        Log.Debug("No cards in Discarded Cards list");
                        // disable discardFree effect even if no card were played
                        Players[discardFreePlayer.Id].DiscardFree = false;
                    }

                    // reset player ready status
                    PlayersReady[discardFreePlayer.Id] = false;
                }
                else
                    HandlePlayerCommand(p, playerCommand, p.HandCards);

                // reset PlayersReady status for player
                PlayersReady[p.Id] = false;
            }
            Turn++;

            // if on the last turn playSeventh effect active for some player and the player hasnt chosen a move return current state
            var playSeventhPlayer = Players.FirstOrDefault(p => p.PlaySeventh == true);
            if (playSeventhPlayer != null)
            {
                if ((Turn == 6 || Turn == 13 || Turn == 20) &&
                    PlayersReady[playSeventhPlayer.Id] == false)
                {
                    return this;
                }
            }
            // increment turn even if playSeventh effect not active but 6, 13, 20 turn (technically a 7th turn which is considered an extra turn)
            else if (Turn == 6 || Turn == 13 || Turn == 20)
                Turn++;

            // discard last card in hand if turn is 7 14 and 21 and all palyer commands were handled (PlayersReady all false)
            if (Turn % 7 == 0 && Turn != 0 && !PlayersReady.Contains(true))
            {
                // transfer the remaining card in each player's hand to the discarded card list
                foreach (Player p in Players)
                {
                    List<Card> cards = p.HandCards;
                    // cards must be size 1!!
                    for (int i = 0; i < cards.Count; i++)
                        DiscardedCards.Add(cards[i]);
                }
            }

            // if at the end of a turn discardFree effect active for some player and the player hasnt chosen a move return current state 
            discardFreePlayer = Players.FirstOrDefault(p => p.DiscardFree == true);
            if (discardFreePlayer is not null &&
                //Turn % 7 == 0 && Turn != 0 &&
                PlayersReady[discardFreePlayer.Id] == false)
            {
                Turn--;
                return this;
            }

            // if end of an era (turns 7 14 and 21) and all player moves were handled (PlayersReady all false)
            // calculate battle tokens, reset OlympiaA2 effect if needed, give new Era cards to players
            if (Turn % 7 == 0 && Turn != 0 && !PlayersReady.Contains(true))
            {
                foreach (Player p in Players)
                {
                    // reset Olympia A stage 2 effect flag
                    if (p.Board.Id == WonderId.OlympiaA && p.Board.Stage >= 2)
                        p.FreeCardOnce = true;
                    else
                        p.FreeCardOnce = false;
                    // calculate Victory and Defeat tokens 
                    p.Battle(Era);
                }

                Era++;
                if (Era >= 4)
                {
                    return this;
                }

                // give new Era cards to players 
                GiveCards();
            }
            // rotate hand cards if turn and extra turn is complete (at this point no players have chosen a move)
            else if (!PlayersReady.Contains(true))
            {
                RotateHandCards();
            }

            return this;
        }

        public bool CompletedTurn(Game prevGame)
        {
            bool completedTurn = false;
            bool handCardsNotChanged;
            bool playedCardsNotChanged;

            foreach (Player p in Players)
            {
                handCardsNotChanged = Players[p.Id].HandCards.SequenceEqual(prevGame.Players[p.Id].HandCards);
                playedCardsNotChanged = Players[p.Id].PlayedCards.SequenceEqual(prevGame.Players[p.Id].PlayedCards);

                if (!handCardsNotChanged || !playedCardsNotChanged)
                {
                    completedTurn = true;
                    break;
                }
            }

            return completedTurn;
        }

        public List<List<Command>> GetPossibleMovesForAllPlayersPruned()
        {
            List<Command>[] playersLegalMoves = new List<Command>[this.Players.Count];
            List<List<Command>> legalMoves = new();

            for (int i = 0; i < Players.Count; i++)
            {
                playersLegalMoves[i] = PlayerLegalMovesPruned(Players[i]);
            }


            GetAllCombinationsRecursive(playersLegalMoves, 0, new List<Command>(), legalMoves);


            return legalMoves;
        }

        public List<List<Command>> GetPossibleMovesForAllPlayers()
        {
            List<string> actionOpt = new List<string> { "build_structure", "build_hand_free", "build_wonder", "discard", "skip_move" };
            List<Command>[] playersLegalMoves = new List<Command>[this.Players.Count];
            List<List<Command>> legalMoves = new();

            var discardFreePlayer = Players.FirstOrDefault(p => p.DiscardFree == true);
            var playSeventhPlayer = Players.FirstOrDefault(p => p.PlaySeventh == true);

            if (discardFreePlayer != null)
            {
                playersLegalMoves[discardFreePlayer.Id] = this.GetLegalMoves(discardFreePlayer.Id, discardFree: true);

                for (int i = 0; i < Players.Count; i++)
                    if (i != discardFreePlayer.Id)
                        playersLegalMoves[i] = new List<Command>() { new Command(actionOpt[4], null), };
            }
            else if (playSeventhPlayer != null &&
                    (this.Turn == 6 ||
                    this.Turn == 13 ||
                    this.Turn == 20))
            {
                playersLegalMoves[playSeventhPlayer.Id] = this.GetLegalMoves(playSeventhPlayer.Id, playSeventh: true);

                for (int i = 0; i < Players.Count; i++)
                    if (i != playSeventhPlayer.Id)
                        playersLegalMoves[i] = new List<Command>() { new Command(actionOpt[4], null), };
            }
            else
            {
                for (int i = 0; i < Players.Count; i++)
                {
                    playersLegalMoves[i] = this.GetLegalMoves(Players[i].Id);
                }
            }


            GetAllCombinationsRecursive(playersLegalMoves, 0, new List<Command>(), legalMoves);


            return legalMoves;
        }

        private List<Command> PlayerLegalMovesPruned(Player player)
        {
            List<Command> legalMoves = new();

            if (player.DiscardFree)
                legalMoves = this.GetLegalMovesPrunedDiscards(player.Id, discardFree: true);
            // if currentPlayer PlaySeveth is true and gameState.turn == 6 13 20, then he has to expand on last card all actions
            else if (player.PlaySeventh &&
                    (this.Turn == 6 ||
                    this.Turn == 13 ||
                    this.Turn == 20))
                legalMoves = this.GetLegalMovesPrunedDiscards(player.Id, playSeventh: true);
            // if currentPlayer FreeCardOnce is true, then he can also expand on building all hand cards free
            // if regular move, then currentPlayer has to expand on build action with playable cards or discard, build wonder actions with any hand cards
            else
                legalMoves = this.GetLegalMovesPrunedDiscards(player.Id);

            return legalMoves;
        }

        private static void GetAllCombinationsRecursive(List<Command>[] arrayOfLists, int currentIndex, List<Command> currentCombination, List<List<Command>> allCombinations)
        {
            if (currentIndex == arrayOfLists.Length)
            {
                allCombinations.Add(new List<Command>(currentCombination));
                return;
            }

            foreach (var command in arrayOfLists[currentIndex])
            {
                currentCombination.Add(command);
                GetAllCombinationsRecursive(arrayOfLists, currentIndex + 1, currentCombination, allCombinations);
                currentCombination.RemoveAt(currentCombination.Count - 1);
            }
        }

        public void ReleaseGameMemory()
        {
            foreach (var player in Players)
            {
                player.HandCards.Clear();
                player.PlayableCards.Clear();
                player.PlayedCards.Clear();
                player.UsedOnDemandResource.Clear();
                player.Resources.Clear();
                player.PlayerEast = null;
                player.PlayerWest = null;
                player.Board = null;
            }
            this.Players.Clear();
            Decks[0].Clear();
            Decks[0].Clear();
            Decks[0].Clear();
            DiscardedCards.Clear();
        }
    }
}
