using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleApp.GameEngine.Storage.Database
{
    // Database model for saved games - maps to "GameStates" table
    // Stores complete game state: board, players, configuration
    [Table("GameStates")]
    public class DbGameState
    {
        // Primary key - auto-increments (1, 2, 3...)
        [Key]
        public int Id { get; set; }
        
        // Game unique identifier (Guid string)
        // Example: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
        [Required]
        [MaxLength(128)]
        public string GameId { get; set; } = default!;
        
        // Timestamp when game was saved
        // SQL: DATETIME
        public DateTime SavedAt { get; set; }
        
        // User-provided save name (must be unique)
        // Examples: "my-game", "backup-1"
        // Uniqueness enforced in AppDbContext.OnModelCreating
        [Required]
        [MaxLength(256)]
        public string SaveName { get; set; } = default!;
        
        // === CONFIGURATION (embedded, not separate table) ===
        
        // Board rows (4-10)
        public int Rows { get; set; }
        
        // Board columns (4-10)
        public int Columns { get; set; }
        
        // Win condition - pieces in a row to win (3-7)
        public int WinCondition { get; set; }
        
        // Whether board edges wrap around (cylinder mode)
        // SQL: BIT (0 or 1)
        public bool IsCylinder { get; set; }
        
        // Configuration name (e.g., "Classic", "Custom")
        [MaxLength(128)]
        public string ConfigurationName { get; set; } = "Custom";
        
        // === GAME STATE ===
        
        // Board state stored as JSON string
        // 2D array serialized: "[[0,0,1],[0,2,1],...]"
        // Stored as text because SQL doesn't support 2D arrays
        // SQL: NVARCHAR(MAX) or TEXT
        [Required]
        public string BoardJson { get; set; } = default!;
        
        // Current player's turn (1 or 2)
        public int CurrentPlayer { get; set; }
        
        // Whether game has ended
        // SQL: BIT
        public bool IsGameOver { get; set; }
        
        // Winner name if game over (nullable)
        // Examples: "Player 1 (Human)", "Draw", or null
        [MaxLength(128)]
        public string? Winner { get; set; }
    }
}