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
            
            // Update repository instance based on session
            UpdateRepository();
            
            // Load saved games and configs
            SavedGames = _repository.GetAllSavedGames();
            SavedConfigs = _repository.GetAllSavedConfigurations();

            // Load configuration from session if exists
            try
            {
                var configData = HttpContext.Session.GetObject<JsonElement>("GameConfiguration");
                if (configData.ValueKind != JsonValueKind.Undefined)
                {
                    LoadedConfig = new GameConfiguration(
                        configData.GetProperty("Name").GetString() ?? "Custom",
                        configData.GetProperty("Rows").GetInt32(),
                        configData.GetProperty("Columns").GetInt32(),
                        configData.GetProperty("WinCondition").GetInt32(),
                        configData.GetProperty("IsCylinder").GetBoolean()
                    );
                }
            }
            catch
            {
                LoadedConfig = null;
            }
        }

        // NEW: Set repository type
        public IActionResult OnPostSetRepository(string repositoryType)
        {
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
            
            // Or clear everything:
            HttpContext.Session.Clear();
            
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

            // Save NEW game settings
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
            
            // ALSO save to TempData
            TempData["Config_Name"] = config.Name;
            TempData["Config_Rows"] = config.Rows;
            TempData["Config_Columns"] = config.Columns;
            TempData["Config_WinCondition"] = config.WinCondition;
            TempData["Config_IsCylinder"] = config.IsCylinder.ToString();
            
            Console.WriteLine($"New game session created");

            return RedirectToPage("/Game/Play");
        }

        public IActionResult OnPostLoadGame(string gameName)
        {
            UpdateRepository();
            
            var state = _repository.LoadGame(gameName);
            if (state == null)
            {
                TempData["ErrorMessage"] = $"Failed to load game: {gameName}";
                return RedirectToPage();
            }

            // Save to session
            HttpContext.Session.SetObject("CurrentGame", new
            {
                Config = state.Configuration,
                state.Board,
                state.CurrentPlayer,
                state.IsGameOver,
                state.Winner,
                state.GameId,
                Player1Type = PlayerType.Human,
                Player2Type = PlayerType.Human
            });

            HttpContext.Session.SetString("Player1Type", "Human");
            HttpContext.Session.SetString("Player2Type", "Human");

            TempData["SuccessMessage"] = $"Game '{gameName}' loaded!";
            return RedirectToPage("/Game/Play");
        }

        public IActionResult OnPostDeleteGame(string gameName)
        {
            UpdateRepository();
            
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
            
            var config = _repository.LoadConfiguration(configName);
            if (config == null)
            {
                TempData["ErrorMessage"] = $"Failed to load configuration: {configName}";
                return RedirectToPage();
            }

            HttpContext.Session.SetObject("GameConfiguration", config);
            TempData["SuccessMessage"] = $"Configuration '{config.Name}' loaded!";
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteConfig(string configName)
        {
            UpdateRepository();
            
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
            HttpContext.Session.Remove("GameConfiguration");
            TempData["SuccessMessage"] = "Configuration reset to Classic!";
            return RedirectToPage();
        }

        // Update repository instance based on session
        private void UpdateRepository()
        {
            var repositoryType = HttpContext.Session.GetString("RepositoryType") ?? "Json";
            
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