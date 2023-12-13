using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGame.src.models
{
    public class Game
    {
        public List<Player> Players { get; set; }
        int NumPlayers { get; set; } = 3;
        public int Era { get; set; } = 1;
        // 0, 7 , 14, 21 are new era turns (0 being the start of the game)
        public int Turn { get; set; } = 0;
        List<Wonder> Wonders { get; set; }
        List<Card>[] Decks { get; set; } 
        public List<Card> DiscardedCards { get; set; }

        // game loop data
        public Command[] PlayerCommands { get; set; }
        public bool[] PlayersReady { get; set; }

        public Game()
        {
            Players = new();
            Wonders = new();
            Decks = new List<Card>[3];
            for (int i = 0; i < Decks.Length; ++i)
                Decks[i] = new List<Card>();
            DiscardedCards = new();

            // init with default size; changes in InitGame()
            PlayerCommands = new Command[NumPlayers];
            PlayersReady = new bool[NumPlayers];
        }

        public Game DeepCopy()
        {
            Game copy = new Game();

            // fully copy players
            // copy player data
            foreach (Player p in Players)
                copy.Players.Add((Player)p.DeepCopy());
            // set player neighbors
            copy.NumPlayers = this.NumPlayers;
            copy.SetPlayerNeighbors();

            copy.Era = this.Era;
            copy.Turn = this.Turn;

            foreach (Wonder w in Wonders)
                copy.Wonders.Add((Wonder)w.DeepCopy());

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
            CreateWonders();
            GiveWonders();
        }

        private void SetPlayerNeighbors()
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
            Random rng = new Random();

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

            //Log.Debug("Decks: {deck1}, {deck2}, {deck3}", Decks[0], Decks[1], Decks[2]);

            // Shuffle decks
            Decks[0] = Decks[0].OrderBy(a => rng.Next()).ToList();
            Decks[1] = Decks[1].OrderBy(a => rng.Next()).ToList();
            Decks[2] = Decks[2].OrderBy(a => rng.Next()).ToList();

            //Log.Debug("SHUFFELED Decks: {deck1}, {deck2}, {deck3}", Decks[0], Decks[1], Decks[2]);
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

        private void CreateWonders()
        {
            Wonders.Add(new GizahA());
            Wonders.Add(new BabylonA());
            Wonders.Add(new OlympiaA());
            Wonders.Add(new RhodosA());
            Wonders.Add(new EphesosA());
            Wonders.Add(new AlexandriaA());
            Wonders.Add(new HalikarnassosA());
            Wonders.Add(new GizahB());
            Wonders.Add(new BabylonB());
            Wonders.Add(new OlympiaB());
            Wonders.Add(new RhodosB());
            Wonders.Add(new EphesosB());
            Wonders.Add(new AlexandriaB());
            Wonders.Add(new HalikarnassosB());
        }

        private void GiveWonders()
        {
            Random rng = new Random();
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
                        player.Board = Wonders[n];
                        player.Resources[player.Board.Production] = 1;
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

        void NextTurn()
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
                        ExtraLoop(player, playSeventh:true);
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
                    ExtraLoop(player, discardFree:true);
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
                player_deck = p1.HandCards.Select(e => e).ToList();

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
                neighbor_deck = neighbor.HandCards.Select(e => e).ToList();
                // change card list references
                neighbor.HandCards = player_deck;
                // go to next player
                p = neighbor;
                // set new list reference that is the neighbors hand card list copy
                player_deck = neighbor_deck;
            } while (p1 != p);
        }

        public void Loop(string matchLogSheet = "")
        {
            while (Turn < 21)
            {
                // TEST:
                if (Turn == 19)
                { }

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

                    // TEST:
                    if (player.FreeCardOnce && player.Board.Id != WonderId.OlympiaA)
                    { }

                    if (player.DiscardFree &&
                        (player.Board.Id == WonderId.HalikarnassosA && player.Board.Stage != 2 ||
                        player.Board.Id == WonderId.HalikarnassosB && player.Board.Stage < 1))
                    { }

                    // handle command for player
                    HandlePlayerCommand(player, playerCommand, player.HandCards);

                    // TEST:
                    if (player.FreeCardOnce && player.Board.Id != WonderId.OlympiaA)
                    { }

                    if (player.DiscardFree &&
                        (player.Board.Id == WonderId.HalikarnassosA && player.Board.Stage < 2 ||
                        player.Board.Id == WonderId.HalikarnassosB && player.Board.Stage < 1))
                    { }

                    // reset PlayersReady status for player
                    PlayersReady[player.Id] = false;
                }

                // Moves the game to the next turn, processes DiscardFree and PlaySeventh effects
                NextTurn();
            }

            //calculate scores
            foreach (Player player in Players)
            {
                // Copies a neighbor guild before scoring if the player has the ability to do so.
                player.CopyGuild(); 
                player.CalculateScore();
                Log.Debug("results Player {PlayerId} score: {PlayerScore}", player.Id, player.VictoryPoints);
            }

            // match_log_results after end game
            if (!matchLogSheet.Equals(""))
            {
                //Filer.WriteMatchLog(matchLogSheet, Players);
                //Filer.WriteMatchLogThreadSafe(matchLogSheet, Players);
                //_ = Filer.WriteMatchLogAsync(matchLogSheet, Players.Select(e => e).ToList());
                Filer.WriteMatchLogBuffered(matchLogSheet, Players.Select(e => e).ToList());
            }

            // NOTE:
            //      maybe consider releasing all used memmory except player scores
            //      save player scores to a separate list and release everything else in game state
            //      changes need applying in MCTS to account for player score 
        }

        public void ExtraLoop(Player player, bool playSeventh = false, bool discardFree = false)
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

        public Player? ApplyMove(Player player, Command playerMove)
        {
            Player? newPlayer = null;

            // if not all players are ready then simply set move and player ready
            if (PlayersReady.Contains(false))
            {
                PlayerCommands[player.Id] = playerMove;
                PlayersReady[player.Id] = true;
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
                    return playSeventhPlayer;
                }
            }
            // increment turn even if playSeventh effect not active but 6, 13, 20 turn (technically a 7th turn which is considered an extra turn)
            else if (Turn == 6 || Turn == 13 || Turn == 20)
                Turn++;

            // apply PlaySeventh extra move if current player from mcts expansion has this effect and its time to play it out
            if (Turn == 6 || Turn == 13 || Turn == 20)
            {
                if (PlayersReady[player.Id] == true && 
                    player.PlaySeventh == true)
                {
                    // handle command for player
                    HandlePlayerCommand(Players[player.Id], playerMove, Players[player.Id].HandCards);

                    // reset PlayersReady status for player
                    PlayersReady[player.Id] = false;

                    Turn++;
                }
            }

            // discard last card in hand if turn is 7 14 and 21
            if (Turn % 7 == 0 && Turn != 0)
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
            if (PlayersReady[player.Id] == true &&
                player.DiscardFree == true)
            {
                Card? cardPlayed = playerMove.Argument;
                if (cardPlayed != null)
                {
                    // build the card for free
                    if (Players[player.Id].BuildDiscardFree(cardPlayed, DiscardedCards))
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
                    Players[player.Id].DiscardFree = false;
                }

                // reset player ready status
                PlayersReady[player.Id] = false;
            }

            // if at the end of age discardFree effect active for some player and the player hasnt chosen a move return current state and set new player to the DiscardFree player
            var discardFreePlayer = Players.FirstOrDefault(p => p.DiscardFree == true);
            if (discardFreePlayer is not null &&
                Turn % 7 == 0 && Turn != 0 &&
                PlayersReady[discardFreePlayer.Id] == false)
            {
                return discardFreePlayer;
            }

            // if end of an era (turns 7 14 and 21)
            // calculate battle tokens, reset OlympiaA2 effect if needed, give new Era cards to players
            if (Turn % 7 == 0 && Turn != 0)
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
                    return null;
                }

                // give new Era cards to players 
                GiveCards();
            }
            // rotate hand cards if turn and extra turn is complete (at this point no players have chosen a move)
            else if (!PlayersReady.Contains(true))
            {
                RotateHandCards();
            }

            // return next node player
            if (Turn % 7 == 0 && Turn != 0)
            {
                // set new player to first so the next turn cycle can be explored
                // NOTE:    not sure if this is valid because this should be reset to root node player, but the turn cycle will still be completed so at that point we should calculate the uct
                newPlayer = (Player)Players[0].DeepCopy();
            }
            else
            {
                // return next player in turn cycle 
                int newPlayerIdx = player.Id - 1 < 0 ? Players.Count - 1 : player.Id - 1;
                // deep copy player without his neighbors
                // neighbors arent used with this variable so it isnt neccessary
                newPlayer = (Player)Players[newPlayerIdx].DeepCopy();
            }
            
            return newPlayer;
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

        public int GetCurrentScoreForPlayer(int playerId)
        {
            Player player = (Player)Players[playerId].DeepCopy();

            // calculate player score
            player.Battle(Era);
            player.CopyGuild();
            int score = player.CalculateScore();

            // NOTE:    probabbly should reward having highest score in the game or consider science / war potential

            return score;
        }
    }
}
