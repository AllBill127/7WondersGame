using System.Globalization;
using _7WondersGame.Cli.Results;
using _7WondersGame.Engine.Cards.Data;
using _7WondersGame.Engine.Cards.Entities;
using _7WondersGame.Engine.Cards.Enums;
using _7WondersGame.Engine.Games.Entities;
using _7WondersGame.Engine.Wonders.Entities;
using _7WondersGame.Engine.Wonders.Enums;

namespace _7WondersGame.Cli.Datasets;

/// <summary>
/// Reads and writes game dataset files: pre-shuffled decks and wonders, one deal per game.
/// Results are written by <see cref="ResultsCsvWriter"/>.
/// </summary>
/// <remarks>
/// The first line says the player count, and every deal is checked when it is read, so a file made for another
/// player count or edited by hand can't give a wrong game:
/// <code>
/// Players: 3
/// Game 0:
/// Deck1:
/// 4, 58, 36, ...
/// Deck2:
/// ...
/// Deck3:
/// ...
/// Wonders:
/// 13, 7, 8
/// </code>
/// A deal is <c>[deck1, deck2, deck3, wonders]</c> as card and wonder ids, as <see cref="Game.SetUpGameFromDataset"/>
/// takes it.
/// </remarks>
public static class Filer
{
    /// <summary>The start of a dataset file's first line, before the player count.</summary>
    public const string HeaderPrefix = "Players:";

    /// <summary>Where a deal keeps the wonders: after the three decks.</summary>
    private const int WondersIndex = Game.LastEra;

    /// <summary>The heading of each part of a deal, in the order the file has them.</summary>
    private static readonly string[] DealSections = { "Deck1:", "Deck2:", "Deck3:", "Wonders:" };

