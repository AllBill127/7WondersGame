using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Moves.Enums;

namespace _7WondersGame.Agents.Heuristic;

/// <summary>The best move so far (none: Skip): the first one with the highest value.</summary>
internal struct BestMove
{
    public ActionType Action;
    public Card? Card;
    private double _value;

    public BestMove() : this(default, null) { }
    private BestMove(ActionType action, Card? card) => (Action, Card, _value) = (action, card, double.NegativeInfinity);

    public void Consider(ActionType action, Card card, double value)
    {
        if (value > _value)
        {
            Action = action;
            Card = card;
            _value = value;
        }
    }
}
