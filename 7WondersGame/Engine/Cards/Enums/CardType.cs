namespace _7WondersGame.Engine.Cards.Enums;

/// <summary>A card's colour: what kind of structure it is.</summary>
public enum CardType
{
    /// <summary>Brown: raw materials (wood, ore, clay, stone).</summary>
    Materials,
    /// <summary>Grey: manufactured goods (loom, glass, papyrus).</summary>
    Manufactured,
    /// <summary>Blue: victory points.</summary>
    Civilian,
    /// <summary>Yellow: coins, trade and production.</summary>
    Commercial,
    /// <summary>Red: shields.</summary>
    Military,
    /// <summary>Green: science symbols.</summary>
    Scientific,
    /// <summary>Purple: era 3 points for what the neighbours (or the player) built.</summary>
    Guild,
};
