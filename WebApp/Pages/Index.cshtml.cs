using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ConsoleApp.GameEngine;
using WebApp.Helpers;
using System.Text.Json;

namespace WebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IGameRepository _repository;

        public IndexModel(IGameRepository repository)
        {
            _repository = repository;
        }

        public List<string> SavedGames { get; set; } = new();
        public List<string> SavedConfigs { get; set; } = new();
        public GameConfiguration? LoadedConfig { get; set; }

        public void OnGet()
        {
            SavedGames = _repository.GetAllSavedGames();
            SavedConfigs = _repository.GetAllSavedConfigurations();
            
            // Check if there's a loaded config in session
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
                // No loaded config
            }
        }

        public IActionResult OnPostNewGame(string player1Type, string player2Type, 
            int rows = 6, int columns = 7, int winCondition = 4, 
            string? isCylinder = null)
        {
            // CLEAR OLD GAME from session!
            HttpContext.Session.Remove("CurrentGame");
    
            // Store player types in session
            HttpContext.Session.SetString("Player1Type", player1Type);
            HttpContext.Session.SetString("Player2Type", player2Type);
    
            // Parse cylinder - checkbox sends "on" when checked, null when not checked
            bool cylinderMode = isCylinder == "on" || isCylinder == "true";
    
            Console.WriteLine($"=== New Game Config === Rows:{rows}, Cols:{columns}, Win:{winCondition}, Cylinder:{cylinderMode} (raw:{isCylinder})");
    
            // Store configuration in session
            var config = new GameConfiguration("Custom", rows, columns, winCondition, cylinderMode);
            Console.WriteLine($"=== SAVED CONFIG === Name:{config.Name}, Cylinder:{config.IsCylinder}");  // <-- LISA SEE RIDA SIIA
            HttpContext.Session.SetObject("GameConfiguration", config);
    
            return RedirectToPage("/Game/Play");
        }

        public IActionResult OnPostLoadGame(string gameName)
        {
            if (string.IsNullOrEmpty(gameName))
                return Page();

            var state = _repository.LoadGame(gameName);
            if (state == null)
            {
                TempData["ErrorMessage"] = "Failed to load game!";
                return RedirectToPage();
            }

            // Clear old game and load this one
            HttpContext.Session.Remove("CurrentGame");
            
            // Store loaded game state
            var gameData = new
            {
                Config = state.Configuration,
                Board = state.Board,
                CurrentPlayer = state.CurrentPlayer,
                IsGameOver = state.IsGameOver,
                Winner = state.Winner,
                GameId = state.GameId,
                Player1Type = PlayerType.Human, // Default for loaded games
                Player2Type = PlayerType.Human
            };
            
            HttpContext.Session.SetObject("CurrentGame", gameData);
            
            return RedirectToPage("/Game/Play");
        }

        public IActionResult OnPostDeleteGame(string gameName)
        {
            if (string.IsNullOrEmpty(gameName))
                return RedirectToPage();

            try
            {
                _repository.DeleteGame(gameName);
                TempData["SuccessMessage"] = $"Game '{gameName}' deleted!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to delete game: {ex.Message}";
            }
            
            return RedirectToPage();
        }

        public IActionResult OnPostLoadConfig(string configName)
        {
            if (string.IsNullOrEmpty(configName))
                return RedirectToPage();

            var config = _repository.LoadConfiguration(configName);
            if (config == null)
            {
                TempData["ErrorMessage"] = "Failed to load configuration!";
                return RedirectToPage();
            }

            // Store configuration in session
            HttpContext.Session.SetObject("GameConfiguration", config);
            
            TempData["SuccessMessage"] = $"Configuration '{config.Name}' loaded! ({config.Rows}x{config.Columns}, Win:{config.WinCondition}, {(config.IsCylinder ? "Cylinder" : "Rectangle")})";
            
            return RedirectToPage(); // Stay on Index page
        }

        public IActionResult OnPostDeleteConfig(string configName)
        {
            if (string.IsNullOrEmpty(configName))
                return RedirectToPage();

            try
            {
                _repository.DeleteConfiguration(configName);
                TempData["SuccessMessage"] = $"Configuration '{configName}' deleted!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to delete configuration: {ex.Message}";
            }
            
            return RedirectToPage();
        }

        public IActionResult OnPostClearConfig()
        {
            // Remove loaded config from session
            HttpContext.Session.Remove("GameConfiguration");
            
            TempData["SuccessMessage"] = "Reset to Classic configuration!";
            
            return RedirectToPage();
        }
    }
}