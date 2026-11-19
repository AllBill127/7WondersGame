using _7WondersGame.Agents.RandomChoice;
using _7WondersGame.Cli.Datasets;
using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Games.Entities;

namespace _7WondersGameTests.Cli.Datasets;

/// <summary>Dataset files say their player count, and every deal is checked when read.</summary>
[TestClass]
public class DatasetFileTests
{
    /// <summary>The deal <see cref="Game.SetUpRandomGame"/> makes, as a dataset deal.</summary>
    private static List<int>[] Deal(int players, ulong seed)
    {
        Game game = new(seed);
        for (int seat = 0; seat < players; seat++)
        {
            game.Agents.Add(new RandomAgent());
        }

        game.SetUpRandomGame();
        List<int>[] deal = new List<int>[4];
        for (int era = 0; era < 3; era++)
        {
            deal[era] = game.Decks[era].Select(card => (int)card.Id).ToList();
        }

        deal[3] = game.Players.Select(player => (int)player.Wonder.Id).ToList();
        return deal;
    }

    private static string[] Lines(int players, params List<int>[][] deals)
    {
        List<string> lines = new() { $"Players: {players}" };
        for (int dealIndex = 0; dealIndex < deals.Length; dealIndex++)
        {
            lines.Add($"Game {dealIndex}:");
            for (int era = 0; era < 3; era++)
            {
                lines.Add($"Deck{era + 1}:");
                lines.Add(string.Join(", ", deals[dealIndex][era]));
            }

            lines.Add("Wonders:");
            lines.Add(string.Join(", ", deals[dealIndex][3]));
        }

        return lines.ToArray();
    }

    [TestMethod]
    public async Task WrittenDeals_ReadBack()
    {
        string path = Path.Combine(Path.GetTempPath(), $"dataset-{Guid.NewGuid():N}.txt");
        try
        {
            List<List<int>[]> deals = new() { Deal(4, 1), Deal(4, 2) };
            await Filer.WriteGameDatasetToFile(path, 4, deals);

            GameDataset dataset = await Filer.ReadGameDatasetFromFile(path);
            List<List<int>[]> read = dataset.Deals;

            Assert.AreEqual(4, dataset.Players);
            Assert.AreEqual(2, read.Count);
            for (int dealIndex = 0; dealIndex < 2; dealIndex++)
            {
                for (int part = 0; part < 4; part++)
                {
                    CollectionAssert.AreEqual(deals[dealIndex][part], read[dealIndex][part]);
                }
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void EveryDealTheGameMakes_IsValid()
    {
        for (int players = 3; players <= 7; players++)
        {
            for (ulong seed = 0; seed < 20; seed++)
            {
                Filer.CheckDeal(Deal(players, seed), players, $"{players} players, seed {seed}");
            }
        }
    }

    [TestMethod]
    public void TheCommittedDatasets_AreValid()
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory.GetFiles("7WondersGame.sln").Length == 0)
        {
            if (directory.Parent == null)
            {
                throw new AssertInconclusiveException("repository root not found");
            }

            directory = directory.Parent;
        }

        FileInfo[] files = directory.GetFiles("gameDataset_*.txt");
        Assert.IsTrue(files.Length > 0);
        foreach (FileInfo file in files)
        {
            GameDataset dataset = Filer.Parse(File.ReadAllLines(file.FullName), file.Name);
            StringAssert.StartsWith(file.Name, $"gameDataset_{dataset.Players}p_{dataset.Deals.Count}g");
        }
    }

    [TestMethod]
    public void AFileWithoutTheHeader_IsRefused()
    {
        string[] lines = Lines(3, Deal(3, 1));
        string[] linesWithoutHeader = lines.Skip(1).ToArray();
        InvalidDataException exception =
            Assert.ThrowsException<InvalidDataException>(() => Filer.Parse(linesWithoutHeader, "old.txt"));
        StringAssert.Contains(exception.Message, "Players:");
    }

    [TestMethod]
    public void DealsForAnotherPlayerCount_AreRefused()
    {
        string[] lines = Lines(3, Deal(4, 1));
        Assert.ThrowsException<InvalidDataException>(() => Filer.Parse(lines, "wrong.txt"));
    }

    [DataTestMethod]
    [DataRow("duplicate card")]
    [DataRow("wrong era")]
    [DataRow("unknown card")]
    [DataRow("same wonder twice")]
    [DataRow("one guild twice")]
    public void ABadDeal_IsRefused(string fault)
    {
        List<int>[] deal = Deal(3, 5);
        switch (fault)
        {
            case "duplicate card":
                deal[0][0] = deal[0][1];
                break;
            case "wrong era":
                SwapFirstCards(deal[0], deal[1]);
                break;
            case "unknown card":
                deal[2][0] = 999;
                break;
            case "same wonder twice":
                deal[3][1] = OtherSideOf(deal[3][0]);
                break;
            case "one guild twice":
                PutFirstGuildTwice(deal[2]);
                break;
        }

        Assert.ThrowsException<InvalidDataException>(() => Filer.CheckDeal(deal, 3, fault));
    }

    private static void SwapFirstCards(List<int> deck, List<int> otherDeck)
    {
        int firstCard = deck[0];
        deck[0] = otherDeck[0];
        otherDeck[0] = firstCard;
    }

    // the wonder ids list the seven A sides, then the seven B sides
    private static int OtherSideOf(int wonderId)
    {
        return wonderId % 7 + 7;
    }

    // the second guild of the deck replaces the first
    private static void PutFirstGuildTwice(List<int> deck)
    {
        List<int> guilds = deck.Where(cardId => CardsDataset.GuildCards.ContainsKey((CardId)cardId)).ToList();
        int firstGuildPosition = deck.IndexOf(guilds[0]);
        deck[firstGuildPosition] = guilds[1];
    }
}
