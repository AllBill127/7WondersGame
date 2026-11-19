using _7Wonders.Agents.HiddenInformation;

namespace _7Wonders.Agents.Mcts.Configuration.Enums;

/// <summary>What the search may know.</summary>
public enum MctsHiddenInformation
{
    /// <summary>
    /// SO-ISMCTS: each iteration searches a new guess at the hidden cards (<see cref="Determinization"/>).
    /// </summary>
    Determinized,
    /// <summary>Searches the real game, hidden cards included: a cheating baseline.</summary>
    PerfectInformation,
}
