using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Games.Enums;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;
using _7WondersGame.Engine.Players.Entities;
using _7WondersGame.Engine.Rules.Services;

namespace _7WondersGame.Agents.RandomChoice;

/// <summary>
/// A random action among the possible ones, then a random card for it:
/// <list type="bullet">
/// <item>Build: a playable card.</item>
/// <item>Olympia A's free card: a hand card not built yet; if every one is, a discard instead.</item>
/// <item>Wonder stage or discard: any hand card.</item>
/// <item>Halikarnassos' discard-pile build: a playable discard-pile card, or Skip when there is none.</item>
/// </list>
/// </summary>
public class RandomChoiceAI : IAgent
{
    public Command Choose(GameView view)
    {
        if (view.Phase == Phase.DiscardBuild)
            return Move(ActionType.Build, ChooseCard(view, view.PlayableCards));

        PlayerState me = view.Me;
        ActionType action = ChooseAction(view);
        Card? card;
        if (action == ActionType.Build)
            card = ChooseCard(view, view.PlayableCards);
        else if (action == ActionType.BuildFreeOnce)
        {
            List<Card> free = me.GetPlayableCards(me.HandCards, playAllFree: true);
            if (free.Count > 0)
                card = ChooseCard(view, free);
            else
            {
                action = ActionType.Discard;
                card = ChooseCard(view, me.HandCards);
            }
        }
        else
            card = ChooseCard(view, me.HandCards);

        return Move(action, card);
    }

    private static Command Move(ActionType action, Card? card) =>
        card == null ? new Command(ActionType.Skip, null) : new Command(action, card);

    /// <summary>Build, BuildFreeOnce (only while Olympia A's free card is unused), BuildWonder or Discard.</summary>
    private static ActionType ChooseAction(GameView view)
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

    /// <summary>One of the cards, or null to skip when the list is empty.</summary>
    private static Card? ChooseCard(GameView view, List<Card> cards) =>
        cards.Count == 0 ? null : cards[view.Random.Next(cards.Count)];
}
