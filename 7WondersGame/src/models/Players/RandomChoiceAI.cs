using _7WondersGame.src.models.Wonders;

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
                Resources = this.Resources.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            return copy;
        }

        public override void ChooseMoveCommand(Game g)
        {
            Random rng = g.AgentRandom(this.Id);

            List<ActionType> comandOpt = new List<ActionType> { ActionType.Build, ActionType.BuildFreeOnce, ActionType.BuildWonder, ActionType.Discard };
            int choice;
            ActionType subcommand;
            Card? argument;

            // remove impossible subcommands
            if (!(this.PlayableCards.Count > 0))
                comandOpt.Remove(ActionType.Build);
            if (!this.CanBuildWonder)
                comandOpt.Remove(ActionType.BuildWonder);
            if (!this.FreeCardOnce)
                comandOpt.Remove(ActionType.BuildFreeOnce);

            // choose random subcomand from available
            choice = rng.Next(comandOpt.Count);
            subcommand = comandOpt[choice];

            // choose card for the move
            if (subcommand == ActionType.Build)
            {
                argument = ChooseMoveCard(g, subcommand, this.PlayableCards);
            }
            else if (subcommand == ActionType.BuildFreeOnce)
            {
                // get new playable cards using wonder effect
                this.PlayableCards = this.GetPlayableCards(this.HandCards, playAllFree: true);
                // if any playable cards then choose card; otherwise try discarding
                if (PlayableCards.Count > 0)
                    // build hand free
                    argument = ChooseMoveCard(g, subcommand, this.PlayableCards);
                else
                {
                    subcommand = ActionType.Discard;
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
                g.PlayerCommands[this.Id] = new Command(ActionType.Skip, null);
            g.PlayersReady[this.Id] = true;
        }

        public override Card? ChooseMoveCard(Game game, ActionType action, List<Card> cardsList)
        {
            Random rng = game.AgentRandom(this.Id);

            // choose random card for move
            if (cardsList.Count == 0)
                return null;
            int choice = rng.Next(cardsList.Count);
            return cardsList[choice];
        }
    }
}
