using _7WondersGame.src.models.Wonders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGame.src.models.Players
{
    public class RandomChoiceAI : Player
    {
        public RandomChoiceAI(int id) : base(id) { }

        public override object DeepCopy()
        {
            RandomChoiceAI copy = new RandomChoiceAI(this.Id)
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

        public override void ChooseMoveCommand(Game g)
        {
            int seed = (int)(DateTime.Now.Ticks & 0xFFFFFFFF);
            Random rng = new Random(seed);

            List<string> comandOpt = new List<string> { "build_structure", "build_hand_free", "build_wonder", "discard" };
            int choice;
            string subcommand;
            Card? argument;

            // remove impossible subcommands
            if (!(this.PlayableCards.Count > 0))
                comandOpt.Remove("build_structure");
            if (!this.CanBuildWonder)
                comandOpt.Remove("build_wonder");
            if (!this.FreeCardOnce)
                comandOpt.Remove("build_hand_free");

            // choose random subcomand from available
            choice = rng.Next(comandOpt.Count);
            subcommand = comandOpt[choice];

            // choose card for the move
            if (subcommand.Equals("build_structure"))
            {
                argument = ChooseMoveCard(g, subcommand, this.PlayableCards);
            }
            else if (subcommand.Equals("build_hand_free"))
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
                return null;
            int choice = rng.Next(cardsList.Count);
            return cardsList[choice];
        }
    }
}
