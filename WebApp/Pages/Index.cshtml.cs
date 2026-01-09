using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ConsoleApp.GameEngine;
using WebApp.Helpers;
using System.Text.Json;
using ConsoleApp.GameEngine.Models;
using ConsoleApp.GameEngine.Storage.Database;
using ConsoleApp.GameEngine.Storage.Json;

namespace WebApp.Pages
{
    public class IndexModel : PageModel
    {
        private IGameRepository _repository;

        public IndexModel(IGameRepository repository)
        {
            _repository = repository;
        }

        public List<string> SavedGames { get; set; } = new();
        public List<string> SavedConfigs { get; set; } = new();
        public GameConfiguration? LoadedConfig { get; set; }
        public string CurrentRepository { get; set; } = "Json";

        public void OnGet()
        {
            // Get current repository type from session (default: Json)
            CurrentRepository = HttpContext.Session.GetString("RepositoryType") ?? "Json";
            
            Console.WriteLine($"=== Index.OnGet === CurrentRepository: {CurrentRepository}");
            
            // Update repository instance based on session
            UpdateRepository();
            
            // Load saved games and configs
            SavedGames = _repository.GetAllSavedGames();
            SavedConfigs = _repository.GetAllSavedConfigurations();
            
            Console.WriteLine($"Found {SavedGames.Count} games and {SavedConfigs.Count} configs");

            // Load configuration from session if exists
            try
            {
                var configName = HttpContext.Session.GetString("Config_Name");
                var configRows = HttpContext.Session.GetInt32("Config_Rows");
                var configColumns = HttpContext.Session.GetInt32("Config_Columns");
                var configWinCondition = HttpContext.Session.GetInt32("Config_WinCondition");
                var configIsCylinderStr = HttpContext.Session.GetString("Config_IsCylinder");
                
                if (configRows.HasValue && configColumns.HasValue && configWinCondition.HasValue)
                {
                    bool isCylinder = !string.IsNullOrEmpty(configIsCylinderStr) && bool.Parse(configIsCylinderStr);
                    LoadedConfig = new GameConfiguration(
                        configName ?? "Custom",
                        configRows.Value,
                        configColumns.Value,
                        configWinCondition.Value,
                        isCylinder
                    );
                }
            }
            catch
            {
                LoadedConfig = null;
            }
        }

        public IActionResult OnPostSetRepository(string repositoryType)
        {
            Console.WriteLine($"=== OnPostSetRepository === Type: {repositoryType}");
            
            // Save repository type to session
            HttpContext.Session.SetString("RepositoryType", repositoryType);
            
            TempData["SuccessMessage"] = $"Storage method changed to: {repositoryType}";
            return RedirectToPage();
        }

        public IActionResult OnPostNewGame(string player1Type, string player2Type, 
            int rows, int columns, int winCondition, bool isCylinder = false)
        {
            Console.WriteLine($"\n========== OnPostNewGame ==========");
            Console.WriteLine($"Starting NEW game - clearing old session data");
            
            // CLEAR OLD GAME DATA
            HttpContext.Session.Remove("Board");
            HttpContext.Session.Remove("CurrentPlayer");
            HttpContext.Session.Remove("IsGameOver");
            HttpContext.Session.Remove("Winner");
            HttpContext.Session.Remove("GameId");
            
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

            // Save to BOTH Session AND TempData
            HttpContext.Session.SetString("Player1Type", player1Type);
            HttpContext.Session.SetString("Player2Type", player2Type);
            TempData["Player1Type"] = player1Type;
            TempData["Player2Type"] = player2Type;

            var config = new GameConfiguration("Custom", rows, columns, winCondition, isCylinder);
            
            Console.WriteLine($"Config.IsCylinder: {config.IsCylinder}");
            
            // Save config
            HttpContext.Session.SetString("Config_Name", config.Name);
            HttpContext.Session.SetInt32("Config_Rows", config.Rows);
            HttpContext.Session.SetInt32("Config_Columns", config.Columns);
            HttpContext.Session.SetInt32("Config_WinCondition", config.WinCondition);
            HttpContext.Session.SetString("Config_IsCylinder", config.IsCylinder.ToString());
            
            TempData["Config_Name"] = config.Name;
            TempData["Config_Rows"] = config.Rows;
            TempData["Config_Columns"] = config.Columns;
            TempData["Config_WinCondition"] = config.WinCondition;
            TempData["Config_IsCylinder"] = config.IsCylinder.ToString();

            return RedirectToPage("/Game/Play");
        }

