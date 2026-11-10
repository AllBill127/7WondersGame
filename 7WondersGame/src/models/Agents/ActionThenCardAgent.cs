using _7WondersGame.src.models.Players;

namespace _7WondersGame.src.models.Agents
{
    /// <summary>
    /// The heuristic and random agents: first an action, then a card for it.
    /// <list type="bullet">
    /// <item>Build: a playable card.</item>
    /// <item>Olympia A's free card: a hand card not built yet; if every one is, a discard instead.</item>
    /// <item>Wonder stage or discard: any hand card.</item>
    /// <item>Halikarnassos' discard-pile build: a playable discard-pile card, or Skip when there is none.</item>
    /// </list>
    /// </summary>
    public abstract class ActionThenCardAgent : IAgent
    {
        public virtual string Name => GetType().Name;

        public Command Choose(GameView view)
        {
            if (view.Phase == Phase.DiscardBuild)
                return Move(ActionType.Build, ChooseCard(view, ActionType.Build, view.PlayableCards));

            PlayerState me = view.Me;
            ActionType action = ChooseAction(view);
            Card? card;
            if (action == ActionType.Build)
                card = ChooseCard(view, action, view.PlayableCards);
            else if (action == ActionType.BuildFreeOnce)
            {
                List<Card> free = me.GetPlayableCards(me.HandCards, playAllFree: true);
                if (free.Count > 0)
                    card = ChooseCard(view, action, free);
                else
                {
                    action = ActionType.Discard;
                    card = ChooseCard(view, action, me.HandCards);
                }
            }
            else
                card = ChooseCard(view, action, me.HandCards);

            return Move(action, card);
        }

        private static Command Move(ActionType action, Card? card) =>
            card == null ? new Command(ActionType.Skip, null) : new Command(action, card);

        /// <summary>Build, BuildFreeOnce (only while Olympia A's free card is unused), BuildWonder or Discard.</summary>
        protected abstract ActionType ChooseAction(GameView view);

        /// <summary>One of the cards for the action, or null to skip (also when the list is empty).</summary>
        protected abstract Card? ChooseCard(GameView view, ActionType action, List<Card> cards);
    }
}
