using _7Wonders.Agents.Common.Entities;
using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Engine.Cards.Data;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Moves.Dtos;
using _7Wonders.Engine.Moves.Enums;

namespace _7Wonders.Tests.TestHelpers;

/// <summary>Always builds the Palace, a card no hand holds in era I, so the rules refuse its first move.</summary>
internal sealed class RuleBreakingAgent : IAgent
{
    /// <summary>A build of the Palace, whatever the view shows.</summary>
    public Move Choose(GameView view)
    {
        return new Move(MoveAction.Build, CardsDataset.GameCards[CardId.Palace]);
    }
}
