using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Wonders.Entities;

namespace _7WondersGame.Engine.Players.Entities;

/// <summary>
/// One player's city and hand: data only. The rules that read and change it are in <see cref="Rules"/>; who
/// decides the player's moves is the game's <see cref="Agents.IAgent"/> for that seat.
/// <see cref="Clone"/> is the one place a player is copied.
/// </summary>
public sealed class PlayerState
{
    /// <summary>The player at seat <paramref name="seat"/>, with 3 coins and nothing else yet.</summary>
    public PlayerState(int seat)
    {
        Seat = seat;
    }

    /// <summary>The seat: Game.Players[Seat] is this player.</summary>
    public int Seat { get; }

    /// <summary>The player's wonder board and how many of its stages are built.</summary>
    public WonderBoardProgress Wonder { get; set; } = null!;

    /// <summary>The cards in the player's hand this turn.</summary>
    public List<Card> Hand { get; set; } = new();

    /// <summary>The cards built in the player's city, in building order.</summary>
    public List<Card> BuiltCards { get; set; } = new();

    /// <summary>The victory points won in battles.</summary>
    public int VictoryTokens = 0;

    /// <summary>The battles lost (each is -1 point).</summary>
    public int DefeatTokens = 0;

    /// <summary>Set by <see cref="Rules.CalculateScore"/>.</summary>
    public int VictoryPoints = 0;

    /// <summary>
    /// The lasting abilities the built cards gave (trading posts, Marketplace). The board's come from its stage;
    /// check both with <see cref="Rules.Has"/>.
    /// </summary>
    public Ability Abilities = Ability.None;

    // One-use effects (see docs/08-special-effects.md)
    /// <summary>Olympia A stage 2: this era's free card is still unused. Re-armed at the start of each era.</summary>
    public bool OlympiaFreeBuildAvailable = false;

    /// <summary>
    /// Halikarnassos: a free build from the discard pile at the end of this turn, from the stage just built.
    /// </summary>
    public bool HalikarnassosDiscardBuildPending = false;

    /// <summary>Production, science symbols, coins and shields.</summary>
    public ResourceCounts Resources = new() { [Resource.Coins] = 3 };

    /// <summary>The neighbour to the east.</summary>
    public PlayerState EastNeighbour { get; set; } = null!;

    /// <summary>The neighbour to the west.</summary>
    public PlayerState WestNeighbour { get; set; } = null!;

    /// <summary>
    /// A copy of everything. The copy's neighbours are still this player's neighbours; a copy of a whole table
    /// (<see cref="Game.DeepCopy"/>) points them at the copied players.
    /// </summary>
    public PlayerState Clone()
    {
        var copy = (PlayerState)MemberwiseClone();
        copy.Wonder = Wonder.Clone();
        copy.Hand = new List<Card>(Hand);
        copy.BuiltCards = new List<Card>(BuiltCards);
        copy.Resources = Resources.Clone();
        return copy;
    }

    /// <summary>
    /// Makes this player a copy of <paramref name="source"/> again (as <see cref="Clone"/> would), reusing its own
    /// lists and counts: for a heuristic that tries a move on a copy, scores it and tries the next one, thousands
    /// of times a second in a search's rollouts. Both must be the same seat. A new field must be copied here too
    /// (a test checks that every field is).
    /// </summary>
    public void CopyFrom(PlayerState source)
    {
        if (source.Seat != Seat)
        {
            throw new ArgumentException($"Seat {Seat} can't copy seat {source.Seat}.", nameof(source));
        }

        // the same board only needs its stage counter
        if (Wonder.Id == source.Wonder.Id)
        {
            Wonder.BuiltStageCount = source.Wonder.BuiltStageCount;
        }
        else
        {
            Wonder = source.Wonder.Clone();
        }

        Hand.Clear();
        Hand.AddRange(source.Hand);
        BuiltCards.Clear();
        BuiltCards.AddRange(source.BuiltCards);
        VictoryTokens = source.VictoryTokens;
        DefeatTokens = source.DefeatTokens;
        VictoryPoints = source.VictoryPoints;
        Abilities = source.Abilities;
        OlympiaFreeBuildAvailable = source.OlympiaFreeBuildAvailable;
        HalikarnassosDiscardBuildPending = source.HalikarnassosDiscardBuildPending;
        Resources.CopyFrom(source.Resources);
        EastNeighbour = source.EastNeighbour;
        WestNeighbour = source.WestNeighbour;
    }

    /// <summary>
    /// A copy of every player at a table (indexed by seat), with the neighbours pointing at the copies.
    /// </summary>
    public static List<PlayerState> CloneTable(IReadOnlyList<PlayerState> players)
    {
        List<PlayerState> copies = new List<PlayerState>(players.Count);
        foreach (PlayerState player in players)
        {
            copies.Add(player.Clone());
        }

        foreach (PlayerState copy in copies)
        {
            copy.WestNeighbour = copies[copy.WestNeighbour.Seat];
            copy.EastNeighbour = copies[copy.EastNeighbour.Seat];
        }
        return copies;
    }
}
