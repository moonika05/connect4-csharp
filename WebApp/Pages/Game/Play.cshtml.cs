using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ConsoleApp.GameEngine;
using WebApp.Helpers;
using System.Text.Json;
using ConsoleApp.GameEngine.Models;
using ConsoleApp.GameEngine.Storage.Database;

namespace WebApp.Pages.Game
{
    public class PlayModel : PageModel
    {
        private readonly IGameRepository _repository;

        public PlayModel(IGameRepository repository)
        {
            _repository = repository;
        }

        public GameConfiguration Config { get; set; } = GameConfiguration.Classic();
        public int[,] Board { get; set; } = new int[6, 7];
        public int CurrentPlayer { get; set; } = 1;
        public bool IsGameOver { get; set; } = false;
        public string? Winner { get; set; }
        public string GameId { get; set; } = Guid.NewGuid().ToString();
        public PlayerType Player1Type { get; set; } = PlayerType.Human;
        public PlayerType Player2Type { get; set; } = PlayerType.Human;
        public string? ErrorMessage { get; set; }
        public string? AiThinking { get; set; }

        public void OnGet(string? gameId = null, long? nocache = null)
        {
            Console.WriteLine($"=== OnGet CALLED === gameId: {gameId}, nocache: {nocache}");
            
            // Check if we have game in session
            var sessionData = HttpContext.Session.GetString("CurrentGame");
            Console.WriteLine($"Session data exists: {!string.IsNullOrEmpty(sessionData)}");
            
            if (!string.IsNullOrEmpty(gameId))
            {
                LoadGameFromSession();
            }
            else
            {
                LoadGameFromSession();
            }
            
            // Load error message from TempData if exists
            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"]?.ToString();
            }
            
            // AI vs AI - make ONE move only (then JavaScript will refresh)
            if (Player1Type != PlayerType.Human && Player2Type != PlayerType.Human && !IsGameOver)
            {
                Console.WriteLine("=== AI vs AI mode - making one move ===");
                
                var board = new GameBoard(Config);
                board.LoadBoardState(Board);
                
                MakeAIMove(board);
                SaveGameToSession();
                
                Console.WriteLine($"AI move completed. Game over: {IsGameOver}");
            }
            
