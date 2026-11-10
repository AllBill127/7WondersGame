namespace _7WondersGame.src.models.Agents
{
    /// <summary>What <see cref="PriorityRandomChoiceAI"/> prefers.</summary>
    public enum ActionPriority
    {
        /// <summary>A wonder stage when it can pay for one, then Olympia A's free card, then a card, else a discard.</summary>
        Wonder,

        /// <summary>Olympia A's free card, then a card, then a wonder stage, else a discard.</summary>
        Structure,
    }

    /// <summary>
    /// The action by a fixed priority, the card at random. The command line has it as PriorityWonderRandomChoiceAI and
    /// PriorityStructureRandomChoiceAI, the two classes it replaces, and results keep those names.
    /// </summary>
    public class PriorityRandomChoiceAI : RandomChoiceAI
    {
        // readonly: fixed by the name it was made under, so it isn't a setting
        private readonly ActionPriority _priority;

        public PriorityRandomChoiceAI(ActionPriority priority)
        {
            _priority = priority;
        }

        public override string Name => $"Priority{_priority}RandomChoiceAI";

        protected override ActionType ChooseAction(GameView view)
        {
            bool canBuild = view.PlayableCards.Count > 0;
            if (_priority == ActionPriority.Wonder && view.CanBuildWonder)
                return ActionType.BuildWonder;
            if (view.Me.FreeCardOnce)
                return ActionType.BuildFreeOnce;
            if (canBuild)
                return ActionType.Build;
            if (_priority == ActionPriority.Structure && view.CanBuildWonder)
                return ActionType.BuildWonder;
            return ActionType.Discard;
        }
    }
}