        public IActionResult OnPostLoadGame(string gameName)
        {
            UpdateRepository();
            
            Console.WriteLine($"=== OnPostLoadGame === Game: {gameName}, Repository: {CurrentRepository}");
            
            var state = _repository.LoadGame(gameName);
            if (state == null)
            {
                TempData["ErrorMessage"] = $"Failed to load game: {gameName}";
                return RedirectToPage();
            }

            // Clear old session
            HttpContext.Session.Clear();

            // Save config
            HttpContext.Session.SetString("Config_Name", state.Configuration.Name);
            HttpContext.Session.SetInt32("Config_Rows", state.Configuration.Rows);
            HttpContext.Session.SetInt32("Config_Columns", state.Configuration.Columns);
            HttpContext.Session.SetInt32("Config_WinCondition", state.Configuration.WinCondition);
            HttpContext.Session.SetString("Config_IsCylinder", state.Configuration.IsCylinder.ToString());
            
            // Save board
            var options = new JsonSerializerOptions
            {
                Converters = { new WebApp.Helpers.MultiDimensionalArrayConverter() }
            };
            string boardJson = JsonSerializer.Serialize(state.Board, options);
            HttpContext.Session.SetString("Board", boardJson);
            
            // Save game state
            HttpContext.Session.SetInt32("CurrentPlayer", state.CurrentPlayer);
            HttpContext.Session.SetString("IsGameOver", state.IsGameOver.ToString());
            HttpContext.Session.SetString("Winner", state.Winner ?? "");
            HttpContext.Session.SetString("GameId", state.GameId);
            HttpContext.Session.SetString("Player1Type", "Human");
            HttpContext.Session.SetString("Player2Type", "Human");

            TempData["SuccessMessage"] = $"Game '{gameName}' loaded!";
            return RedirectToPage("/Game/Play");
        }

        public IActionResult OnPostDeleteGame(string gameName)
        {
            UpdateRepository();
            
            Console.WriteLine($"=== OnPostDeleteGame === Game: {gameName}, Repository: {CurrentRepository}");
            
            if (_repository.DeleteGame(gameName))
            {
                TempData["SuccessMessage"] = $"Game '{gameName}' deleted!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to delete game: {gameName}";
            }
            return RedirectToPage();
        }

        public IActionResult OnPostLoadConfig(string configName)
        {
            UpdateRepository();
            
            Console.WriteLine($"=== OnPostLoadConfig === Config: {configName}, Repository: {CurrentRepository}");
            
            var config = _repository.LoadConfiguration(configName);
            if (config == null)
            {
                TempData["ErrorMessage"] = $"Failed to load configuration: {configName}";
                return RedirectToPage();
            }

            HttpContext.Session.SetString("Config_Name", config.Name);
            HttpContext.Session.SetInt32("Config_Rows", config.Rows);
            HttpContext.Session.SetInt32("Config_Columns", config.Columns);
            HttpContext.Session.SetInt32("Config_WinCondition", config.WinCondition);
            HttpContext.Session.SetString("Config_IsCylinder", config.IsCylinder.ToString());
            
            TempData["SuccessMessage"] = $"Configuration '{config.Name}' loaded!";
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteConfig(string configName)
        {
            UpdateRepository();
            
            Console.WriteLine($"=== OnPostDeleteConfig === Config: {configName}, Repository: {CurrentRepository}");
            
            if (_repository.DeleteConfiguration(configName))
            {
                TempData["SuccessMessage"] = $"Configuration '{configName}' deleted!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to delete configuration: {configName}";
            }
            return RedirectToPage();
        }

        public IActionResult OnPostClearConfig()
        {
            HttpContext.Session.Remove("Config_Name");
            HttpContext.Session.Remove("Config_Rows");
            HttpContext.Session.Remove("Config_Columns");
            HttpContext.Session.Remove("Config_WinCondition");
            HttpContext.Session.Remove("Config_IsCylinder");
            
            TempData["SuccessMessage"] = "Configuration reset to Classic!";
            return RedirectToPage();
        }

        // Update repository instance based on session
        private void UpdateRepository()
        {
            var repositoryType = HttpContext.Session.GetString("RepositoryType") ?? "Json";
            
            Console.WriteLine($"UpdateRepository: {repositoryType}");
            
            if (repositoryType == "Database")
            {
                _repository = new DbRepository();
            }
            else
            {
                _repository = new JsonRepository();
            }
            
            CurrentRepository = repositoryType;
        }
    }
}