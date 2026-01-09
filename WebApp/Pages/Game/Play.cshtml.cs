using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ConsoleApp.GameEngine;
using WebApp.Helpers;
using System.Text.Json;
using ConsoleApp.GameEngine.Models;
using ConsoleApp.GameEngine.Storage.Database;
using ConsoleApp.GameEngine.Storage.Json;

namespace WebApp.Pages.Game
{
    public class PlayModel : PageModel
    {
        private IGameRepository _repository;
        
        // Static dictionary for in-memory game storage (shared across all users)
        private static readonly Dictionary<string, GameState> _activeGames = new();
        private static readonly object _lock = new object();

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
        public string? ShareableLink { get; set; }
        

        public void OnGet(string? gameId = null, long? nocache = null)
        {
            Console.WriteLine($"=== OnGet CALLED === gameId: {gameId}, nocache: {nocache}");
            
            // PRIORITY 1: Load from in-memory storage (multiplayer)
            if (!string.IsNullOrEmpty(gameId))
            {
                Console.WriteLine($"Loading game from memory: {gameId}");
                LoadGameFromMemory(gameId);
                
                // Generate shareable link
                ShareableLink = $"{Request.Scheme}://{Request.Host}/Game/Play?gameId={GameId}";
                
                // CRITICAL: Do NOT load from session after loading from memory!
                // Session might have old data that would overwrite memory
            }
            else
            {
                // PRIORITY 2: Load from session (single player OR new game)
                LoadGameFromSession();
                
                // Add to memory if game exists (for potential sharing)
                if (!string.IsNullOrEmpty(GameId))
                {
                    SaveGameToMemory();
                    ShareableLink = $"{Request.Scheme}://{Request.Host}/Game/Play?gameId={GameId}";
                }
            }
            
            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"]?.ToString();
            }
            
            // AI vs AI - make ONE move only
            if (Player1Type != PlayerType.Human && Player2Type != PlayerType.Human && !IsGameOver)
            {
                Console.WriteLine("=== AI vs AI mode - making one move ===");
                
                var board = new GameBoard(Config);
                board.LoadBoardState(Board);
                
                MakeAIMove(board);
                
                // Save to memory and session
                SaveGameToMemory();
                SaveGameToSession();
                
                Console.WriteLine($"AI move completed. Game over: {IsGameOver}");
            }
            
            // DEBUG
            Console.WriteLine($"OnGet - Current Player: {CurrentPlayer}");
            Console.WriteLine($"Active games in memory: {_activeGames.Count}");
        }

        public IActionResult OnPostMove(int column)
        {
            Console.WriteLine($"\n=== OnPostMove CALLED === column: {column}");
            
            // Check if multiplayer (gameId in URL)
            var gameId = Request.Query["gameId"].ToString();
            
            if (!string.IsNullOrEmpty(gameId))
            {
                // MULTIPLAYER: Load from memory
                Console.WriteLine($"Multiplayer mode - loading from memory: {gameId}");
                LoadGameFromMemory(gameId);
            }
            else
            {
                // Single player: Load from session
                LoadGameFromSession();
            }
            
            Console.WriteLine($"BEFORE move - CurrentPlayer: {CurrentPlayer}");

            if (IsGameOver)
            {
                ErrorMessage = "Game is already over!";
                TempData["ErrorMessage"] = ErrorMessage;
                return RedirectToPage(new { gameId = GameId, nocache = DateTime.Now.Ticks });
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
                    
                    // Save to memory and session (NOT database)
                    SaveGameToMemory();
                    SaveGameToSession();
                    
                    Console.WriteLine($"HUMAN WON! Redirecting...");
                    return RedirectToPage(new { gameId = GameId, nocache = DateTime.Now.Ticks });
                }
                
                if (board.IsFull())
                {
                    IsGameOver = true;
                    Winner = "Draw";
                    
                    // Save to memory and session (NOT database)
                    SaveGameToMemory();
                    SaveGameToSession();
                    
                    Console.WriteLine($"DRAW! Redirecting...");
                    return RedirectToPage(new { gameId = GameId, nocache = DateTime.Now.Ticks });
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

                // Save to memory and session (NOT database)
                SaveGameToMemory();
                SaveGameToSession();
                
                Console.WriteLine($"Game saved to memory. Redirecting...");
                return RedirectToPage(new { gameId = GameId, nocache = DateTime.Now.Ticks });
            }
            else
            {
                ErrorMessage = "Invalid move! Column is full.";
                TempData["ErrorMessage"] = ErrorMessage;
                Console.WriteLine($"Invalid move! Redirecting...");
                return RedirectToPage(new { gameId = GameId, nocache = DateTime.Now.Ticks });
            }
        }

