using _7WondersGame.src.models.Wonders;
﻿using System.Globalization;

namespace _7WondersGame.src.models
{
    /// <summary>
    /// Reads and writes game dataset files: pre-shuffled decks and wonders, one deal per game.
    /// Results are written by <see cref="ResultsCsvWriter"/>.
    /// </summary>
    /// <remarks>
    /// The first line says the player count (X8), and every deal is checked when it is read, so a file made for
    /// another player count or edited by hand can't give a wrong game:
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
    /// A deal is <c>[deck1, deck2, deck3, wonders]</c> as card and wonder ids, as <see cref="Game.InitGameNotRandom"/> takes it.
    /// </remarks>
    public static class Filer
    {
        public const string HeaderPrefix = "Players:";

        /// <summary>Writes the deals, numbered 0, 1, ... in the order given.</summary>
        public static async Task WriteGameDatasetToFile(string path, int players, IReadOnlyList<List<int>[]> deals)
        {
            for (int i = 0; i < deals.Count; i++)
                CheckDeal(deals[i], players, $"deal {i}");

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            await using var writer = new StreamWriter(path);
            await writer.WriteLineAsync($"{HeaderPrefix} {players}");
            for (int i = 0; i < deals.Count; i++)
            {
                await writer.WriteLineAsync($"Game {i}:");
                for (int era = 0; era < 3; era++)
                {
                    await writer.WriteLineAsync($"Deck{era + 1}:");
                    await writer.WriteLineAsync(string.Join(", ", deals[i][era]));
                }
                await writer.WriteLineAsync("Wonders:");
                await writer.WriteLineAsync(string.Join(", ", deals[i][3]));
            }
        }

        /// <summary>Reads every deal, checked for the player count in the header.</summary>
        /// <exception cref="InvalidDataException">The file has no header, is malformed, or has a deal that isn't valid.</exception>
        public static async Task<(int Players, List<List<int>[]> Deals)> ReadGameDatasetFromFile(string path)
        {
            string[] lines = await File.ReadAllLinesAsync(path);
            return Parse(lines, path);
        }

        public static (int Players, List<List<int>[]> Deals) Parse(IReadOnlyList<string> lines, string name)
        {
            int i = 0;
            string? Next() => i < lines.Count ? lines[i++].Trim() : null;
            string? NonEmpty()
            {
                string? line;
                while ((line = Next()) == "") { }
                return line;
            }

            int players = ParseHeader(NonEmpty(), name);
            var deals = new List<List<int>[]>();
            string? line;
            while ((line = NonEmpty()) != null)
            {
                string expected = $"Game {deals.Count}:";
                if (line != expected)
                    throw new InvalidDataException($"{name}, line {i}: expected '{expected}', found '{line}'.");

                var deal = new List<int>[4];
                string[] sections = { "Deck1:", "Deck2:", "Deck3:", "Wonders:" };
                for (int s = 0; s < 4; s++)
                {
                    if (NonEmpty() != sections[s])
                        throw new InvalidDataException($"{name}, line {i}: expected '{sections[s]}' in game {deals.Count}.");
                    deal[s] = ParseIds(Next() ?? "", name, i);
                }
                CheckDeal(deal, players, $"{name}, game {deals.Count}");
                deals.Add(deal);
            }
            if (deals.Count == 0)
                throw new InvalidDataException($"{name} has no deals.");
            return (players, deals);
        }

        private static int ParseHeader(string? line, string name)
        {
            if (line == null || !line.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(line[HeaderPrefix.Length..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int players))
                throw new InvalidDataException(
                    $"{name} doesn't start with a '{HeaderPrefix} <n>' line. Files made before the header was added " +
                    $"(step 7) need it as their first line, e.g. '{HeaderPrefix} 3' for 3-player deals.");
            if (players is < 3 or > 7)
                throw new InvalidDataException($"{name}: {players} players; a game has 3 to 7.");
            return players;
        }

        private static List<int> ParseIds(string line, string name, int lineNumber)
        {
            var ids = new List<int>();
            foreach (string part in line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    throw new InvalidDataException($"{name}, line {lineNumber}: '{part}' is not a number.");
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
            if (deal.Length != 4)
                throw new InvalidDataException($"{where}: a deal has three decks and the wonders.");

            for (int era = 1; era <= 3; era++)
            {
                List<int> deck = deal[era - 1];
                if (deck.Count != Game.HandSize * players)
                    throw new InvalidDataException($"{where}: deck {era} has {deck.Count} cards; {players} players need {Game.HandSize * players}.");

                var counts = new Dictionary<CardId, int>();
                foreach (int id in deck)
                {
                    if (!CardsDataset.GameCards.TryGetValue((CardId)id, out Card? card))
                        throw new InvalidDataException($"{where}: deck {era} has {id}, which is not a card id.");
                    if (card.Era != era)
                        throw new InvalidDataException($"{where}: deck {era} has {card.Id}, an era {card.Era} card.");
                    counts[card.Id] = counts.GetValueOrDefault(card.Id) + 1;
                }

                foreach (Card card in CardsDataset.Cards.Values.Where(c => c.Era == era))
                {
                    int found = counts.GetValueOrDefault(card.Id), wanted = card.CopiesFor(players);
                    if (found != wanted)
                        throw new InvalidDataException(
                            $"{where}: deck {era} has {found} {card.Id}; with {players} players it has {wanted}.");
                }
                if (era == 3)
                {
                    var guilds = counts.Where(kv => CardsDataset.GuildCards.ContainsKey(kv.Key)).ToList();
                    if (guilds.Any(kv => kv.Value > 1) || guilds.Count != players + 2)
                        throw new InvalidDataException(
                            $"{where}: deck 3 has {guilds.Sum(kv => kv.Value)} guilds; {players} players need {players + 2} different ones.");
                }
            }

            List<int> wonders = deal[3];
            if (wonders.Count != players)
                throw new InvalidDataException($"{where}: {wonders.Count} wonders for {players} players.");
            if (wonders.Any(w => !Enum.IsDefined(typeof(WonderId), w)))
                throw new InvalidDataException($"{where}: wonder ids are 0 to 13.");
            if (wonders.Select(Wonder.BoardOf).Distinct().Count() != players)
                throw new InvalidDataException($"{where}: two seats have the same wonder.");
        }
    }
}
