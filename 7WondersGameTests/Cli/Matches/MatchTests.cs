using _7WondersGame.Agents.Common.Interfaces;
using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Matches;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Moves.Exceptions;
using _7WondersGameTests.TestHelpers;

namespace _7WondersGameTests.Cli.Matches;

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
