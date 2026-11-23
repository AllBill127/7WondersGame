using _7Wonders.Agents.Common.Entities;
using _7Wonders.Agents.Common.Interfaces;
using _7Wonders.Agents.RandomChoice;
using _7Wonders.Engine.Moves.Dtos;

namespace _7Wonders.Tests.TestHelpers;

/// <summary>Plays randomly and keeps every move it chose, in order.</summary>
internal sealed class CountingAgent : IAgent
{
    private readonly RandomAgent _random = new();

    /// <summary>Every move chosen so far, one per decision.</summary>
    public List<Move> Choices { get; } = new();

    /// <summary>Chooses a random move and keeps it.</summary>
    public Move Choose(GameView view)
    {
        Move move = _random.Choose(view);
        Choices.Add(move);
        return move;
    }
}
