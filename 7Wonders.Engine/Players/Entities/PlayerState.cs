using _7Wonders.Engine.Cards.Entities;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Enums;
using _7Wonders.Engine.Effects.Interfaces;
using _7Wonders.Engine.Games.Entities;
using _7Wonders.Engine.Rules.Services;
using _7Wonders.Engine.Wonders.Entities;

namespace _7Wonders.Engine.Players.Entities;

/// <summary>
/// One player's city and hand: data only. The rules that read and change it are the static services in
/// Engine/Rules/Services (<see cref="BuildRules"/>, <see cref="MovePlanning"/>, <see cref="MoveResolution"/>,
/// <see cref="Scoring"/> and others). The engine doesn't know who decides the player's moves: whoever plays the
/// seat submits them to the game.
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

    /// <summary>
    /// The player's wonder board and how many of its stages are built. Throws until the game is set up.
    /// </summary>
    public WonderBoardProgress Wonder
    {
        get
        {
            if (_wonder == null)
            {
                throw NotSetUp("wonder board");
            }
            return _wonder;
        }
        set
        {
            _wonder = value;
        }
    }

    private WonderBoardProgress? _wonder;

    /// <summary>The cards in the player's hand this turn.</summary>
    public List<Card> Hand { get; set; } = new();

    /// <summary>The cards built in the player's city, in building order.</summary>
    public List<Card> BuiltCards { get; set; } = new();

    /// <summary>The victory points won in battles.</summary>
    public int VictoryTokens { get; set; }

    /// <summary>The battles lost (each is -1 point).</summary>
    public int DefeatTokens { get; set; }

    /// <summary>Set by <see cref="Scoring.CalculateScore"/>.</summary>
    public int VictoryPoints { get; set; }

    /// <summary>
    /// The lasting abilities the built cards gave (trading posts, Marketplace). The board's come from its stage;
    /// check both with <see cref="PlayerAbilities.Has"/>.
    /// </summary>
    public Ability Abilities { get; set; } = Ability.None;

    // One-use effects (see docs/08-special-effects.md)
    /// <summary>Olympia A stage 2: this era's free card is still unused. Re-armed at the start of each era.</summary>
    public bool FreeBuildAvailable { get; set; }

    /// <summary>
    /// The card or stage just built whose effect gives a free build from the discard pile at the end of this turn
    /// (Halikarnassos); null if there is none.
    /// </summary>
    public IEffectSource? DiscardPileBuildSource { get; set; }

    /// <summary>Whether a free build from the discard pile is due at the end of this turn.</summary>
    public bool DiscardPileBuildPending
    {
        get
        {
            return DiscardPileBuildSource != null;
        }
    }

    /// <summary>Production, science symbols, coins and shields.</summary>
    public ResourceCounts Resources { get; set; } = new() { [Resource.Coins] = 3 };

    /// <summary>The neighbour to the east. Throws until the players are seated.</summary>
    public PlayerState EastNeighbour
    {
        get
        {
            if (_eastNeighbour == null)
            {
                throw NotSetUp("east neighbour");
            }
            return _eastNeighbour;
        }
        set
        {
            _eastNeighbour = value;
        }
    }

    private PlayerState? _eastNeighbour;

    /// <summary>The neighbour to the west. Throws until the players are seated.</summary>
    public PlayerState WestNeighbour
    {
        get
        {
            if (_westNeighbour == null)
            {
                throw NotSetUp("west neighbour");
            }
            return _westNeighbour;
        }
        set
        {
            _westNeighbour = value;
        }
    }

    private PlayerState? _westNeighbour;

    /// <summary>The error for reading a part of the player the game's setup hasn't given yet.</summary>
    private InvalidOperationException NotSetUp(string part)
    {
        return new InvalidOperationException($"Seat {Seat} has no {part} yet: set up the game first.");
    }

    /// <summary>
    /// A copy of everything. The copy's neighbours are still this player's neighbours; a copy of a whole table
    /// (<see cref="Game.DeepCopy"/>) points them at the copied players.
    /// </summary>
    public PlayerState Clone()
    {
        PlayerState copy = (PlayerState)MemberwiseClone();
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
        FreeBuildAvailable = source.FreeBuildAvailable;
        DiscardPileBuildSource = source.DiscardPileBuildSource;
        Resources.CopyFrom(source.Resources);
        EastNeighbour = source.EastNeighbour;
        WestNeighbour = source.WestNeighbour;
    }

    /// <summary>
    /// A copy of every player at a table (indexed by seat), with the neighbours pointing at the copies.
    /// </summary>
    public static List<PlayerState> CloneTable(IReadOnlyList<PlayerState> players)
    {
        List<PlayerState> copies = new(players.Count);
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
