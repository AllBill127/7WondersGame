namespace _7Wonders.Agents.Matches.Records;

/// <summary>One seat's final victory points and where they came from.</summary>
public sealed class SeatScore
{
    /// <summary>Seat <paramref name="seat"/>'s score.</summary>
    public SeatScore(int seat,
                     string agentName,
                     int victoryPoints,
                     int militaryPoints,
                     int coinPoints,
                     int wonderPoints,
                     int civilianPoints,
                     int commercialPoints,
                     int guildPoints,
                     int sciencePoints,
                     int coinsLeft)
    {
        Seat = seat;
        AgentName = agentName;
        VictoryPoints = victoryPoints;
        MilitaryPoints = militaryPoints;
        CoinPoints = coinPoints;
        WonderPoints = wonderPoints;
        CivilianPoints = civilianPoints;
        CommercialPoints = commercialPoints;
        GuildPoints = guildPoints;
        SciencePoints = sciencePoints;
        CoinsLeft = coinsLeft;
    }

    /// <summary>The seat.</summary>
    public int Seat { get; }

    /// <summary>The agent's name.</summary>
    public string AgentName { get; }

    /// <summary>The final victory points, the sum of the parts below.</summary>
    public int VictoryPoints { get; }

    /// <summary>The points from military tokens.</summary>
    public int MilitaryPoints { get; }

    /// <summary>The points from coins, one per three.</summary>
    public int CoinPoints { get; }

    /// <summary>The points from wonder stages.</summary>
    public int WonderPoints { get; }

    /// <summary>The points from civilian (blue) cards.</summary>
    public int CivilianPoints { get; }

    /// <summary>The points from commercial (yellow) cards.</summary>
    public int CommercialPoints { get; }

    /// <summary>The points from guilds (purple cards).</summary>
    public int GuildPoints { get; }

    /// <summary>The points from science (green cards).</summary>
    public int SciencePoints { get; }

    /// <summary>The coins the seat has at the end.</summary>
    public int CoinsLeft { get; }
}
