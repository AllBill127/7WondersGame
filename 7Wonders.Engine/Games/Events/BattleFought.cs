namespace _7Wonders.Engine.Games.Events;

/// <summary>One seat's two battles at the end of an era, against its west and east neighbours.</summary>
public sealed class BattleFought : GameEvent
{
    /// <summary>The event with these values.</summary>
    public BattleFought(int seat,
                        int shields,
                        int westSeat,
                        int westShields,
                        int eastSeat,
                        int eastShields,
                        int victoryTokensForWin)
    {
        Seat = seat;
        Shields = shields;
        WestSeat = westSeat;
        WestShields = westShields;
        EastSeat = eastSeat;
        EastShields = eastShields;
        VictoryTokensForWin = victoryTokensForWin;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The seat's shields.</summary>
    public int Shields { get; }

    /// <summary>The west neighbour's seat.</summary>
    public int WestSeat { get; }

    /// <summary>The west neighbour's shields.</summary>
    public int WestShields { get; }

    /// <summary>The east neighbour's seat.</summary>
    public int EastSeat { get; }

    /// <summary>The east neighbour's shields.</summary>
    public int EastShields { get; }

    /// <summary>The victory points a won battle gives in this era; a lost one takes away 1.</summary>
    public int VictoryTokensForWin { get; }
}
