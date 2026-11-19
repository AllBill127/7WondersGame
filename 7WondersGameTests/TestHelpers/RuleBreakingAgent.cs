using _7WondersGame.Agents.Common.Entities;
using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Moves.Dtos;
using _7WondersGame.Engine.Moves.Enums;

namespace _7WondersGameTests.TestHelpers;

/// <summary>Always builds the Palace, a card no hand holds in era I, so the rules refuse its first move.</summary>
internal sealed class RuleBreakingAgent : IAgent
{
    /// <summary>A build of the Palace, whatever the view shows.</summary>
    public Move Choose(GameView view)
    {
        return new Move(MoveAction.Build, CardsDataset.GameCards[CardId.Palace]);
    }
}
