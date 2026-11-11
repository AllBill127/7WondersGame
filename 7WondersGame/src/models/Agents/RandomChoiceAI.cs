namespace _7WondersGame.src.models.Agents
{
    /// <summary>A random action among the possible ones, then a random card for it.</summary>
    public class RandomChoiceAI : ActionThenCardAgent
    {
        protected override ActionType ChooseAction(GameView view)
        {
            var options = new List<ActionType> { ActionType.Build, ActionType.BuildFreeOnce, ActionType.BuildWonder, ActionType.Discard };
            if (view.PlayableCards.Count == 0)
                options.Remove(ActionType.Build);
            if (!view.CanBuildWonder)
                options.Remove(ActionType.BuildWonder);
            if (!view.Me.FreeCardOnce)
                options.Remove(ActionType.BuildFreeOnce);
            return options[view.Random.Next(options.Count)];
        }

        protected override Card? ChooseCard(GameView view, ActionType action, List<Card> cards) =>
            cards.Count == 0 ? null : cards[view.Random.Next(cards.Count)];
    }
}
