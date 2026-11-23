using _7Wonders.Engine.Players.Entities;
using _7Wonders.Engine.Rules.Services;
using _7Wonders.Engine.Wonders.Entities;

namespace _7Wonders.Engine.Effects.Enums;

/// <summary>
/// Lasting abilities a card or wonder stage gives. A card's go into <see cref="PlayerState.Abilities"/> when it is
/// built; a wonder's are read from its built stages (<see cref="WonderBoardProgress.Abilities"/>).
/// Check them with <see cref="PlayerAbilities.Has"/>.
/// </summary>
/// <remarks>
/// <c>[Flags]</c>: each ability is one bit (<c>1 &lt;&lt; n</c> is 2 to the power n: 1, 2, 4, 8...), so a player's
/// abilities fit in one value. <c>a | b</c> holds both; <c>(abilities &amp; ability) != 0</c> tests for one. A player
/// with the Marketplace and Olympia B stage 1 has <c>0b1100</c> (12). <c>[Flags]</c> also makes <c>ToString</c> list
/// every set ability by name.
/// </remarks>
[Flags]
public enum Ability
{
    None = 0,
    /// <summary>East Trading Post: raw resources cost 1 coin from the east neighbour.</summary>
    RawMaterialsCheapFromEastNeighbour = 1 << 0,
    /// <summary>West Trading Post: raw resources cost 1 coin from the west neighbour.</summary>
    RawMaterialsCheapFromWestNeighbour = 1 << 1,
    /// <summary>Marketplace: manufactured goods cost 1 coin from both neighbours.</summary>
    ManufacturedGoodsCheapFromBothNeighbours = 1 << 2,
    /// <summary>Olympia B stage 1: raw resources cost 1 coin from both neighbours.</summary>
    RawMaterialsCheapFromBothNeighbours = 1 << 3,
    /// <summary>Alexandria A stage 2, B stage 1: one raw resource of the player's choice each turn.</summary>
    AnyRawMaterialEachTurn = 1 << 4,
    /// <summary>Alexandria B stage 2: one manufactured good of the player's choice each turn.</summary>
    AnyManufacturedGoodEachTurn = 1 << 5,
    /// <summary>Babylon B stage 2: also plays the 7th card of each era.</summary>
    PlaySeventhCard = 1 << 6,
    /// <summary>
    /// Olympia A stage 2: one card per era for free (the use is <see cref="PlayerState.FreeBuildAvailable"/>).
    /// </summary>
    FreeBuildEachEra = 1 << 7,
    /// <summary>Olympia B stage 3: copies a neighbour's guild at the end of the game.</summary>
    CopyGuild = 1 << 8,
}
