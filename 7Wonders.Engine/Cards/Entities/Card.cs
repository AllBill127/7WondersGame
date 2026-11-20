using System.Text;
using _7Wonders.Engine.Cards.Data;
using _7Wonders.Engine.Cards.Enums;
using _7Wonders.Engine.Effects.Effects;
using _7Wonders.Engine.Effects.Interfaces;

namespace _7Wonders.Engine.Cards.Entities;

/// <summary>
/// One card of the game: what it costs, what it chains from and what it does. Card objects come from
/// <see cref="CardsDataset"/> and are shared by every game, so they never change.
/// </summary>
public sealed class Card : IEquatable<Card>, IEffectSource
{
    /// <summary>Which card this is; the two copies of a name in different eras have different ids.</summary>
    public CardId Id { get; }

    /// <summary>The name printed on the card.</summary>
    public string Name { get; }

    /// <summary>The card's colour.</summary>
    public CardType Type { get; }

    /// <summary>The era (age) whose deck it is in: 1, 2 or 3.</summary>
    public int Era { get; }

    /// <summary>The card that lets this one be built for free (chaining), or <see cref="CardId.None"/>.</summary>
    public CardId FreeIfBuiltCardId { get; }

    /// <summary>The resources and coins it costs.</summary>
    public IReadOnlyDictionary<Resource, int> Cost { get; }

    /// <summary>What the card does when built and at the end of the game.</summary>
    public IReadOnlyList<IEffect> Effects { get; }

    /// <summary>
    /// Its either/or production for its owner as a bit mask (bit i = resource i), or 0 (see <see cref="OneOf"/>).
    /// </summary>
    public int ChoiceMask { get; }

    /// <summary>The part of <see cref="ChoiceMask"/> a neighbour can buy.</summary>
    public int SellableChoiceMask { get; }

    /// <summary>Science symbols of the owner's choice it gives (Scientists Guild).</summary>
    public int ScienceWildcards { get; }

    /// <summary>Copies in the deck for 3, 4, 5, 6 and 7 players.</summary>
    private readonly int[] _copies;

    /// <summary>A card as listed in <see cref="CardsDataset"/>.</summary>
    /// <param name="cost">
    /// Wood, Ore, Clay, Stone, Loom, Glass, Papyrus, Coins (not the <see cref="Resource"/> order).
    /// </param>
    /// <param name="copies">Copies in the deck for 3, 4, 5, 6 and 7 players.</param>
    public Card(CardId id, string name, CardType type, int era, CardId freeIfBuilt, int[] cost, int[] copies,
                params IEffect[] effects)
    {
        Id = id;
        Name = name;
        Type = type;
        Era = era;
        FreeIfBuiltCardId = freeIfBuilt;
        _copies = copies.ToArray();
        Effects = effects;

        // what the effects add up to, worked out once, since the payment search reads them all the time
        foreach (IEffect effect in effects)
        {
            if (effect is OneOf choice)
            {
                ChoiceMask |= choice.Mask;
                if (choice.Sellable)
                {
                    SellableChoiceMask |= choice.Mask;
                }
            }
            if (effect is ScienceWildcard)
            {
                ScienceWildcards++;
            }
        }

        Cost = new Dictionary<Resource, int>()
        {
            { Resource.Wood,       cost[0] },
            { Resource.Ore,        cost[1] },
            { Resource.Clay,       cost[2] },
            { Resource.Stone,      cost[3] },
            { Resource.Loom,       cost[4] },
            { Resource.Glass,      cost[5] },
            { Resource.Papyrus,    cost[6] },
            { Resource.Coins,      cost[7] }
        };
    }

    /// <summary>
    /// How many copies of this card go into the deck with <paramref name="players"/> players (3 to 7).
    /// </summary>
    public int CopiesFor(int players)
    {
        if (players < 3 || players > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(players), players, "7 Wonders is played by 3 to 7 players.");
        }
        int copiesIndex = players - 3;
        return _copies[copiesIndex];
    }

    /// <summary>
    /// The rulebook's no-duplicates rule: a city can't hold two cards with the same name
    /// (Loom from age I and Loom from age II are different cards with the same name).
    /// </summary>
    public bool SameNameAs(Card other)
    {
        return Name == other.Name;
    }

    /// <summary>Cards are the same card when they have the same <see cref="Id"/>.</summary>
    public bool Equals(Card? other)
    {
        return other is not null && Id == other.Id;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Card);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>Two lines: id, name, era and copies; then type and the card it chains from.</summary>
    public override string ToString()
    {
        string copies = string.Join(' ', _copies);
        StringBuilder description = new();
        description.AppendLine($"Id: {Id}   Name: {Name}    Era: {Era}  Copies (3-7 players): {copies}");
        description.AppendLine($"Type: {Type}   FreeWith: {FreeIfBuiltCardId}");
        return description.ToString();
    }
}
