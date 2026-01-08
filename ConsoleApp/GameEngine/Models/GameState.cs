using System;
using System.Text.Json.Serialization;

namespace ConsoleApp.GameEngine.Models
{
    // GameState - complete snapshot of a game
    // Includes board state, configuration, and game progress
    // Used for saving/loading games
    public class GameState
    {
        public string GameId { get; set; }                     // Unique game identifier (Guid)
        public DateTime SavedAt { get; set; }                  // When game was saved
        public GameConfiguration Configuration { get; set; }   // Game rules/settings
        public int[,] Board { get; set; }                      // 2D array: board state
        public int CurrentPlayer { get; set; }                 // Whose turn: 1 or 2
        public bool IsGameOver { get; set; }                   // Whether game ended
        public string? Winner { get; set; }                    // Winner name or null
        
        // Primary constructor - used by JSON deserializer
        // Takes all game state parameters
        [JsonConstructor]
        public GameState(string gameId, DateTime savedAt, GameConfiguration configuration, 
            int[,] board, int currentPlayer, bool isGameOver, string? winner)
        {
            GameId = gameId;
            SavedAt = savedAt;
            Configuration = configuration;
            Board = board;
            CurrentPlayer = currentPlayer;
            IsGameOver = isGameOver;
            Winner = winner;
        }
        
        // Parameterless constructor - creates new empty game
        // Generates default values for new game
        public GameState()
        {
            GameId = Guid.NewGuid().ToString();        // Generate unique ID
            SavedAt = DateTime.Now;                    // Current timestamp
            Configuration = new GameConfiguration();   // Classic config
            Board = new int[6, 7];                     // Empty 6x7 board
            CurrentPlayer = 1;                         // Player 1 starts
            IsGameOver = false;                        // Game in progress
            Winner = null;                             // No winner yet
        }
    }
}