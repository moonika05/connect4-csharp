using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleApp.GameEngine
{
    // Database model for game configurations
    // Entity Framework maps this to "GameConfigurations" table
    // Stores game settings presets (board size, win condition, cylinder mode)
    [Table("GameConfigurations")]  // SQL table name
    public class DbGameConfiguration
    {
        // Primary key - auto-increments (1, 2, 3...)
        // Unique identifier for each configuration
        [Key]
        public int Id { get; set; }
        
        // Configuration name - must be unique and non-empty
        // Examples: "Classic", "Speed Game", "Mega Board"
        // Max 256 characters, NOT NULL in database
        [Required]              // SQL: NOT NULL
        [MaxLength(256)]        // SQL: VARCHAR(256)
        public string Name { get; set; } = default!;  // default! = trust EF to initialize
        
        // Number of board rows (4-10)
        public int Rows { get; set; }
        
        // Number of board columns (4-10)
        public int Columns { get; set; }
        
        // How many pieces in a row to win (3-7)
        public int WinCondition { get; set; }
        
        // Whether board edges wrap around (cylinder mode)
        // SQL: BIT (0 = false, 1 = true)
        public bool IsCylinder { get; set; }
    }
}