        public IActionResult OnPostSaveGame(string saveName, string? gameId)
        {
            // Update repository based on session setting
            UpdateRepository();

            // Check if multiplayer (gameId from form or query string)
            gameId ??= Request.Query["gameId"].ToString();
            if (!string.IsNullOrEmpty(gameId))
            {
                LoadGameFromMemory(gameId);
            }
            else
            {
                LoadGameFromSession();
            }

            if (string.IsNullOrWhiteSpace(saveName))
            {
                ErrorMessage = "Please enter a save name!";
                TempData["ErrorMessage"] = ErrorMessage;
                return RedirectToPage(new { gameId = GameId, nocache = DateTime.Now.Ticks });
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

            // NOW save to database (user requested)
            _repository.SaveGame(state, $"{saveName}.json");
            
            TempData["SuccessMessage"] = $"Game saved as '{saveName}'!";
            return RedirectToPage(new { gameId = GameId, nocache = DateTime.Now.Ticks });
        }

        public IActionResult OnPostSaveConfig(string configName, string? gameId)
        {
            // Update repository based on session setting
            UpdateRepository();

            // Check if multiplayer (gameId from form or query string)
            gameId ??= Request.Query["gameId"].ToString();
            if (!string.IsNullOrEmpty(gameId))
            {
                LoadGameFromMemory(gameId);
            }
            else
            {
                LoadGameFromSession();
            }

            if (string.IsNullOrWhiteSpace(configName))
            {
                ErrorMessage = "Please enter a config name!";
                TempData["ErrorMessage"] = ErrorMessage;
                return RedirectToPage(new { gameId = GameId, nocache = DateTime.Now.Ticks });
            }

            try
            {
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
            
            return RedirectToPage(new { gameId = GameId, nocache = DateTime.Now.Ticks });
        }

        public IActionResult OnPostNewGame(string player1Type, string player2Type, 
            int rows, int columns, int winCondition, bool isCylinder = false)
        {
            Console.WriteLine($"=== OnPostNewGame CALLED ===");
            Console.WriteLine($"isCylinder: {isCylinder}");
    
            // Validate
            if (rows < 4 || rows > 10)
            {
                TempData["ErrorMessage"] = "Rows must be between 4-10!";
                return RedirectToPage();
            }
    
            if (columns < 4 || columns > 10)
            {
                TempData["ErrorMessage"] = "Columns must be between 4-10!";
                return RedirectToPage();
            }
    
            if (winCondition < 3 || winCondition > 7)
            {
                TempData["ErrorMessage"] = "Win condition must be between 3-7!";
                return RedirectToPage();
            }

            // Save player types to session
            HttpContext.Session.SetString("Player1Type", player1Type);
            HttpContext.Session.SetString("Player2Type", player2Type);

            // Create configuration
            var config = new GameConfiguration("Custom", rows, columns, winCondition, isCylinder);
    
            Console.WriteLine($"=== Config created ===");
            Console.WriteLine($"Config.IsCylinder: {config.IsCylinder}");
    
            // Save configuration as individual session keys (MORE RELIABLE)
            HttpContext.Session.SetString("Config_Name", config.Name);
            HttpContext.Session.SetInt32("Config_Rows", config.Rows);
            HttpContext.Session.SetInt32("Config_Columns", config.Columns);
            HttpContext.Session.SetInt32("Config_WinCondition", config.WinCondition);
            HttpContext.Session.SetString("Config_IsCylinder", config.IsCylinder.ToString());
    
            Console.WriteLine($"=== Saved to session ===");
            Console.WriteLine($"Config_IsCylinder: {HttpContext.Session.GetString("Config_IsCylinder")}");

            return RedirectToPage("/Game/Play");
        }

        private void InitializeNewGame()
        {
            Console.WriteLine("\n========== InitializeNewGame ==========");
            
            // Try Session first, then TempData as fallback
            var p1Type = HttpContext.Session.GetString("Player1Type") ?? TempData["Player1Type"]?.ToString();
            var p2Type = HttpContext.Session.GetString("Player2Type") ?? TempData["Player2Type"]?.ToString();
            
            Console.WriteLine($"Player types: P1={p1Type}, P2={p2Type}");

            Player1Type = Enum.TryParse<PlayerType>(p1Type, out var pt1) ? pt1 : PlayerType.Human;
            Player2Type = Enum.TryParse<PlayerType>(p2Type, out var pt2) ? pt2 : PlayerType.Human;

            // Try Session first, then TempData
            var configName = HttpContext.Session.GetString("Config_Name") ?? TempData["Config_Name"]?.ToString();
            
            var configRows = HttpContext.Session.GetInt32("Config_Rows");
            if (configRows == null && TempData["Config_Rows"] != null)
            {
                configRows = int.Parse(TempData["Config_Rows"].ToString()!);
            }
            
            var configColumns = HttpContext.Session.GetInt32("Config_Columns");
            if (configColumns == null && TempData["Config_Columns"] != null)
            {
                configColumns = int.Parse(TempData["Config_Columns"].ToString()!);
            }
            
            var configWinCondition = HttpContext.Session.GetInt32("Config_WinCondition");
            if (configWinCondition == null && TempData["Config_WinCondition"] != null)
            {
                configWinCondition = int.Parse(TempData["Config_WinCondition"].ToString()!);
            }
            
            var configIsCylinderStr = HttpContext.Session.GetString("Config_IsCylinder") ?? TempData["Config_IsCylinder"]?.ToString();
            
            Console.WriteLine($"Reading from session/tempdata:");
            Console.WriteLine($"  - Config_Name: {configName}");
            Console.WriteLine($"  - Config_Rows: {configRows}");
            Console.WriteLine($"  - Config_Columns: {configColumns}");
            Console.WriteLine($"  - Config_WinCondition: {configWinCondition}");
            Console.WriteLine($"  - Config_IsCylinder: '{configIsCylinderStr}'");
            
            if (configRows.HasValue && configColumns.HasValue && configWinCondition.HasValue && !string.IsNullOrEmpty(configIsCylinderStr))
            {
                bool isCylinder = bool.Parse(configIsCylinderStr);
                Console.WriteLine($"  - Parsed isCylinder: {isCylinder}");
                
                Config = new GameConfiguration(
                    configName ?? "Custom",
                    configRows.Value,
                    configColumns.Value,
                    configWinCondition.Value,
                    isCylinder
                );
                
                Console.WriteLine($"Config object created:");
                Console.WriteLine($"  - Config.Name: {Config.Name}");
                Console.WriteLine($"  - Config.Rows: {Config.Rows}");
                Console.WriteLine($"  - Config.Columns: {Config.Columns}");
                Console.WriteLine($"  - Config.WinCondition: {Config.WinCondition}");
                Console.WriteLine($"  - Config.IsCylinder: {Config.IsCylinder}");
            }
            else
            {
                Config = GameConfiguration.Classic();
                Console.WriteLine("Using default Classic configuration");
                Console.WriteLine($"  WHY? configRows: {configRows}, configColumns: {configColumns}, configWinCondition: {configWinCondition}, configIsCylinderStr: '{configIsCylinderStr}'");
            }

            Board = new int[Config.Rows, Config.Columns];
            CurrentPlayer = 1;
            IsGameOver = false;
            Winner = null;
            GameId = Guid.NewGuid().ToString();

            // Save to memory and session
            SaveGameToMemory();
            SaveGameToSession();
            
            Console.WriteLine($"\nFINAL STATE:");
            Console.WriteLine($"  - Board: {Config.Rows}x{Config.Columns}");
            Console.WriteLine($"  - Cylinder: {Config.IsCylinder}");
            Console.WriteLine($"  - Players: P1={Player1Type}, P2={Player2Type}");
            Console.WriteLine($"==========================================\n");

            if (Player1Type != PlayerType.Human)
            {
                Console.WriteLine("Player 1 is AI, making first move...");
                var board = new GameBoard(Config);
                MakeAIMove(board);
                SaveGameToMemory();
                SaveGameToSession();
            }
        }

        private void MakeAIMove(GameBoard board)
        {
            PlayerType aiType = CurrentPlayer == 1 ? Player1Type : Player2Type;
            var ai = new GameAI(Config, GetAIDifficulty(aiType));

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

        // NEW: Save to in-memory storage (thread-safe)
        private void SaveGameToMemory()
        {
            lock (_lock)
            {
                var state = new GameState(
                    GameId,
                    DateTime.Now,
                    Config,
                    Board,
                    CurrentPlayer,
                    IsGameOver,
                    Winner
                );
                
                _activeGames[GameId] = state;
                Console.WriteLine($"=== SAVED to memory === GameId: {GameId}, Total games in memory: {_activeGames.Count}");
            }
        }

        // Load from in-memory storage
        private void LoadGameFromMemory(string gameId)
        {
            lock (_lock)
            {
                Console.WriteLine($"=== Loading from memory === GameId: {gameId}");
                
                if (_activeGames.TryGetValue(gameId, out var state))
                {
                    Config = state.Configuration;
                    Board = state.Board;
                    CurrentPlayer = state.CurrentPlayer;
                    IsGameOver = state.IsGameOver;
                    Winner = state.Winner;
                    GameId = state.GameId;
                    
                    // Get player types from session or default
                    var p1Type = HttpContext.Session.GetString("Player1Type");
                    var p2Type = HttpContext.Session.GetString("Player2Type");
                    Player1Type = Enum.TryParse<PlayerType>(p1Type, out var pt1) ? pt1 : PlayerType.Human;
                    Player2Type = Enum.TryParse<PlayerType>(p2Type, out var pt2) ? pt2 : PlayerType.Human;
                    
                    // OLULINE: Uuenda ka session memory'st laetud andmetega!
                    SaveGameToSession();
                    
                    Console.WriteLine($"=== LOADED from memory === GameId: {GameId}, CurrentPlayer: {CurrentPlayer}");
                }
                else
                {
                    // Game not in memory - try loading from database (saved games)
                    Console.WriteLine($"Game not found in memory: {gameId}, trying database");
                    var savedState = _repository.LoadGame($"{gameId}.json");
                    
                    if (savedState != null)
                    {
                        // Load from saved game
                        Config = savedState.Configuration;
                        Board = savedState.Board;
                        CurrentPlayer = savedState.CurrentPlayer;
                        IsGameOver = savedState.IsGameOver;
                        Winner = savedState.Winner;
                        GameId = savedState.GameId;
                        
                        var p1Type = HttpContext.Session.GetString("Player1Type");
                        var p2Type = HttpContext.Session.GetString("Player2Type");
                        Player1Type = Enum.TryParse<PlayerType>(p1Type, out var pt1) ? pt1 : PlayerType.Human;
                        Player2Type = Enum.TryParse<PlayerType>(p2Type, out var pt2) ? pt2 : PlayerType.Human;
                        
                        // Add to memory for future access
                        SaveGameToMemory();
                        SaveGameToSession();
                        
                        Console.WriteLine($"=== LOADED from database and added to memory === GameId: {GameId}");
                    }
                    else
                    {
                        // Game doesn't exist anywhere - this is an error for multiplayer
                        Console.WriteLine($"ERROR: Game {gameId} not found in memory or database!");
                        TempData["ErrorMessage"] = "Game not found. It may have expired or been deleted.";
                        
                        // Don't initialize new game - redirect to home
                        // This prevents overwriting another player's game
                    }
                }
            }
        }

        private void SaveGameToSession()
        {
            // Save config as individual keys
            HttpContext.Session.SetString("Config_Name", Config.Name);
            HttpContext.Session.SetInt32("Config_Rows", Config.Rows);
            HttpContext.Session.SetInt32("Config_Columns", Config.Columns);
            HttpContext.Session.SetInt32("Config_WinCondition", Config.WinCondition);
            HttpContext.Session.SetString("Config_IsCylinder", Config.IsCylinder.ToString());
    
            // Save board as JSON
            var options = new JsonSerializerOptions
            {
                Converters = { new WebApp.Helpers.MultiDimensionalArrayConverter() }
            };
            string boardJson = JsonSerializer.Serialize(Board, options);
            HttpContext.Session.SetString("Board", boardJson);
    
            // Save other game state
            HttpContext.Session.SetInt32("CurrentPlayer", CurrentPlayer);
            HttpContext.Session.SetString("IsGameOver", IsGameOver.ToString());
            HttpContext.Session.SetString("Winner", Winner ?? "");
            HttpContext.Session.SetString("GameId", GameId);
            HttpContext.Session.SetString("Player1Type", Player1Type.ToString());
            HttpContext.Session.SetString("Player2Type", Player2Type.ToString());

            Console.WriteLine($"=== SAVED to session === CurrentPlayer: {CurrentPlayer}, IsCylinder: {Config.IsCylinder}");
        }

        private void LoadGameFromSession()
        {
            // Check if there's an active game in session
            var gameId = HttpContext.Session.GetString("GameId");
            var boardJson = HttpContext.Session.GetString("Board");
            
            Console.WriteLine($"=== LoadGameFromSession ===");
            Console.WriteLine($"  GameId in session: {gameId}");
            Console.WriteLine($"  Board in session: {!string.IsNullOrEmpty(boardJson)}");
            
            // If NO active game (GameId or Board missing) → initialize new
            if (string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(boardJson))
            {
                Console.WriteLine("=== No active game in session, initializing new ===");
                InitializeNewGame();
                return;
            }
            
            // Otherwise, load existing game
            Console.WriteLine("=== Loading existing game from session ===");
            
            // Try to load config from individual keys
            var configName = HttpContext.Session.GetString("Config_Name") ?? TempData["Config_Name"]?.ToString();
            
            var configRows = HttpContext.Session.GetInt32("Config_Rows");
            if (configRows == null && TempData["Config_Rows"] != null)
            {
                configRows = int.Parse(TempData["Config_Rows"].ToString()!);
            }
            
            var configColumns = HttpContext.Session.GetInt32("Config_Columns");
            if (configColumns == null && TempData["Config_Columns"] != null)
            {
                configColumns = int.Parse(TempData["Config_Columns"].ToString()!);
            }
            
            var configWinCondition = HttpContext.Session.GetInt32("Config_WinCondition");
            if (configWinCondition == null && TempData["Config_WinCondition"] != null)
            {
                configWinCondition = int.Parse(TempData["Config_WinCondition"].ToString()!);
            }
            
            var configIsCylinderStr = HttpContext.Session.GetString("Config_IsCylinder") ?? TempData["Config_IsCylinder"]?.ToString();
            
            if (configRows.HasValue && configColumns.HasValue && configWinCondition.HasValue)
            {
                // Load config
                bool isCylinder = !string.IsNullOrEmpty(configIsCylinderStr) && bool.Parse(configIsCylinderStr);
                Config = new GameConfiguration(
                    configName ?? "Custom",
                    configRows.Value,
                    configColumns.Value,
                    configWinCondition.Value,
                    isCylinder
                );
                
                // Load board
                var options = new JsonSerializerOptions
                {
                    Converters = { new WebApp.Helpers.MultiDimensionalArrayConverter() }
                };
                Board = JsonSerializer.Deserialize<int[,]>(boardJson, options) ?? new int[Config.Rows, Config.Columns];
                
                // Load other state
                CurrentPlayer = HttpContext.Session.GetInt32("CurrentPlayer") ?? 1;
                var isGameOverStr = HttpContext.Session.GetString("IsGameOver");
                IsGameOver = !string.IsNullOrEmpty(isGameOverStr) && bool.Parse(isGameOverStr);
                Winner = HttpContext.Session.GetString("Winner");
                if (Winner == "") Winner = null;
                GameId = gameId;
                
                var p1TypeStr = HttpContext.Session.GetString("Player1Type") ?? TempData["Player1Type"]?.ToString();
                var p2TypeStr = HttpContext.Session.GetString("Player2Type") ?? TempData["Player2Type"]?.ToString();
                Player1Type = Enum.TryParse<PlayerType>(p1TypeStr, out var pt1) ? pt1 : PlayerType.Human;
                Player2Type = Enum.TryParse<PlayerType>(p2TypeStr, out var pt2) ? pt2 : PlayerType.Human;
                
                Console.WriteLine($"=== LOADED existing game === CurrentPlayer: {CurrentPlayer}, IsCylinder: {Config.IsCylinder}");
            }
            else
            {
                Console.WriteLine("=== Config missing, initializing new ===");
                InitializeNewGame();
            }
        }

        // Update repository instance based on session (same as Index.cshtml.cs)
        private void UpdateRepository()
        {
            var repositoryType = HttpContext.Session.GetString("RepositoryType") ?? "Json";

            Console.WriteLine($"Play.UpdateRepository: {repositoryType}");

            if (repositoryType == "Database")
            {
                _repository = new DbRepository();
            }
            else
            {
                _repository = new JsonRepository();
            }
        }
    }
}