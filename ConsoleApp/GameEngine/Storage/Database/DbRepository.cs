using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ConsoleApp.GameEngine
{
    // Database repository implementation using Entity Framework + SQLite
    // IGameRepository implementation for database storage (alternative to JSON files)
    public class DbRepository : IGameRepository
    {
        private readonly AppDbContext _context;
        private readonly JsonSerializerOptions _jsonOptions;
        
        public DbRepository()
        {
            // Configure database connection (SQLite)
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=connect4.db")  // Database file: connect4.db
                .Options;
            
            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();  // Create database if doesn't exist
            
            // JSON options for serializing 2D arrays (Board)
            _jsonOptions = new JsonSerializerOptions
            {
                Converters = { new MultiDimensionalArrayConverter() }
            };
        }
        
        // ===== GAME STATE CRUD =====
        
        // Save game to database (insert or update)
        public void SaveGame(GameState state, string? fileName = null)
        {
            // Remove .json extension, generate name if null
            string saveName = fileName?.Replace(".json", "") 
                ?? $"game_{state.GameId}_{DateTime.Now:yyyyMMdd_HHmmss}";
            
            // Check if save already exists (update vs insert)
            var existing = _context.GameStates.FirstOrDefault(g => g.SaveName == saveName);
            
            if (existing != null)
            {
                // UPDATE existing record
                existing.SavedAt = state.SavedAt;
                existing.Rows = state.Configuration.Rows;
                existing.Columns = state.Configuration.Columns;
                existing.WinCondition = state.Configuration.WinCondition;
                existing.IsCylinder = state.Configuration.IsCylinder;
                existing.ConfigurationName = state.Configuration.Name;
                existing.BoardJson = JsonSerializer.Serialize(state.Board, _jsonOptions);  // 2D array → JSON string
                existing.CurrentPlayer = state.CurrentPlayer;
                existing.IsGameOver = state.IsGameOver;
                existing.Winner = state.Winner;
            }
            else
            {
                // INSERT new record
                var dbState = new DbGameState
                {
                    GameId = state.GameId,
                    SavedAt = state.SavedAt,
                    SaveName = saveName,
                    Rows = state.Configuration.Rows,
                    Columns = state.Configuration.Columns,
                    WinCondition = state.Configuration.WinCondition,
                    IsCylinder = state.Configuration.IsCylinder,
                    ConfigurationName = state.Configuration.Name,
                    BoardJson = JsonSerializer.Serialize(state.Board, _jsonOptions),  // 2D array → JSON string
                    CurrentPlayer = state.CurrentPlayer,
                    IsGameOver = state.IsGameOver,
                    Winner = state.Winner
                };
                
                _context.GameStates.Add(dbState);
            }
            
            _context.SaveChanges();  // SQL: INSERT or UPDATE
        }
        
        // Load game from database by name
        public GameState? LoadGame(string identifier)
        {
            // Remove .json extension (for compatibility with JSON repository)
            var saveName = identifier.Replace(".json", "");
            
            // SQL: SELECT * FROM GameStates WHERE SaveName = 'saveName' LIMIT 1
            var dbState = _context.GameStates.FirstOrDefault(g => g.SaveName == saveName);
            
            if (dbState == null)
                return null;
            
            // Reconstruct GameConfiguration from database fields
            var config = new GameConfiguration(
                dbState.ConfigurationName,
                dbState.Rows,
                dbState.Columns,
                dbState.WinCondition,
                dbState.IsCylinder
            );
            
            // Deserialize board: JSON string → 2D array
            var board = JsonSerializer.Deserialize<int[,]>(dbState.BoardJson, _jsonOptions);
            
            if (board == null)
                return null;
            
            // Reconstruct GameState
            return new GameState(
                dbState.GameId,
                dbState.SavedAt,
                config,
                board,
                dbState.CurrentPlayer,
                dbState.IsGameOver,
                dbState.Winner
            );
        }
        
        // Get all saved game names
        public List<string> GetAllSavedGames()
        {
            // SQL: SELECT SaveName FROM GameStates ORDER BY SavedAt DESC
            return _context.GameStates
                .OrderByDescending(g => g.SavedAt)  // Newest first
                .Select(g => g.SaveName + ".json")  // Add .json for compatibility
                .ToList();
        }
        
        // Delete game from database
        public bool DeleteGame(string identifier)
        {
            var saveName = identifier.Replace(".json", "");
            
            // SQL: SELECT * FROM GameStates WHERE SaveName = 'saveName'
            var dbState = _context.GameStates.FirstOrDefault(g => g.SaveName == saveName);
            
            if (dbState == null)
                return false;
            
            // SQL: DELETE FROM GameStates WHERE Id = ...
            _context.GameStates.Remove(dbState);
            _context.SaveChanges();
            return true;
        }
        
        // ===== CONFIGURATION CRUD =====
        
        // Save configuration to database (insert or update)
        public void SaveConfiguration(GameConfiguration config, string? fileName = null)
        {
            string saveName = fileName?.Replace(".json", "") ?? config.Name;
            
            // Check if config already exists (update vs insert)
            var existing = _context.GameConfigurations.FirstOrDefault(c => c.Name == saveName);
            
            if (existing != null)
            {
                // UPDATE existing record
                existing.Rows = config.Rows;
                existing.Columns = config.Columns;
                existing.WinCondition = config.WinCondition;
                existing.IsCylinder = config.IsCylinder;
            }
            else
            {
                // INSERT new record
                var dbConfig = new DbGameConfiguration
                {
                    Name = saveName,
                    Rows = config.Rows,
                    Columns = config.Columns,
                    WinCondition = config.WinCondition,
                    IsCylinder = config.IsCylinder
                };
                
                _context.GameConfigurations.Add(dbConfig);
            }
            
            _context.SaveChanges();  // SQL: INSERT or UPDATE
        }
        
        // Load configuration from database by name
        public GameConfiguration? LoadConfiguration(string identifier)
        {
            var saveName = identifier.Replace(".json", "");
            
            // SQL: SELECT * FROM GameConfigurations WHERE Name = 'saveName'
            var dbConfig = _context.GameConfigurations.FirstOrDefault(c => c.Name == saveName);
            
            if (dbConfig == null)
                return null;
            
            // Reconstruct GameConfiguration
            return new GameConfiguration(
                dbConfig.Name,
                dbConfig.Rows,
                dbConfig.Columns,
                dbConfig.WinCondition,
                dbConfig.IsCylinder
            );
        }
        
        // Get all saved configuration names
        public List<string> GetAllSavedConfigurations()
        {
            // SQL: SELECT Name FROM GameConfigurations ORDER BY Name ASC
            return _context.GameConfigurations
                .OrderBy(c => c.Name)  // Alphabetical order
                .Select(c => c.Name + ".json")  // Add .json for compatibility
                .ToList();
        }
        
        // Delete configuration from database
        public bool DeleteConfiguration(string identifier)
        {
            var saveName = identifier.Replace(".json", "");
            
            // SQL: SELECT * FROM GameConfigurations WHERE Name = 'saveName'
            var dbConfig = _context.GameConfigurations.FirstOrDefault(c => c.Name == saveName);
            
            if (dbConfig == null)
                return false;
            
            // SQL: DELETE FROM GameConfigurations WHERE Id = ...
            _context.GameConfigurations.Remove(dbConfig);
            _context.SaveChanges();
            return true;
        }
    }
}