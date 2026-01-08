using System;
using System.Text.Json.Serialization;

namespace ConsoleApp.GameEngine
{
    // Game configuration - board size, win condition, cylinder mode
    // Represents game rules/settings (not game state)
    public class GameConfiguration
    {
        public string Name { get; set; }           // Configuration name: "Classic", "Custom"
        public int Rows { get; set; }              // Board rows: 4-10
        public int Columns { get; set; }           // Board columns: 4-10
        public int WinCondition { get; set; }      // Pieces in a row to win: 3-7
        public bool IsCylinder { get; set; }       // Whether edges wrap around
        
        // Primary constructor - used by JSON deserializer
        [JsonConstructor]
        public GameConfiguration(string name, int rows, int columns, int winCondition, bool isCylinder)
        {
            Name = name;
            Rows = rows;
            Columns = columns;
            WinCondition = winCondition;
            IsCylinder = isCylinder;
        }
        
        // Parameterless constructor - calls primary with Classic defaults
        // Required for some JSON deserialization scenarios
        public GameConfiguration() : this("Classic", 6, 7, 4, false)
        {
        }
        
        // Static factory methods - predefined configurations
        // Usage: var config = GameConfiguration.Classic();
        
        public static GameConfiguration Classic() => 
            new GameConfiguration("Classic", 6, 7, 4, false);  // Standard Connect4
        
        public static GameConfiguration Connect3() => 
            new GameConfiguration("Connect3", 5, 6, 3, false);  // Smaller board, 3 to win
        
        public static GameConfiguration Connect5() => 
            new GameConfiguration("Connect5", 8, 9, 5, false);  // Larger board, 5 to win
        
        public static GameConfiguration Cylinder() => 
            new GameConfiguration("Cylinder", 6, 7, 4, true);  // Classic with wrap-around
        
        // String representation for display
        // Output: "Classic (6x7, Win:4, Rectangle)"
        public override string ToString()
        {
            return $"{Name} ({Rows}x{Columns}, Win:{WinCondition}, {(IsCylinder ? "Cylinder" : "Rectangle")})";
        }
    }
}