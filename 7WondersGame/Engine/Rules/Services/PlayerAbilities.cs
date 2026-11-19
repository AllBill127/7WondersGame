using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Effects.Enums;
using _7WondersGame.Engine.Players.Entities;

namespace _7WondersGame.Engine.Rules.Services;

/// <summary>
/// The lasting abilities a player has from built cards and wonder stages (see <see cref="Ability"/>),
/// and their science wildcards.
/// </summary>
public static class PlayerAbilities
{
    /// <summary>Does a built card or wonder stage give the player this ability?</summary>
    public static bool Has(PlayerState player, Ability ability)
    {
        Ability allAbilities = player.Abilities | player.Wonder.Abilities;
        return (allAbilities & ability) != 0;
    }

    /// <summary>Science symbols of the player's choice: Scientists Guild, Babylon.</summary>
    public static int ScienceWildcards(PlayerState player)
    {
        int wildcards = player.Wonder.ScienceWildcards;
        foreach (Card card in player.BuiltCards)
        {
            wildcards += card.ScienceWildcards;
        }
        return wildcards;
    }
}
