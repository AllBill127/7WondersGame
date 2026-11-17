using System.Text;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Effects.Effects;
using _7WondersGame.Engine.Effects.Interfaces;

namespace _7WondersGame.Engine.Cards.Entities;

public sealed class Card : IEquatable<Card>
{
    public CardId Id { get; }
    public string Name { get; }
    public CardType Type { get; }
    public int Era { get; }
    public CardId FreeWithId { get; }
    public IReadOnlyDictionary<Resource, int> Cost { get; }

    /// <summary>What the card does when built and at the end of the game (E3).</summary>
    public IReadOnlyList<IEffect> Effects { get; }

    /// <summary>Its either/or production for its owner as a bit mask (bit i = resource i), or 0 (see <see cref="OneOf"/>).</summary>
    public int ChoiceMask { get; }

    /// <summary>The part of <see cref="ChoiceMask"/> a neighbour can buy.</summary>
    public int SellableChoiceMask { get; }

    /// <summary>Science symbols of the owner's choice it gives (Scientists Guild).</summary>
    public int ScienceWildcards { get; }

    /// <summary>Copies in the deck for 3, 4, 5, 6 and 7 players.</summary>
    private readonly int[] _copies;

    public Card(CardId id, string name, CardType type, int era, CardId freeWithId, int[] cost, int[] copies, params IEffect[] effects)
    {
        Id = id;
        Name = name;
        Type = type;
        Era = era;
        FreeWithId = freeWithId;
        _copies = copies.ToArray();
        Effects = effects;
        ChoiceMask = effects.OfType<OneOf>().Aggregate(0, (mask, e) => mask | e.Mask);
        SellableChoiceMask = effects.OfType<OneOf>().Where(e => e.Sellable).Aggregate(0, (mask, e) => mask | e.Mask);
        ScienceWildcards = effects.OfType<ScienceWildcard>().Count();
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

    /// <summary>How many copies of this card go into the deck with <paramref name="players"/> players (3 to 7).</summary>
    public int CopiesFor(int players)
    {
        if (players < 3 || players > 7)
            throw new ArgumentOutOfRangeException(nameof(players), players, "7 Wonders is played by 3 to 7 players.");
        return _copies[players - 3];
    }

    /// <summary>
    /// The rulebook's no-duplicates rule: a city can't hold two cards with the same name
    /// (Loom from age I and Loom from age II are different cards with the same name).
    /// </summary>
    public bool SameNameAs(Card other) => Name == other.Name;

    /// <summary>Cards are the same card when they have the same <see cref="Id"/> (E6).</summary>
    public bool Equals(Card? other) => other is not null && Id == other.Id;

    public override bool Equals(object? obj) => Equals(obj as Card);

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        StringBuilder cardStr = new StringBuilder();
        cardStr.AppendLine($"Id: {Id}   Name: {Name}    Era: {Era}  Copies (3-7 players): {string.Join(' ', _copies)}");
        cardStr.AppendLine($"Type: {Type}   FreeWith: {FreeWithId}");
        return cardStr.ToString();
    }
}
