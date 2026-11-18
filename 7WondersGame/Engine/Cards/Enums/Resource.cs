namespace _7WondersGame.Engine.Cards.Enums;

/// <summary>
/// What a city counts: raw materials, manufactured goods, science symbols, coins and shields. Card costs in
/// <see cref="Data.CardsDataset"/> are listed in another order.
/// </summary>
public enum Resource
{
    // raw materials
    Wood,
    Ore,
    Clay,
    Stone,

    // manufactured goods
    Loom,
    Glass,
    Papyrus,

    // science symbols
    Gear,
    Compass,
    Tablet,

    // not produced: counted
    Coins,
    Shields,
};
