namespace _7WondersGame.Cli.Experiments;

/// <summary>Which deal, seating and sweep value game <see cref="Index"/> of a run plays.</summary>
/// <param name="Rotation">The line-up moved this many seats on (0 = as listed).</param>
/// <param name="Value">The value of the range setting, if there is one.</param>
public readonly record struct GameSlot(int Index, int Step, int Deal, int Rotation, double? Value);
