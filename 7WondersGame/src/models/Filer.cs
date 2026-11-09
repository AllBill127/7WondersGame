using Serilog;

namespace _7WondersGame.src.models
{
    /// <summary>
    /// Reads and writes game dataset files (pre-shuffled decks and wonders).
    /// Results are written by <see cref="ResultsCsvWriter"/>.
    /// </summary>
    internal static class Filer
    {
        private static string _filepath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        /// <summary>Folder that dataset files are read from and written to (the exe folder).</summary>
        public static void SetDataPath(string? filepath)
        {
            if (filepath is not null)
                _filepath = filepath;
            else
                Log.Warning("Filer data path was null; using the default: {filePath}", _filepath);
        }

        public static async Task WriteGameDatasetToFile(string fileName, Dictionary<int, List<int>[]> gameData)
        {
            String filePath = Path.Combine(_filepath, fileName);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var game in gameData)
                {
                    await writer.WriteLineAsync($"Game {game.Key}:");
                    for (int i = 0; i < 3; i++)
                    {
                        await writer.WriteLineAsync($"Deck{i+1}:");
                        // Write each deck's data
                        await writer.WriteLineAsync(string.Join(", ", game.Value[i]));
                    }
                    await writer.WriteLineAsync("Wonders:");
                    await writer.WriteLineAsync(string.Join(", ", game.Value[3]));
                }
            }
        }

        public static async Task<Dictionary<int, List<int>[]>> ReadGameDatasetFromFile(string fileName)
        {
            Dictionary<int, List<int>[]> gameData = new Dictionary<int, List<int>[]>();
            int currentGame = 0;
            List<int> wonders = null;
            List<int> deck1 = null;
            List<int> deck2 = null;
            List<int> deck3 = null;

            String filePath = Path.Combine(_filepath, fileName);
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                string[] ids;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("Game"))
                    {
                        currentGame = int.Parse(line.Split(' ')[1].TrimEnd(':'));
                    }
                    else if (line.StartsWith("Deck1"))
                    {
                        deck1 = new List<int>();
                        line = await reader.ReadLineAsync();
                        AddIdsToList(deck1, line);
                    }
                    else if (line.StartsWith("Deck2"))
                    {
                        deck2 = new List<int>();
                        line = await reader.ReadLineAsync();
                        AddIdsToList(deck2, line);
                    }
                    else if (line.StartsWith("Deck3"))
                    {
                        deck3 = new List<int>();
                        line = await reader.ReadLineAsync();
                        AddIdsToList(deck3, line);
                    }
                    else if (line.StartsWith("Wonders"))
                    {
                        wonders = new List<int>();
                        line = await reader.ReadLineAsync();
                        AddIdsToList(wonders, line);
                    }
                    //else if (!string.IsNullOrWhiteSpace(line))
                    //{
                    //    // Assume other lines contain deck or wonder data
                    //    string[] ids = line.Split(',');
                    //    foreach (var id in ids)
                    //    {
                    //        int parsedId;
                    //        if (int.TryParse(id.Trim(), out parsedId))
                    //        {
                    //            if (deck1 != null)
                    //            {
                    //                deck1.Add(parsedId);
                    //            }
                    //            else if (deck2 != null)
                    //            {
                    //                deck2.Add(parsedId);
                    //            }
                    //            else if (deck3 != null)
                    //            {
                    //                deck3.Add(parsedId);
                    //            }
                    //            else if (wonders != null)
                    //            {
                    //                wonders.Add(parsedId);
                    //            }
                    //        }
                    //    }
                    //}

                    if ((deck1 != null && deck1.Count > 0) &&
                        (deck2 != null && deck2.Count > 0) &&
                        (deck3 != null && deck3.Count > 0) &&
                        (wonders != null && wonders.Count > 0))
                    {
                        gameData.Add(currentGame, new List<int>[] { deck1, deck2, deck3, wonders });
                        deck1 = null;
                        deck2 = null;
                        deck3 = null;
                        wonders = null;
                    }
                }
            }

            return gameData;
        }

        private static void AddIdsToList (List<int> idList, string line)
        {
            string[] ids = line.Split(',');
            foreach (var id in ids)
            {
                int parsedId;
                if (int.TryParse(id.Trim(), out parsedId))
                    idList.Add(parsedId);
            }
        }
    }
}
