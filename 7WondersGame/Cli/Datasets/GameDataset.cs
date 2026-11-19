namespace _7WondersGame.Cli.Datasets;

/// <summary>The contents of a dataset file: the player count and the deals, each checked for that count.</summary>
public sealed class GameDataset
{
    /// <summary>The <paramref name="deals"/> for <paramref name="players"/> players.</summary>
    public GameDataset(int players, List<List<int>[]> deals)
    {
        Players = players;
        Deals = deals;
    }

    /// <summary>The player count in the file's header.</summary>
    public int Players { get; }

    /// <summary>
    /// The deals in file order: each is the card ids of the three era decks, then the wonder board ids.
    /// </summary>
    public List<List<int>[]> Deals { get; }
}
