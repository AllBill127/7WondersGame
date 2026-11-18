namespace _7WondersGame.Cli.ResultsAnalysis;

/// <summary>How 'analyze' puts result rows into groups.</summary>
public enum GroupBy
{
    /// <summary>One group per agent configuration (the swept agent apart).</summary>
    Player,

    /// <summary>One group per agent name.</summary>
    Agent,

    /// <summary>One group per seat.</summary>
    Seat,

    /// <summary>One group per wonder board and side.</summary>
    Wonder,
}
