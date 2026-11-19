using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Agents.RandomChoice;
using _7Wonders.Cli.Matches;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Moves.Exceptions;
using _7Wonders.Tests.TestHelpers;

namespace _7Wonders.Tests.Cli.Matches;

/// <summary>A match seats agents at a game and plays it.</summary>
[TestClass]
public class MatchTests
{
    [TestMethod]
    public void ARefusedMove_NamesTheAgentThatChoseIt()
    {
        Game game = new(5);
        game.SetUpRandomGame(3);
        List<IAgent> agents = new() { new RandomAgent(), new RuleBreakingAgent(), new RandomAgent() };
        Match match = new(game, agents);

        InvalidOperationException refused = Assert.ThrowsException<InvalidOperationException>(() => match.Play());

        StringAssert.StartsWith(refused.Message, "Seat 1 (RuleBreakingAgent) could not Build Palace on turn 0 ");
        Assert.IsInstanceOfType(refused.InnerException, typeof(MoveNotAllowedException));
    }

    [TestMethod]
    public void AnAgentForEverySeat_IsRequired()
    {
        Game game = new(5);
        game.SetUpRandomGame(3);
        List<IAgent> agents = new() { new RandomAgent(), new RandomAgent() };

        Assert.ThrowsException<InvalidOperationException>(() => new Match(game, agents));
    }
}
