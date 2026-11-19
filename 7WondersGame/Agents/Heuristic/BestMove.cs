using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Moves.Enums;

namespace _7Wonders.Agents.Heuristic;

/// <summary>The best move so far (none: Skip): the first one with the highest value.</summary>
internal struct BestMove
{
    private double _value;

    /// <summary>No move yet: every move considered is better.</summary>
    public BestMove()
    {
        Action = default;
        Card = null;
        _value = double.NegativeInfinity;
    }

    /// <summary>The best move's action.</summary>
    public MoveAction Action { get; private set; }

    /// <summary>The best move's card; null while no move was considered.</summary>
    public Card? Card { get; private set; }

    /// <summary>Takes the move if it is worth more than the best so far (a tie keeps the earlier one).</summary>
    public void Consider(MoveAction action, Card card, double value)
    {
        if (value > _value)
        {
            Action = action;
            Card = card;
            _value = value;
        }
    }
}