    /// <summary>Writes the deals, numbered 0, 1, ... in the order given.</summary>
    public static async Task WriteGameDatasetToFile(string path, int players, IReadOnlyList<List<int>[]> deals)
    {
        for (int dealIndex = 0; dealIndex < deals.Count; dealIndex++)
        {
            CheckDeal(deals[dealIndex], players, $"deal {dealIndex}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await using (StreamWriter writer = new(path))
        {
            await writer.WriteLineAsync($"{HeaderPrefix} {players}");
            for (int dealIndex = 0; dealIndex < deals.Count; dealIndex++)
            {
                await writer.WriteLineAsync($"Game {dealIndex}:");
                for (int era = 0; era < Game.LastEra; era++)
                {
                    await writer.WriteLineAsync($"Deck{era + 1}:");
                    await writer.WriteLineAsync(string.Join(", ", deals[dealIndex][era]));
                }
                await writer.WriteLineAsync("Wonders:");
                await writer.WriteLineAsync(string.Join(", ", deals[dealIndex][WondersIndex]));
            }
        }
    }

    /// <summary>Reads every deal, checked for the player count in the header.</summary>
    /// <exception cref="InvalidDataException">
    /// The file has no header, is malformed, or has a deal that isn't valid.
    /// </exception>
    public static async Task<(int Players, List<List<int>[]> Deals)> ReadGameDatasetFromFile(string path)
    {
        string[] lines = await File.ReadAllLinesAsync(path);
        return Parse(lines, path);
    }

    /// <summary>
    /// The player count and the deals of a dataset file's lines (<paramref name="name"/> is for messages).
    /// </summary>
    /// <exception cref="InvalidDataException">The lines are not a valid dataset.</exception>
    public static (int Players, List<List<int>[]> Deals) Parse(IReadOnlyList<string> lines, string name)
    {
        int lineIndex = 0;
        int players = ParseHeader(NextNonEmptyLine(lines, ref lineIndex), name);
        List<List<int>[]> deals = new();
        string? line = NextNonEmptyLine(lines, ref lineIndex);
        while (line != null)
        {
            string expected = $"Game {deals.Count}:";
            if (line != expected)
            {
                throw new InvalidDataException($"{name}, line {lineIndex}: expected '{expected}', found '{line}'.");
            }

            List<int>[] deal = ParseDeal(lines, ref lineIndex, name, deals.Count);
            CheckDeal(deal, players, $"{name}, game {deals.Count}");
            deals.Add(deal);
            line = NextNonEmptyLine(lines, ref lineIndex);
        }
        if (deals.Count == 0)
        {
            throw new InvalidDataException($"{name} has no deals.");
        }
        return (players, deals);
    }

    /// <summary>The deal after a game's heading: each section's heading and its line of ids.</summary>
    private static List<int>[] ParseDeal(IReadOnlyList<string> lines, ref int nextLineIndex, string name,
                                         int gameNumber)
    {
        List<int>[] deal = new List<int>[DealSections.Length];
        for (int section = 0; section < DealSections.Length; section++)
        {
            if (NextNonEmptyLine(lines, ref nextLineIndex) != DealSections[section])
            {
                throw new InvalidDataException(
                    $"{name}, line {nextLineIndex}: expected '{DealSections[section]}' in game {gameNumber}.");
            }

            string? idsLine = NextLine(lines, ref nextLineIndex);
            if (idsLine == null)
            {
                idsLine = "";
            }
            deal[section] = ParseIds(idsLine, name, nextLineIndex);
        }
        return deal;
    }

    /// <summary>The line at <paramref name="nextLineIndex"/>, trimmed, or null at the end; moves on one line.</summary>
    private static string? NextLine(IReadOnlyList<string> lines, ref int nextLineIndex)
    {
        if (nextLineIndex >= lines.Count)
        {
            return null;
        }
        string line = lines[nextLineIndex].Trim();
        nextLineIndex++;
        return line;
    }

    /// <summary>The next line that isn't empty, trimmed, or null at the end.</summary>
    private static string? NextNonEmptyLine(IReadOnlyList<string> lines, ref int nextLineIndex)
    {
        string? line = NextLine(lines, ref nextLineIndex);
        while (line == "")
        {
            line = NextLine(lines, ref nextLineIndex);
        }
        return line;
    }

    private static int ParseHeader(string? line, string name)
    {
        if (line == null || !line.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw MissingHeader(name);
        }
        string playersText = line.Substring(HeaderPrefix.Length).Trim();
        if (!int.TryParse(playersText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int players))
        {
            throw MissingHeader(name);
        }
        if (players < 3 || players > 7)
        {
            throw new InvalidDataException($"{name}: {players} players; a game has 3 to 7.");
        }
        return players;
    }

    private static InvalidDataException MissingHeader(string name)
    {
        return new InvalidDataException(
            $"{name} doesn't start with a '{HeaderPrefix} <n>' line. Files made before the header was added " +
            $"need it as their first line, e.g. '{HeaderPrefix} 3' for 3-player deals.");
    }

    private static List<int> ParseIds(string line, string name, int lineNumber)
    {
        List<int> ids = new();
        foreach (string part in line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
            {
                throw new InvalidDataException($"{name}, line {lineNumber}: '{part}' is not a number.");
            }
            ids.Add(id);
        }
        return ids;
    }

    /// <summary>
    /// Checks that a deal is one <see cref="Game.CreateDecks"/> could have made for this player count: each era's
    /// deck holds exactly that era's cards with the right number of copies, era III has players + 2 different
    /// guilds, and every seat has a wonder, each a different one.
    /// </summary>
    /// <exception cref="InvalidDataException">It isn't.</exception>
    public static void CheckDeal(List<int>[] deal, int players, string where)
    {
        if (deal.Length != DealSections.Length)
        {
            throw new InvalidDataException($"{where}: a deal has three decks and the wonders.");
        }

        for (int deckIndex = 0; deckIndex < Game.LastEra; deckIndex++)
        {
            int era = deckIndex + 1;
            CheckDeck(deal[deckIndex], era, players, where);
        }
        CheckWonders(deal[WondersIndex], players, where);
    }

    /// <summary>
    /// Checks that <paramref name="deck"/> holds exactly <paramref name="era"/>'s cards for the players.
    /// </summary>
    private static void CheckDeck(List<int> deck, int era, int players, string where)
    {
        int deckSize = Game.HandSize * players;
        if (deck.Count != deckSize)
        {
            throw new InvalidDataException(
                $"{where}: deck {era} has {deck.Count} cards; {players} players need {deckSize}.");
        }

        Dictionary<CardId, int> counts = CountCardsOfEra(deck, era, where);
        foreach (Card card in CardsDataset.Cards.Values)
        {
            if (card.Era != era)
            {
                continue;
            }
            int found = counts.GetValueOrDefault(card.Id);
            int wanted = card.CopiesFor(players);
            if (found != wanted)
            {
                throw new InvalidDataException(
                    $"{where}: deck {era} has {found} {card.Id}; with {players} players it has {wanted}.");
            }
        }
        if (era == Game.LastEra)
        {
            CheckGuilds(counts, players, where);
        }
    }

    /// <summary>
    /// How many copies of each card the deck holds; every id must be a card of <paramref name="era"/>.
    /// </summary>
    private static Dictionary<CardId, int> CountCardsOfEra(List<int> deck, int era, string where)
    {
        Dictionary<CardId, int> counts = new();
        foreach (int id in deck)
        {
            if (!CardsDataset.GameCards.TryGetValue((CardId)id, out Card? card))
            {
                throw new InvalidDataException($"{where}: deck {era} has {id}, which is not a card id.");
            }
            if (card.Era != era)
            {
                throw new InvalidDataException($"{where}: deck {era} has {card.Id}, an era {card.Era} card.");
            }
            counts[card.Id] = counts.GetValueOrDefault(card.Id) + 1;
        }
        return counts;
    }

    /// <summary>Checks that the last era's deck has players + 2 guilds, all different.</summary>
    private static void CheckGuilds(Dictionary<CardId, int> counts, int players, string where)
    {
        int guildCards = 0;
        int differentGuilds = 0;
        bool isAnyGuildRepeated = false;
        foreach (KeyValuePair<CardId, int> count in counts)
        {
            if (!CardsDataset.GuildCards.ContainsKey(count.Key))
            {
                continue;
            }
            guildCards += count.Value;
            differentGuilds++;
            if (count.Value > 1)
            {
                isAnyGuildRepeated = true;
            }
        }

        int guildsNeeded = players + 2;
        if (isAnyGuildRepeated || differentGuilds != guildsNeeded)
        {
            throw new InvalidDataException(
                $"{where}: deck 3 has {guildCards} guilds; {players} players need {guildsNeeded} different ones.");
        }
    }

    /// <summary>Checks that every seat has a wonder, each a different board.</summary>
    private static void CheckWonders(List<int> wonders, int players, string where)
    {
        if (wonders.Count != players)
        {
            throw new InvalidDataException($"{where}: {wonders.Count} wonders for {players} players.");
        }
        if (wonders.Any(IsNotWonderId))
        {
            throw new InvalidDataException($"{where}: wonder ids are 0 to 13.");
        }
        if (wonders.Select(WonderBoardProgress.BoardOf).Distinct().Count() != players)
        {
            throw new InvalidDataException($"{where}: two seats have the same wonder.");
        }
    }

    private static bool IsNotWonderId(int id)
    {
        return !Enum.IsDefined(typeof(WonderId), id);
    }
}
