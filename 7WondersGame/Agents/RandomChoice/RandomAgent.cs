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
public class RandomAgent : IAgent
{
    /// <summary>A random move for the view's seat (see the class summary).</summary>
    public Move Choose(GameView view)
    {
        if (view.Phase == Phase.DiscardPileBuild)
        {
            Card? discardPileCard = ChooseCard(view, view.BuildableCards);
            return MoveOrSkip(MoveAction.Build, discardPileCard);
        }

        PlayerState player = view.DecidingPlayer;
        MoveAction action = ChooseAction(view);
        Card? card;
        if (action == MoveAction.Build)
        {
            card = ChooseCard(view, view.BuildableCards);
        }
        else if (action == MoveAction.BuildWithOlympiaFreeBuild)
        {
            List<Card> free = player.BuildableCards(player.Hand, everyCardFree: true);
            if (free.Count > 0)
            {
                card = ChooseCard(view, free);
            }
            else
            {
                action = MoveAction.Discard;
                card = ChooseCard(view, player.Hand);
            }
        }
        else
        {
            card = ChooseCard(view, player.Hand);
        }

        return MoveOrSkip(action, card);
    }

    /// <summary>The move with the card, or Skip when there is no card.</summary>
    private static Move MoveOrSkip(MoveAction action, Card? card)
    {
        if (card == null)
        {
            return new Move(MoveAction.Skip, null);
        }
        return new Move(action, card);
    }

    /// <summary>Build, BuildWithOlympiaFreeBuild (only while Olympia A's free card is unused), BuildWonderStage or
    /// Discard.</summary>
    private static MoveAction ChooseAction(GameView view)
    {
        var options = new List<MoveAction>
        {
            MoveAction.Build,
            MoveAction.BuildWithOlympiaFreeBuild,
            MoveAction.BuildWonderStage,
            MoveAction.Discard,
        };
        if (view.BuildableCards.Count == 0)
        {
            options.Remove(MoveAction.Build);
        }
        if (!view.CanBuildWonder)
        {
            options.Remove(MoveAction.BuildWonderStage);
        }
        if (!view.DecidingPlayer.OlympiaFreeBuildAvailable)
        {
            options.Remove(MoveAction.BuildWithOlympiaFreeBuild);
        }
        int optionIndex = view.Random.Next(options.Count);
        return options[optionIndex];
    }

    /// <summary>One of the cards, or null to skip when the list is empty.</summary>
    private static Card? ChooseCard(GameView view, List<Card> cards)
    {
        if (cards.Count == 0)
        {
            return null;
        }
        int cardIndex = view.Random.Next(cards.Count);
        return cards[cardIndex];
    }
}
