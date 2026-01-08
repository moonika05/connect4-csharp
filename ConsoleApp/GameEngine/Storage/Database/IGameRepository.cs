using System.Collections.Generic;
using ConsoleApp.GameEngine.Models;

namespace ConsoleApp.GameEngine.Storage.Database
{
    public interface IGameRepository
    {
        // Game State operations
        void SaveGame(GameState state, string? fileName = null);
        GameState? LoadGame(string identifier);
        List<string> GetAllSavedGames();
        bool DeleteGame(string identifier);
        
        // Configuration operations
        void SaveConfiguration(GameConfiguration config, string? fileName = null);
        GameConfiguration? LoadConfiguration(string identifier);
        List<string> GetAllSavedConfigurations();
        bool DeleteConfiguration(string identifier);
    }
}