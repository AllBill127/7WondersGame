namespace _7WondersGame.src.models.Agents
{
    /// <summary>
    /// Rule-based: a wonder stage when it can pay for one, then Olympia A's free card, then a card, else a discard.
    /// The card is the one with the best era heuristic (<see cref="Heuristics"/>).
    /// </summary>
    public class DeterministicAI : ActionThenCardAgent
    {
        protected override ActionType ChooseAction(GameView view)
        {
            if (view.CanBuildWonder)
                return ActionType.BuildWonder;
            if (view.Me.FreeCardOnce)
                return ActionType.BuildFreeOnce;
            if (view.PlayableCards.Count > 0)
                return ActionType.Build;
            return ActionType.Discard;
        }

        protected override Card? ChooseCard(GameView view, ActionType action, List<Card> cards)
        {
            if (cards.Count == 0)
                return null;
            return view.Era switch
            {
                1 => view.Me.Age1BuildHeuristic(cards),
                2 => view.Me.Age2BuildHeuristic(cards),
                3 => view.Me.Age3BuildHeuristic(cards),
                _ => null,
            };
        }
    }
}
