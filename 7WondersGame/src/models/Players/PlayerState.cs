using _7WondersGame.src.models.Effects;
using _7WondersGame.src.models.Wonders;

namespace _7WondersGame.src.models.Players
{
    /// <summary>
    /// One player's city and hand: data only (E5). The rules that read and change it are in <see cref="Rules"/>; who
    /// decides the player's moves is the game's <see cref="Agents.IAgent"/> for that seat.
    /// <see cref="Clone"/> is the one place a player is copied.
    /// </summary>
    public sealed class PlayerState
    {
        public PlayerState(int id)
        {
            Id = id;
        }

        /// <summary>The seat: Game.Players[Id] is this player.</summary>
        public int Id { get; }

        public Wonder Board { get; set; } = null!;
        public List<Card> HandCards { get; set; } = new();
        public List<Card> PlayedCards { get; set; } = new();

        public int VictoryTokens = 0;
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
        public bool FreeCardOnce = false;
        /// <summary>Halikarnassos: a free build from the discard pile at the end of this turn, from the stage just built.</summary>
        public bool DiscardFree = false;

        public ResourceCounts Resources = new() { [Resource.Coins] = 3 };

        public PlayerState PlayerEast { get; set; } = null!;
        public PlayerState PlayerWest { get; set; } = null!;

        /// <summary>
        /// A copy of everything. The copy's neighbours are still this player's neighbours; a copy of a whole table
        /// (<see cref="Game.DeepCopy"/>) points them at the copied players.
        /// </summary>
        public PlayerState Clone()
        {
            var copy = (PlayerState)MemberwiseClone();
            copy.Board = (Wonder)Board.DeepCopy();
            copy.HandCards = HandCards.ToList();
            copy.PlayedCards = PlayedCards.ToList();
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
            if (source.Id != Id)
                throw new ArgumentException($"Seat {Id} can't copy seat {source.Id}.", nameof(source));
            if (Board.Id == source.Board.Id)
                Board.Stage = source.Board.Stage;
            else
                Board = (Wonder)source.Board.DeepCopy();
            HandCards.Clear();
            HandCards.AddRange(source.HandCards);
            PlayedCards.Clear();
            PlayedCards.AddRange(source.PlayedCards);
            VictoryTokens = source.VictoryTokens;
            DefeatTokens = source.DefeatTokens;
            VictoryPoints = source.VictoryPoints;
            Abilities = source.Abilities;
            FreeCardOnce = source.FreeCardOnce;
            DiscardFree = source.DiscardFree;
            Resources.CopyFrom(source.Resources);
            PlayerEast = source.PlayerEast;
            PlayerWest = source.PlayerWest;
        }

        /// <summary>A copy of every player at a table (indexed by seat), with the neighbours pointing at the copies.</summary>
        public static List<PlayerState> CloneTable(IReadOnlyList<PlayerState> players)
        {
            List<PlayerState> copies = players.Select(p => p.Clone()).ToList();
            foreach (PlayerState copy in copies)
            {
                copy.PlayerWest = copies[copy.PlayerWest.Id];
                copy.PlayerEast = copies[copy.PlayerEast.Id];
            }
            return copies;
        }
    }
}
