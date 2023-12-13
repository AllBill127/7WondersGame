using _7WondersGame.src.models.Wonders;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGame.src.models.Players
{
    public class DeterministicAI : Player
    {
        public DeterministicAI(int id) : base(id)
        {
        }

        public override object DeepCopy()
        {
            DeterministicAI copy = new DeterministicAI(this.Id)
            {
                Board = (Wonder)this.Board.DeepCopy(),
                HandCards = this.HandCards.Select(e => e).ToList(),
                PlayedCards = this.PlayedCards.Select(e => e).ToList(),
                PlayableCards = this.PlayableCards.Select(e => e).ToList(),

                VictoryTokens = this.VictoryTokens,
                DefeatTokens = this.DefeatTokens,
                VictoryPoints = this.VictoryPoints,
                // Babylon B stage 2
                PlaySeventh = this.PlaySeventh,
                // Olympia B stage 1
                RawCheapEast = this.RawCheapEast,
                RawCheapWest = this.RawCheapWest,
                // Marketplace card effect
                ManufCheap = this.ManufCheap,
                // Olympia A stage 2
                FreeCardOnce = this.FreeCardOnce,
                // Halikarnassos A stage 2 & B stage 1-3
                DiscardFree = this.DiscardFree,
                // Science guild effect
                ExtraScience = this.ExtraScience,
                CanBuildWonder = this.CanBuildWonder,

                // flags used to check multichoice materials only once
                UsedOnDemandResource = this.UsedOnDemandResource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Resources = this.Resources.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            return copy;
        }

        override public void ChooseMoveCommand(Game g)
        {
            List<string> comandOpt = new List<string> { "build_structure", "build_hand_free", "build_wonder", "discard" };
            string subcommand;
            Card? argument;

            // TODO: implement heuristics for choosing a move (consider discarding card for money if not enough to buy from neighbors)

            // opt 2 build wonder first
            if (this.CanBuildWonder)
                subcommand = comandOpt[2];
            else if (this.FreeCardOnce)
                subcommand = comandOpt[1];
            else if (this.PlayableCards.Count > 0)
                subcommand = comandOpt[0];
            else
                subcommand = comandOpt[3];


            // choose highest scoring card for move
            if (subcommand.Equals(comandOpt[0]))
            {
                // build structure from playable cards
                argument = ChooseMoveCard(g, subcommand, this.PlayableCards);
            }
            else if (subcommand.Equals(comandOpt[1]))
            {
                // get new playable cards using wonder effect
                this.PlayableCards = this.GetPlayableCards(this.HandCards, playAllFree: true);
                // if any playable cards then choose card; otherwise try discarding
                if (PlayableCards.Count > 0)
                    // build hand free
                    argument = ChooseMoveCard(g, subcommand, this.PlayableCards);
                else
                {
                    subcommand = "discard";
                    argument = ChooseMoveCard(g, subcommand, this.HandCards);
                }
            }
            else
            {
                // discard any card in hand
                argument = ChooseMoveCard(g, subcommand, this.HandCards);
            }

            // set the command and ready status in game
            if (argument != null)
                g.PlayerCommands[this.Id] = new Command(subcommand, argument);
            else
                g.PlayerCommands[this.Id] = new Command("skip_move", null);
            g.PlayersReady[this.Id] = true;
        }

        public override Card? ChooseMoveCard(Game game, string commandStr, List<Card> cardsList)
        {
            int seed = (int)(DateTime.Now.Ticks & 0xFFFFFFFF);
            Random rng = new Random(seed);
            Card? chosenCard;

            // choose random card for move
            if (cardsList.Count == 0)
            {
                return null;
            }

            if (game.Era == 3)
            {
                chosenCard = Age3BuildHeuristic(game, cardsList);
            }
            else if (game.Era == 2)
            {
                chosenCard = Age2BuildHeuristic(game, cardsList);
            }
            else if (game.Era == 1)
            {
                chosenCard = Age1BuildHeuristic(game, cardsList);
            }
            else
            {
                int choice = rng.Next(cardsList.Count);
                chosenCard =  cardsList[choice];
            }

            if (chosenCard == null)
            {
                return null;
            }

            return chosenCard;
        }
    }
}
