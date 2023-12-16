using _7WondersGame.src.models.Wonders;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGame.src.models.Players
{
    public class PriorityRandomChoiceAI : Player
    {
        public PriorityRandomChoiceAI(int id) : base(id)
        {
        }

        public override object DeepCopy()
        {
            PriorityRandomChoiceAI copy = new PriorityRandomChoiceAI(this.Id)
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
                WonderRawCheap = this.WonderRawCheap,
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
            int seed = (int)(DateTime.Now.Ticks & 0xFFFFFFFF);
            Random rng = new Random(seed);

            List<string> comandOpt = new List<string> { "build_structure", "build_hand_free", "build_wonder", "discard" };
            string subcommand;
            Card? argument;

            // choose less random subcomand from available
            //// opt 1 build cards first
            //if (this.FreeCardOnce)
            //    subcommand = comandOpt[1];
            //else if (this.PlayableCards.Count > 0)
            //    subcommand = comandOpt[0];
            //else if (this.PlayableCards.Count == 0 && this.CanBuildWonder)
            //    subcommand = comandOpt[2];
            //else
            //    subcommand = comandOpt[3];

            // opt 2 build wonder first
            if (this.CanBuildWonder)
                subcommand = comandOpt[2];
            else if (this.FreeCardOnce)
                subcommand = comandOpt[1];
            else if (this.PlayableCards.Count > 0)
                subcommand = comandOpt[0];
            else
                subcommand = comandOpt[3];

            //// opt 3 build wonder first then discard all cards
            //if (this.FreeCardOnce)
            //    subcommand = comandOpt[1];
            //else if (this.CanBuildWonder)
            //    subcommand = comandOpt[2];
            //else if (this.PlayableCards.Count > 0 &&
            //    this.Board.Stage < 2)
            //    subcommand = comandOpt[0];
            //else
            //    subcommand = comandOpt[3];

            //// opt 4 build wonder first but build Babylon B 2 on turn 19
            //if (this.CanBuildWonder)
            //{
            //    if (this.Board.Id == WonderId.BabylonB)
            //    {
            //        if (this.Board.Stage < 1 ||
            //            g.Game.Turn == 19)
            //            subcommand = comandOpt[2];
            //        else
            //            subcommand = comandOpt[3];
            //    }
            //    else
            //    {
            //        subcommand = comandOpt[2];
            //    }
            //}
            //else if (this.FreeCardOnce)
            //    subcommand = comandOpt[1];
            //else if (this.PlayableCards.Count > 0)
            //    subcommand = comandOpt[0];
            //else
            //    subcommand = comandOpt[3];

            // choose random card for move
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

            // choose random card for move
            if (cardsList.Count == 0)
            {
                return null;
            }
            int choice = rng.Next(cardsList.Count);
            return cardsList[choice];
        }
    }
}