            // DEBUG
            Console.WriteLine($"OnGet - Board state:");
            for (int row = 0; row < Config.Rows; row++)
            {
                for (int col = 0; col < Config.Columns; col++)
                {
                    Console.Write(Board[row, col] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine($"Current Player: {CurrentPlayer}");
            Console.WriteLine($"Player1Type: {Player1Type}, Player2Type: {Player2Type}");
        }

        public IActionResult OnPostMove(int column)
        {
            Console.WriteLine($"\n=== OnPostMove CALLED === column: {column}");
            LoadGameFromSession();
            
            Console.WriteLine($"BEFORE move - CurrentPlayer: {CurrentPlayer}");

            if (IsGameOver)
            {
                ErrorMessage = "Game is already over!";
                TempData["ErrorMessage"] = ErrorMessage;
                return RedirectToPage(new { nocache = DateTime.Now.Ticks });
            }

            var board = new GameBoard(Config);
            board.LoadBoardState(Board);

            // Make human move
            if (board.DropPiece(column, CurrentPlayer))
            {
                Board = board.GetBoardState();
                Console.WriteLine($"After human drop - piece placed in column {column + 1}");

                // Check win
                if (board.CheckWin(CurrentPlayer))
                {
                    IsGameOver = true;
                    PlayerType winnerType = CurrentPlayer == 1 ? Player1Type : Player2Type;
                    Winner = $"Player {CurrentPlayer} ({winnerType})";
                    SaveGameToSession();
                    Console.WriteLine($"HUMAN WON! Redirecting...");
                    return RedirectToPage(new { nocache = DateTime.Now.Ticks });
                }
                
                if (board.IsFull())
                {
                    IsGameOver = true;
                    Winner = "Draw";
                    SaveGameToSession();
                    Console.WriteLine($"DRAW! Redirecting...");
                    return RedirectToPage(new { nocache = DateTime.Now.Ticks });
                }

                // Switch player
                CurrentPlayer = CurrentPlayer == 1 ? 2 : 1;
                Console.WriteLine($"AFTER switch - CurrentPlayer: {CurrentPlayer}");

                // Check if next player is AI
                PlayerType nextPlayerType = CurrentPlayer == 1 ? Player1Type : Player2Type;
                Console.WriteLine($"Next player type: {nextPlayerType}");
                
                if (nextPlayerType != PlayerType.Human)
                {
                    // AI turn
                    Console.WriteLine($"AI turn starting...");
                    var aiBoard = new GameBoard(Config);
                    aiBoard.LoadBoardState(Board);
                    MakeAIMove(aiBoard);
                    Console.WriteLine($"AI turn completed");
                }

                SaveGameToSession();
                Console.WriteLine($"Game saved to session. Redirecting...");
                return RedirectToPage(new { nocache = DateTime.Now.Ticks });
            }
            else
            {
                ErrorMessage = "Invalid move! Column is full.";
                TempData["ErrorMessage"] = ErrorMessage;
                Console.WriteLine($"Invalid move! Redirecting...");
                return RedirectToPage(new { nocache = DateTime.Now.Ticks });
            }
        }

        public IActionResult OnPostSaveGame(string saveName)
        {
            LoadGameFromSession();

            if (string.IsNullOrWhiteSpace(saveName))
            {
                ErrorMessage = "Please enter a save name!";
                TempData["ErrorMessage"] = ErrorMessage;
                return RedirectToPage(new { nocache = DateTime.Now.Ticks });
            }

            var state = new GameState(
                GameId,
                DateTime.Now,
                Config,
                Board,
                CurrentPlayer,
                IsGameOver,
                Winner
            );

            _repository.SaveGame(state, $"{saveName}.json");
            
            TempData["SuccessMessage"] = $"Game saved as '{saveName}'!";
            return RedirectToPage(new { nocache = DateTime.Now.Ticks });
        }

        public IActionResult OnPostSaveConfig(string configName)
        {
            LoadGameFromSession();

            if (string.IsNullOrWhiteSpace(configName))
            {
                ErrorMessage = "Please enter a config name!";
                TempData["ErrorMessage"] = ErrorMessage;
                return RedirectToPage(new { nocache = DateTime.Now.Ticks });
            }

            try
            {
                // Save current configuration with custom name
                var namedConfig = new GameConfiguration(
                    configName,
                    Config.Rows,
                    Config.Columns,
                    Config.WinCondition,
                    Config.IsCylinder
                );
                
                _repository.SaveConfiguration(namedConfig, $"{configName}.json");
                
                TempData["SuccessMessage"] = $"Configuration '{configName}' saved!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to save configuration: {ex.Message}";
            }
            
            return RedirectToPage(new { nocache = DateTime.Now.Ticks });
        }

        public IActionResult OnPostNewGame()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Index");
        }

        private void InitializeNewGame()
        {
            Console.WriteLine("=== InitializeNewGame called ===");
            
            // Get player types from session
            var p1Type = HttpContext.Session.GetString("Player1Type");
            var p2Type = HttpContext.Session.GetString("Player2Type");
            
            Console.WriteLine($"Player types from session: P1={p1Type}, P2={p2Type}");

            Player1Type = Enum.TryParse<PlayerType>(p1Type, out var pt1) ? pt1 : PlayerType.Human;
            Player2Type = Enum.TryParse<PlayerType>(p2Type, out var pt2) ? pt2 : PlayerType.Human;

            // Get configuration from session (or use default)
            try
            {
                var configData = HttpContext.Session.GetObject<JsonElement>("GameConfiguration");
                if (configData.ValueKind != JsonValueKind.Undefined)
                {
                    Config = new GameConfiguration(
                        configData.GetProperty("Name").GetString() ?? "Custom",
                        configData.GetProperty("Rows").GetInt32(),
                        configData.GetProperty("Columns").GetInt32(),
                        configData.GetProperty("WinCondition").GetInt32(),
                        configData.GetProperty("IsCylinder").GetBoolean()
                    );
                    Console.WriteLine($"Loaded custom config: {Config.Rows}x{Config.Columns}, Win:{Config.WinCondition}, Cylinder:{Config.IsCylinder}");
                }
                else
                {
                    Config = GameConfiguration.Classic();
                    Console.WriteLine("Using default Classic configuration");
                }
            }
            catch
            {
                Config = GameConfiguration.Classic();
                Console.WriteLine("Failed to load config, using Classic");
            }

            Board = new int[Config.Rows, Config.Columns];
            CurrentPlayer = 1;
            IsGameOver = false;
            Winner = null;
            GameId = Guid.NewGuid().ToString();

            SaveGameToSession();
            Console.WriteLine($"New game initialized: P1={Player1Type}, P2={Player2Type}");

            // If Player 1 is AI, make first move
            if (Player1Type != PlayerType.Human)
            {
                Console.WriteLine("Player 1 is AI, making first move...");
                var board = new GameBoard(Config);
                MakeAIMove(board);
                SaveGameToSession();
            }
        }

        private void MakeAIMove(GameBoard board)
        {
            PlayerType aiType = CurrentPlayer == 1 ? Player1Type : Player2Type;
            var ai = new GameAI(Config, GetAIDifficulty(aiType));

            // Get current board state for AI
            int[,] currentBoard = board.GetBoardState();
            int column = ai.GetBestMove(currentBoard, CurrentPlayer);
            
            AiThinking = $"AI ({aiType}) chose column {column + 1}";
            Console.WriteLine($"=== AI making move === Player: {CurrentPlayer}, AI Type: {aiType}, Column: {column + 1}");

            if (board.DropPiece(column, CurrentPlayer))
            {
                Board = board.GetBoardState();
                Console.WriteLine($"After AI drop - piece placed in column {column + 1}");

                if (board.CheckWin(CurrentPlayer))
                {
                    IsGameOver = true;
                    Winner = $"Player {CurrentPlayer} ({aiType})";
                    Console.WriteLine($"AI WON!");
                }
                else if (board.IsFull())
                {
                    IsGameOver = true;
                    Winner = "Draw";
                    Console.WriteLine($"DRAW after AI move!");
                }
                else
                {
                    CurrentPlayer = CurrentPlayer == 1 ? 2 : 1;
                    Console.WriteLine($"After AI move - switched to Player {CurrentPlayer}");
                }
            }
        }

        private int GetAIDifficulty(PlayerType type)
        {
            return type switch
            {
                PlayerType.AIEasy => 1,
                PlayerType.AIMedium => 2,
                PlayerType.AIHard => 3,
                _ => 2
            };
        }

        private void SaveGameToSession()
        {
            var gameData = new
            {
                Config,
                Board,
                CurrentPlayer,
                IsGameOver,
                Winner,
                GameId,
                Player1Type,
                Player2Type
            };

            HttpContext.Session.SetObject("CurrentGame", gameData);
            Console.WriteLine($"=== SAVED to session === CurrentPlayer: {CurrentPlayer}, P1Type: {Player1Type}, P2Type: {Player2Type}");
        }

        private void LoadGameFromSession()
        {
            var gameData = HttpContext.Session.GetObject<JsonElement>("CurrentGame");
            
            if (gameData.ValueKind == JsonValueKind.Undefined)
            {
                Console.WriteLine("=== No game in session, initializing new ===");
                InitializeNewGame();
                return;
            }

            try
            {
                Console.WriteLine("=== Loading game from session ===");
                
                // Parse configuration
                var configJson = gameData.GetProperty("Config");
                Config = new GameConfiguration(
                    configJson.GetProperty("Name").GetString() ?? "Classic",
                    configJson.GetProperty("Rows").GetInt32(),
                    configJson.GetProperty("Columns").GetInt32(),
                    configJson.GetProperty("WinCondition").GetInt32(),
                    configJson.GetProperty("IsCylinder").GetBoolean()
                );

                // Parse board
                var boardJson = gameData.GetProperty("Board");
                var options = new JsonSerializerOptions
                {
                    Converters = { new WebApp.Helpers.MultiDimensionalArrayConverter() }
                };
                Board = JsonSerializer.Deserialize<int[,]>(boardJson.GetRawText(), options) ?? new int[6, 7];
                
                Console.WriteLine($"Loaded board - checking cells:");
                int pieceCount = 0;
                for (int r = 0; r < Config.Rows; r++)
                {
                    for (int c = 0; c < Config.Columns; c++)
                    {
                        if (Board[r, c] != 0) pieceCount++;
                    }
                }
                Console.WriteLine($"Total pieces on board: {pieceCount}");

                CurrentPlayer = gameData.GetProperty("CurrentPlayer").GetInt32();
                IsGameOver = gameData.GetProperty("IsGameOver").GetBoolean();
                Winner = gameData.GetProperty("Winner").GetString();
                GameId = gameData.GetProperty("GameId").GetString() ?? Guid.NewGuid().ToString();

                // Parse PlayerType - can be either string or number
                try
                {
                    var p1TypeValue = gameData.GetProperty("Player1Type");
                    var p2TypeValue = gameData.GetProperty("Player2Type");
                    
                    if (p1TypeValue.ValueKind == JsonValueKind.String)
                    {
                        Player1Type = Enum.TryParse<PlayerType>(p1TypeValue.GetString(), out var pt1) ? pt1 : PlayerType.Human;
                    }
                    else if (p1TypeValue.ValueKind == JsonValueKind.Number)
                    {
                        Player1Type = (PlayerType)p1TypeValue.GetInt32();
                    }
                    else
                    {
                        Player1Type = PlayerType.Human;
                    }
                    
                    if (p2TypeValue.ValueKind == JsonValueKind.String)
                    {
                        Player2Type = Enum.TryParse<PlayerType>(p2TypeValue.GetString(), out var pt2) ? pt2 : PlayerType.Human;
                    }
                    else if (p2TypeValue.ValueKind == JsonValueKind.Number)
                    {
                        Player2Type = (PlayerType)p2TypeValue.GetInt32();
                    }
                    else
                    {
                        Player2Type = PlayerType.Human;
                    }
                }
                catch
                {
                    Console.WriteLine("Failed to parse PlayerTypes, defaulting to Human");
                    Player1Type = PlayerType.Human;
                    Player2Type = PlayerType.Human;
                }
                
                Console.WriteLine($"=== LOADED from session === CurrentPlayer: {CurrentPlayer}, Pieces: {pieceCount}, P1: {Player1Type}, P2: {Player2Type}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Load FAILED === Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                InitializeNewGame();
            }
        }
    }
}