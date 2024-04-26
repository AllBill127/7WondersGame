using _7WondersGame.src.models.Players;
using _7WondersGame.src.models.Wonders;
using OfficeOpenXml;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace _7WondersGame.src.models
{
    internal static class Filer
    {
        private static readonly int MAX_PLAYERS = 7;
        private static int maxLogBuffer = 2;
        private static string _filepath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static string _fileName = "Game_results.xlsx";

        private static readonly object _logFileLock = new object();
        private static readonly object _logBufferLock = new object();
        private static List<List<PlayerResults>> playerResultsBuffer = new List<List<PlayerResults>>();
        private static List<Task> runningTasks = new List<Task>();

        public static void InitFiler(string? filepath, string fileName, int maxBufferSize = 2)
        {
            maxLogBuffer = maxBufferSize;

            if (filepath is not null)
            {
                _filepath = filepath;
                _fileName = fileName;
            }
            else
            {
                Log.Warning("Filer initialized with default path because given path was null.\n Default file path: {filePath}", _filepath);
                _fileName = fileName;
            }

            // check for file access errors
            try
            {
                FileInfo file = new FileInfo(Path.Combine(_filepath, _fileName));

                // Check if the file exists
                if (!file.Exists)
                {
                    // File doesn't exist, create it
                    using (ExcelPackage excelPackage = new ExcelPackage())
                    {
                        excelPackage.Workbook.Worksheets.Add("Sheet1");
                        excelPackage.SaveAs(file);
                    }
                }
                else
                {
                    // File exists, open it
                    using (ExcelPackage excelPackage = new ExcelPackage(file))
                    {
                        excelPackage.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("File access denied!\n{exType}: {exMessage}", ex.GetType().Name, ex.Message);
            }
        }

        public static void WriteMatchLog(string sheetName, List<PlayerResults> players)
        {
            String fileName = Path.Combine(_filepath, _fileName);
            FileInfo file = new FileInfo(fileName);

            try
            {
                using (ExcelPackage excelPackage = new ExcelPackage(file))
                {
                    ExcelWorksheet? worksheet = excelPackage.Workbook.Worksheets.SingleOrDefault(sheet => sheet.Name == sheetName);

                    // If the worksheet already exists, use it; otherwise, create a new one
                    if (worksheet == null)
                    {
                        excelPackage.Workbook.Worksheets.Add(sheetName);
                        worksheet = excelPackage.Workbook.Worksheets.SingleOrDefault(sheet => sheet.Name == sheetName);
                        for (int i = 0; i < players.Count; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = $"Player_{i}";
                        }
                        //worksheet.Cells[1, players.Count + 1].Value = "LogedAt";
                        worksheet.Cells[1, MAX_PLAYERS + 1].Value = "p2 Wonder";
                        worksheet.Cells[1, MAX_PLAYERS + 2].Value = "Winner Wonder";
                        worksheet.Cells[1, MAX_PLAYERS + 3].Value = "WinnerId";
                        worksheet.Cells[1, MAX_PLAYERS + 4].Value = "Score";
                        worksheet.Cells[1, MAX_PLAYERS + 5].Value = "p2 Rank";
                    }

                    // Find the next available row in the worksheet
                    int row = worksheet.Dimension?.End.Row + 1 ?? 1;

                    if (row % 1 == 0)
                        Log.Information("Logging row: {rowNr}", row);

                    // Write player scores to the worksheet
                    for (int i = 0; i < players.Count; i++)
                    {
                        worksheet.Cells[row, i + 1].Value = players[i].VictoryPoints;
                    }
                    // save time of writing this log
                    //worksheet.Cells[row, players.Count + 1].Value = logTimestamp.ToOADate();

                    // log player 2 wonder names
                    worksheet.Cells[row, MAX_PLAYERS + 1].Value = players[2].Board.Name;

                    // log winning player wonder name, id and score
                    List<PlayerResults> sortedPlayers = players.OrderByDescending(p => p.VictoryPoints).ToList();
                    worksheet.Cells[row, MAX_PLAYERS + 2].Value = sortedPlayers[0].Board.Name;
                    worksheet.Cells[row, MAX_PLAYERS + 3].Value = sortedPlayers[0].Id;
                    worksheet.Cells[row, MAX_PLAYERS + 4].Value = sortedPlayers[0].VictoryPoints;

                    // log player 2 rank (scoring place)
                    worksheet.Cells[row, MAX_PLAYERS + 5].Value = sortedPlayers
                        .OrderByDescending(x => x.VictoryPoints)
                        .Select((entry, i) => new { entry.Id, Index = i })
                        .FirstOrDefault(x => x.Id == 2)?.Index + 1 ?? -1;

                    // Save changes to the Excel file
                    excelPackage.Save();
                }

                Log.Debug("Results written to {fileName}.", fileName);
            }
            catch (Exception ex)
            {
                Log.Error("{exType}: {exMessage}", ex.GetType(), ex.Message);
            }
        }

        public static void WriteMatchLogBuffer(string sheetName, List<List<PlayerResults>> playersBuffer)
        {
            String fileName = Path.Combine(_filepath, _fileName);
            FileInfo file = new FileInfo(fileName);

            try
            {
                using (ExcelPackage excelPackage = new ExcelPackage(file))
                {
                    ExcelWorksheet? worksheet = excelPackage.Workbook.Worksheets.SingleOrDefault(sheet => sheet.Name == sheetName);

                    // If the worksheet already exists, use it; otherwise, create a new one
                    if (worksheet == null)
                    {
                        excelPackage.Workbook.Worksheets.Add(sheetName);
                        worksheet = excelPackage.Workbook.Worksheets.SingleOrDefault(sheet => sheet.Name == sheetName);
                        for (int i = 0; i < playersBuffer[0].Count; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = $"Player_{i}";
                        }
                        //worksheet.Cells[1, players.Count + 1].Value = "LogedAt";
                        worksheet.Cells[1, MAX_PLAYERS + 1].Value = "p2 Wonder";
                        worksheet.Cells[1, MAX_PLAYERS + 2].Value = "Winner Wonder";
                        worksheet.Cells[1, MAX_PLAYERS + 3].Value = "WinnerId";
                        worksheet.Cells[1, MAX_PLAYERS + 4].Value = "Score";
                        worksheet.Cells[1, MAX_PLAYERS + 5].Value = "p2 Rank";
                    }

                    foreach (var players in playersBuffer)
                    {
                        // Find the next available row in the worksheet
                        int row = worksheet.Dimension?.End.Row + 1 ?? 1;

                        if (row % 1 == 0)
                            Log.Information("Logging row: {rowNr}", row);

                        // Write player scores to the worksheet
                        for (int i = 0; i < players.Count; i++)
                        {
                            worksheet.Cells[row, i + 1].Value = players[i].VictoryPoints;
                        }
                        // save time of writing this log
                        //worksheet.Cells[row, players.Count + 1].Value = logTimestamp.ToOADate();

                        // log player 2 wonder names
                        worksheet.Cells[row, MAX_PLAYERS + 1].Value = players[2].Board.Name;

                        // log winning player wonder name, id and score
                        List<PlayerResults> sortedPlayers = players.OrderByDescending(p => p.VictoryPoints).ToList();
                        worksheet.Cells[row, MAX_PLAYERS + 2].Value = sortedPlayers[0].Board.Name;
                        worksheet.Cells[row, MAX_PLAYERS + 3].Value = sortedPlayers[0].Id;
                        worksheet.Cells[row, MAX_PLAYERS + 4].Value = sortedPlayers[0].VictoryPoints;

                        // log player 2 rank (scoring place)
                        worksheet.Cells[row, MAX_PLAYERS + 5].Value = sortedPlayers
                            .OrderByDescending(x => x.VictoryPoints)
                            .Select((entry, i) => new { entry.Id, Index = i })
                            .FirstOrDefault(x => x.Id == 2)?.Index + 1 ?? -1;
                    }

                    // Save changes to the Excel file
                    excelPackage.Save();
                }

                Log.Debug("Results written to {fileName}.", fileName);
            }
            catch (Exception ex)
            {
                Log.Error("{exType}: {exMessage}", ex.GetType(), ex.Message);
            }
        }

        public static void WriteMatchLogThreadSafe(string sheetName, List<PlayerResults> players)
        {
            lock (_logFileLock)
            {
                WriteMatchLog(sheetName, players);
            }
        }

        public static async Task WriteMatchLogAsync(string sheetName, List<PlayerResults> players)
        {
            // Asynchronously append the log to the file
            Task task = Task.Run(() =>
            {
                WriteMatchLogThreadSafe(sheetName, players);
            });

            // add new task to awaitable running tasks list
            runningTasks.Add(task);
        }

        public static void WriteMatchLogBuffered(string sheetName, List<PlayerResults> players)
        {
            lock(_logBufferLock)
            {
                if (playerResultsBuffer.Count < maxLogBuffer - 1)
                {
                    playerResultsBuffer.Add(players);
                }
                else
                {
                    playerResultsBuffer.Add(players);
                    //List<List<Player>> tempBuffer = playerResultsBuffer.Select(x => x.Select(e => (Player)e.DeepCopy()).ToList()).ToList();
                    List<List<PlayerResults>> tempBuffer = playerResultsBuffer.Select(x => x.Select(e => e).ToList()).ToList();
                    playerResultsBuffer.Clear();
                    _ = WriteMatchLogBufferAsync(sheetName, tempBuffer);
                }
            }
        }

        public static void WriteMatchLogBufferThreadSafe(string sheetName, List<List<PlayerResults>> playersBuffer)
        {
            lock (_logFileLock)
            {
                WriteMatchLogBuffer(sheetName, playersBuffer);
            }
        }

        public static async Task WriteMatchLogBufferAsync(string sheetName, List<List<PlayerResults>> playersBuffer)
        {
            // Asynchronously append the log to the file
            Task task = Task.Run(() =>
            {
                WriteMatchLogBufferThreadSafe(sheetName, playersBuffer);
            });

            // add new task to awaitable running tasks list
            runningTasks.Add(task);
        }

        public static async Task<bool> FlushMatchLogBuffer(string sheetName)
        {
            foreach (var results in playerResultsBuffer)
            {
                await WriteMatchLogAsync(sheetName, results.Select(e => e).ToList());
            }
            playerResultsBuffer.Clear();

            return playerResultsBuffer.Count > 0 ? false : true;
        }

        public static async Task<bool> AwaitAllLoggingTasksAsync()
        {
            await Task.WhenAll(runningTasks);
            return true;
        }

        public static void OLDWriteMatchLog(List<Player> players, DateTime logTimestamp)
        {
            Log.Debug("NEED IMPLEMENT MATCH LOG");
            Log.Debug("Log time:{logTimestamp}", logTimestamp);
            //  char file[64];
            //  sprintf(file, "../logs/7w_match_%d.csv", log_id);
            //  log_file_path = file;

            //  log_file.open(log_file_path, std::ofstream::out);

            //  if (player_list.empty())
            //  {
            //      log_file.close();
            //      return;  // No players to log, so exit early
            //  }

            //  log_file << " ";
            //  for (const auto&player : player_list) {
            //      log_file << "," << "player_" << player->GetId();
            //  }
            //  log_file << "\n";

            //  log_file << "Victory Points";
            //  for (const auto&player : player_list) {
            //      log_file << "," << player->CalculateScore();
            //  }
            //  log_file << "\n";

            //  log_file << "Victory Points";
            //  for (const auto&player : player_list) {
            //      log_file << "," << player->CalculateCivilianScore();
            //  }
            //  log_file << "\n";

            //  log_file << "Victory Points";
            //  for (const auto&player : player_list) {
            //      log_file << "," << player->CalculateCommercialScore();
            //  }
            //  log_file << "\n";

            //  log_file << "Victory Points";
            //  for (const auto&player : player_list) {
            //      log_file << "," << player->CalculateGuildScore();
            //  }
            //  log_file << "\n";

            //  log_file << "Victory Points";
            //  for (const auto&player : player_list) {
            //      log_file << "," << player->CalculateMilitaryScore();
            //  }
            //  log_file << "\n";

            //  log_file << "Victory Points";
            //  for (const auto&player : player_list) {
            //      log_file << "," << player->CalculateScientificScore();
            //  }
            //  log_file << "\n";

            //  log_file << "Victory Points";
            //  for (const auto&player : player_list) {
            //      log_file << "," << player->CalculateWonderScore();
            //  }
            //  log_file << "\n";


            //  // print resources
            //  std::unordered_map < int, std::string> resourceNames = {
            //      { RESOURCE::wood, "Wood"},
            //{ RESOURCE::ore, "Ore"},
            //{ RESOURCE::clay, "Clay"},
            //{ RESOURCE::stone, "Stone"},
            //{ RESOURCE::loom, "Loom"},
            //{ RESOURCE::glass, "Glass"},
            //{ RESOURCE::papyrus, "Papyrus"},
            //{ RESOURCE::gear, "Gear"},
            //{ RESOURCE::compass, "Compass"},
            //{ RESOURCE::tablet, "Tablet"},
            //{ RESOURCE::coins, "Clay"},
            //{ RESOURCE::shields, "Shields"}
            //  };

            //  for (const auto&resourceEntry : resourceNames) {
            //      log_file << resourceEntry.second;  // Use the string representation
            //      for (const auto&player : player_list) {
            //          log_file << "," << player->GetResources()[resourceEntry.first];
            //      }
            //      log_file << "\n";
            //  }

            //  log_file.close();
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
