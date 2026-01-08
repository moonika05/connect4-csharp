using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleApp.GameEngine.Models;
using ConsoleApp.GameEngine.Storage.Database;

namespace ConsoleApp.GameEngine.Storage.Json
{
    // JSON file-based repository implementation
    // Saves games and configs as JSON files in "saves/" folder
    public class JsonRepository : IGameRepository
    {
        private readonly string _gamesFolder;      // "saves/games/"
        private readonly string _configsFolder;    // "saves/configs/"
        
        private readonly JsonSerializerOptions _jsonOptions;
        
        public JsonRepository()
        {
            // Set up folder paths
            _gamesFolder = Path.Combine("saves", "games");
            _configsFolder = Path.Combine("saves", "configs");
            
            // Create folders if they don't exist
            Directory.CreateDirectory(_gamesFolder);
            Directory.CreateDirectory(_configsFolder);
            
            // Configure JSON serialization
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,                              // Pretty-print (readable)
                PropertyNameCaseInsensitive = true,                // "name" = "Name"
                Converters = { new MultiDimensionalArrayConverter() }  // 2D array support
            };
        }
        
        // ===== GAME STATE CRUD =====
        
        // Save game as JSON file
        public void SaveGame(GameState state, string? fileName = null)
        {
            // Auto-generate filename if null: "game_abc123_20250108_143045.json"
            fileName ??= $"game_{state.GameId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(_gamesFolder, fileName);
            
            // Serialize to JSON string
            string json = JsonSerializer.Serialize(state, _jsonOptions);
            
            // Write to file
            File.WriteAllText(filePath, json);
        }
        
        // Load game from JSON file
        public GameState? LoadGame(string fileName)
        {
            string filePath = Path.Combine(_gamesFolder, fileName);
            
            // Check if file exists
            if (!File.Exists(filePath))
                return null;
            
            // Read file and deserialize
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<GameState>(json, _jsonOptions);
        }
        
        // Get list of all saved game filenames
        public List<string> GetAllSavedGames()
        {
            if (!Directory.Exists(_gamesFolder))
                return new List<string>();
            
            // Get all .json files, extract filenames, sort by newest first
            return Directory.GetFiles(_gamesFolder, "*.json")
                           .Select(Path.GetFileName)           // Full path → filename
                           .Where(f => f != null)              // Filter nulls
                           .Select(f => f!)                    // Non-null assertion
                           .OrderByDescending(f => f)          // Sort Z→A (newest first)
                           .ToList();
        }
        
        // Delete game file
        public bool DeleteGame(string fileName)
        {
            string filePath = Path.Combine(_gamesFolder, fileName);
            
            if (!File.Exists(filePath))
                return false;
            
            File.Delete(filePath);
            return true;
        }
        
        // ===== CONFIGURATION CRUD =====
        
        // Save configuration as JSON file
        public void SaveConfiguration(GameConfiguration config, string? fileName = null)
        {
            // Auto-generate filename if null: "config_Classic_20250108_143045.json"
            fileName ??= $"config_{config.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(_configsFolder, fileName);
            
            string json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        // Load configuration from JSON file
        public GameConfiguration? LoadConfiguration(string fileName)
        {
            string filePath = Path.Combine(_configsFolder, fileName);
            
            if (!File.Exists(filePath))
                return null;
            
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<GameConfiguration>(json, _jsonOptions);
        }
        
        // Get list of all saved configuration filenames
        public List<string> GetAllSavedConfigurations()
        {
            if (!Directory.Exists(_configsFolder))
                return new List<string>();
            
            return Directory.GetFiles(_configsFolder, "*.json")
                           .Select(Path.GetFileName)
                           .Where(f => f != null)
                           .Select(f => f!)
                           .OrderByDescending(f => f)
                           .ToList();
        }
        
        // Delete configuration file
        public bool DeleteConfiguration(string fileName)
        {
            string filePath = Path.Combine(_configsFolder, fileName);
            
            if (!File.Exists(filePath))
                return false;
            
            File.Delete(filePath);
            return true;
        }
    }
    
    // Custom JSON converter for 2D arrays
    // JSON doesn't natively support int[,], so convert to/from jagged array int[][]
    public class MultiDimensionalArrayConverter : JsonConverter<int[,]>
    {
        // Deserialize: JSON → jagged array → 2D array
        public override int[,]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read as jagged array: [[0,1,2],[3,4,5]]
            var jaggedArray = JsonSerializer.Deserialize<int[][]>(ref reader, options);
            if (jaggedArray == null) return null;
            
            // Get dimensions
            int rows = jaggedArray.Length;
            int cols = jaggedArray[0].Length;
            
            // Create 2D array
            var result = new int[rows, cols];
            
            // Copy data: jagged → 2D
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i, j] = jaggedArray[i][j];
            
            return result;
        }
        
        // Serialize: 2D array → jagged array → JSON
        public override void Write(Utf8JsonWriter writer, int[,] value, JsonSerializerOptions options)
        {
            // Get dimensions
            int rows = value.GetLength(0);
            int cols = value.GetLength(1);
            
            // Create jagged array
            var jaggedArray = new int[rows][];
            
            // Copy data: 2D → jagged
            for (int i = 0; i < rows; i++)
            {
                jaggedArray[i] = new int[cols];
                for (int j = 0; j < cols; j++)
                    jaggedArray[i][j] = value[i, j];
            }
            
            // Serialize jagged array to JSON
            JsonSerializer.Serialize(writer, jaggedArray, options);
        }
    }
}