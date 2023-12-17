using _7WondersGame.src.models.Players;
using OfficeOpenXml;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        private static List<List<Player>> playerResultsBuffer = new List<List<Player>>();
        private static List<Task> runningTasks = new List<Task>();

        public static void InitFiler(string? filepath, string fileName, int maxBufferSize)
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
                using (ExcelPackage excelPackage = new ExcelPackage(file))
                {
                    excelPackage.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Error("File access denied!\n{exType}: {exMessage}", ex.GetType().Name, ex.Message);
            }
        }

        public static void WriteMatchLog(string sheetName, List<Player> players)
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
                    List<Player> sortedPlayers = players.OrderByDescending(p => p.VictoryPoints).ToList();
                    worksheet.Cells[row, MAX_PLAYERS + 2].Value = sortedPlayers[0].Board.Name;
                    worksheet.Cells[row, MAX_PLAYERS + 3].Value = sortedPlayers[0].Id;
                    worksheet.Cells[row, MAX_PLAYERS + 4].Value = sortedPlayers[0].VictoryPoints;

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

        public static void WriteMatchLogThreadSafe(string sheetName, List<Player> players)
        {
            lock (_logFileLock)
            {
                WriteMatchLog(sheetName, players);
            }
        }

        public static async Task WriteMatchLogAsync(string sheetName, List<Player> players)
        {
            // Asynchronously append the log to the file
            Task task = Task.Run(() =>
            {
                WriteMatchLogThreadSafe(sheetName, players);
            });

            // add new task to awaitable running tasks list
            runningTasks.Add(task);
        }

        public static void WriteMatchLogBuffered(string sheetName, List<Player> players)
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
                    List<List<Player>> tempBuffer = playerResultsBuffer.Select(x => x.ToList()).ToList();
                    playerResultsBuffer.Clear();
                    foreach (var results in tempBuffer)
                    {
                        _ = WriteMatchLogAsync(sheetName, results.Select(e => (Player)e.DeepCopy()).ToList());
                    }
                }
            }
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
    }
}
