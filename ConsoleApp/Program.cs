using MenuSystem;
using ConsoleApp.GameEngine;
using System;
using ConsoleApp.GameEngine.Models;
using ConsoleApp.GameEngine.Storage.Database;
using ConsoleApp.GameEngine.Storage.Json;


namespace ConsoleApp
{
    // Console application entry point
    // Main menu system for Connect4 game
    class Program
    {
        static void Main(string[] args)
        {
            // ===== REPOSITORY SELECTION =====
            // Let user choose between JSON files or SQLite database
            Console.WriteLine("=== Connect4 ===");
            Console.WriteLine("\nSelect storage type:");
            Console.WriteLine("1) JSON Files");
            Console.WriteLine("2) Database (SQLite)");
            Console.Write("\nYour choice (1 or 2): ");
            
            string? choice = Console.ReadLine()?.Trim();
            
            IGameRepository repository;
            
            // Dependency injection - use interface for flexibility
            if (choice == "2")
            {
                repository = new DbRepository();
                Console.WriteLine("\n✓ Using SQLite Database");
            }
            else
            {
                repository = new JsonRepository();  // Default
                Console.WriteLine("\n✓ Using JSON Files");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            
            // Create main menu and submenus
            var mainMenu = new Menu("Connect4 Main Menu", EMenuLevel.Root);
            var gameModeMenu = new Menu("Select Game Mode", EMenuLevel.First);
            var configMenu = new Menu("Game Configuration", EMenuLevel.First);
            var savedGamesMenu = new Menu("Saved Games", EMenuLevel.First);
            var savedConfigsMenu = new Menu("Saved Configurations", EMenuLevel.First);
            
            // Current configuration (shared state across menus)
            GameConfiguration currentConfig = GameConfiguration.Classic();
            
            // ===== MAIN MENU =====
            
            mainMenu.AddSubMenu("n", gameModeMenu);        // New Game
            mainMenu.AddSubMenu("l", savedGamesMenu);      // Load Game
            mainMenu.AddSubMenu("c", configMenu);          // Configuration
            mainMenu.AddSubMenu("s", savedConfigsMenu);    // Saved Configs
            
            // Show current configuration info
            mainMenu.AddMenuItem("i", $"Current: {currentConfig.Name}", () =>
            {
                Console.WriteLine($"\n=== Current Configuration ===");
                Console.WriteLine($"Board Size: {currentConfig.Rows} x {currentConfig.Columns}");
                Console.WriteLine($"Win Condition: {currentConfig.WinCondition} in a row");
                Console.WriteLine($"Board Type: {(currentConfig.IsCylinder ? "Cylinder" : "Rectangle")}");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                return "";  // Stay in menu
            });
            
            // ===== GAME MODE MENU =====
            // 6 game modes: Human vs Human, Human vs AI (Easy/Med/Hard), AI vs AI
            
            gameModeMenu.AddMenuItem("1", "Human vs Human", () =>
            {
                Console.WriteLine($"\nStarting: Human vs Human");
                Console.WriteLine($"Config: {currentConfig}");
                var game = new Connect4Game(currentConfig, repository, PlayerType.Human, PlayerType.Human);
                game.Play();
                return "";
            });
            
            gameModeMenu.AddMenuItem("2", "Human vs AI (Easy)", () =>
            {
                Console.WriteLine($"\nStarting: Human vs AI (Easy)");
                Console.WriteLine($"Config: {currentConfig}");
                var game = new Connect4Game(currentConfig, repository, PlayerType.Human, PlayerType.AIEasy);
                game.Play();
                return "";
            });
            
            gameModeMenu.AddMenuItem("3", "Human vs AI (Medium)", () =>
            {
                Console.WriteLine($"\nStarting: Human vs AI (Medium)");
                Console.WriteLine($"Config: {currentConfig}");
                var game = new Connect4Game(currentConfig, repository, PlayerType.Human, PlayerType.AIMedium);
                game.Play();
                return "";
            });
            
            gameModeMenu.AddMenuItem("4", "Human vs AI (Hard)", () =>
            {
                Console.WriteLine($"\nStarting: Human vs AI (Hard)");
                Console.WriteLine($"Config: {currentConfig}");
                var game = new Connect4Game(currentConfig, repository, PlayerType.Human, PlayerType.AIHard);
                game.Play();
                return "";
            });
            
            gameModeMenu.AddMenuItem("5", "AI vs AI (Easy vs Hard)", () =>
            {
                Console.WriteLine($"\nStarting: AI (Easy) vs AI (Hard)");
                Console.WriteLine($"Config: {currentConfig}");
                var game = new Connect4Game(currentConfig, repository, PlayerType.AIEasy, PlayerType.AIHard);
                game.Play();
                return "";
            });
            
            gameModeMenu.AddMenuItem("6", "AI vs AI (Medium vs Medium)", () =>
            {
                Console.WriteLine($"\nStarting: AI (Medium) vs AI (Medium)");
                Console.WriteLine($"Config: {currentConfig}");
                var game = new Connect4Game(currentConfig, repository, PlayerType.AIMedium, PlayerType.AIMedium);
                game.Play();
                return "";
            });
            
            // ===== CONFIGURATION MENU =====
            // Customize board size, win condition, cylinder mode
            
            // Rows (R)
            configMenu.AddMenuItem("r", $"Rows: {currentConfig.Rows}", () =>
            {
                Console.Write("\nEnter number of rows (4-10): ");
                if (int.TryParse(Console.ReadLine(), out int rows) && rows >= 4 && rows <= 10)
                {
                    currentConfig.Rows = rows;
                    currentConfig.Name = "Custom";
                    // Update menu labels dynamically
                    configMenu.UpdateMenuItemLabel("r", $"Rows: {currentConfig.Rows}");
                    mainMenu.UpdateMenuItemLabel("i", $"Current: {currentConfig.Name}");
                    Console.WriteLine($"✓ Rows set to {rows}");
                }
                else
                {
                    Console.WriteLine("✗ Invalid! Must be between 4-10.");
                }
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // Columns (C)
            configMenu.AddMenuItem("c", $"Columns: {currentConfig.Columns}", () =>
            {
                Console.Write("\nEnter number of columns (4-10): ");
                if (int.TryParse(Console.ReadLine(), out int cols) && cols >= 4 && cols <= 10)
                {
                    currentConfig.Columns = cols;
                    currentConfig.Name = "Custom";
                    configMenu.UpdateMenuItemLabel("c", $"Columns: {currentConfig.Columns}");
                    mainMenu.UpdateMenuItemLabel("i", $"Current: {currentConfig.Name}");
                    Console.WriteLine($"✓ Columns set to {cols}");
                }
                else
                {
                    Console.WriteLine("✗ Invalid! Must be between 4-10.");
                }
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // Win Condition (W)
            configMenu.AddMenuItem("w", $"Win Condition: {currentConfig.WinCondition}", () =>
            {
                Console.Write("\nEnter win condition (3-7): ");
                if (int.TryParse(Console.ReadLine(), out int win) && win >= 3 && win <= 7)
                {
                    currentConfig.WinCondition = win;
                    currentConfig.Name = "Custom";
                    configMenu.UpdateMenuItemLabel("w", $"Win Condition: {currentConfig.WinCondition}");
                    mainMenu.UpdateMenuItemLabel("i", $"Current: {currentConfig.Name}");
                    Console.WriteLine($"✓ Win condition set to {win}");
                }
                else
                {
                    Console.WriteLine("✗ Invalid! Must be between 3-7.");
                }
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // Type - Toggle Cylinder Mode (T)
            configMenu.AddMenuItem("t", $"Type: {(currentConfig.IsCylinder ? "Cylinder" : "Rectangle")}", () =>
            {
                currentConfig.IsCylinder = !currentConfig.IsCylinder;  // Toggle
                currentConfig.Name = "Custom";
                configMenu.UpdateMenuItemLabel("t", $"Type: {(currentConfig.IsCylinder ? "Cylinder" : "Rectangle")}");
                mainMenu.UpdateMenuItemLabel("i", $"Current: {currentConfig.Name}");
                Console.WriteLine($"\n✓ Board type: {(currentConfig.IsCylinder ? "Cylinder" : "Rectangle")}");
                if (currentConfig.IsCylinder)
                {
                    Console.WriteLine("   (Left and right edges wrap around)");
                }
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // Save Configuration (S)
            configMenu.AddMenuItem("s", "Save Current Configuration", () =>
            {
                Console.Write("\nEnter configuration name: ");
                string? name = Console.ReadLine()?.Trim();
                
                if (!string.IsNullOrWhiteSpace(name))
                {
                    currentConfig.Name = name;
                    repository.SaveConfiguration(currentConfig, $"{name}.json");
                    Console.WriteLine($"\n✓ Configuration '{name}' saved!");
                }
                else
                {
                    Console.WriteLine("\n✗ Invalid name!");
                }
                
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // Reset to Default (D)
            configMenu.AddMenuItem("d", "Reset to Default", () =>
            {
                currentConfig = GameConfiguration.Classic();
                // Update all menu labels
                configMenu.UpdateMenuItemLabel("r", $"Rows: {currentConfig.Rows}");
                configMenu.UpdateMenuItemLabel("c", $"Columns: {currentConfig.Columns}");
                configMenu.UpdateMenuItemLabel("w", $"Win Condition: {currentConfig.WinCondition}");
                configMenu.UpdateMenuItemLabel("t", $"Type: {(currentConfig.IsCylinder ? "Cylinder" : "Rectangle")}");
                mainMenu.UpdateMenuItemLabel("i", $"Current: {currentConfig.Name}");
                Console.WriteLine("\n✓ Configuration reset to Classic");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // ===== SAVED GAMES MENU =====
            
            // Refresh List (R)
            savedGamesMenu.AddMenuItem("r", "Refresh List", () =>
            {
                Console.WriteLine("\n✓ List refreshed!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // Load Game (L)
            savedGamesMenu.AddMenuItem("l", "Load Game", () =>
            {
                var games = repository.GetAllSavedGames();
                
                if (games.Count == 0)
                {
                    Console.WriteLine("\nNo saved games found!");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return "";
                }
                
                // Display list
                Console.WriteLine("\n=== Saved Games ===");
                for (int i = 0; i < games.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {games[i]}");
                }
                
                // Get user choice
                Console.Write("\nEnter number to load (or 0 to cancel): ");
                if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= games.Count)
                {
                    var state = repository.LoadGame(games[choice - 1]);
                    if (state != null)
                    {
                        Console.WriteLine($"\n✓ Loading game: {games[choice - 1]}");
                        Console.WriteLine("Note: Loaded games are Human vs Human mode");
                        Console.WriteLine("Press any key to start...");
                        Console.ReadKey();
                        
                        // Start game from loaded state
                        var game = new Connect4Game(state, repository, PlayerType.Human, PlayerType.Human);
                        game.Play();
                    }
                    else
                    {
                        Console.WriteLine("\n✗ Failed to load game!");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                    }
                }
                
                return "";
            });
            
            // Delete Game (D)
            savedGamesMenu.AddMenuItem("d", "Delete Game", () =>
            {
                var games = repository.GetAllSavedGames();
                
                if (games.Count == 0)
                {
                    Console.WriteLine("\nNo saved games found!");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return "";
                }
                
                // Display list
                Console.WriteLine("\n=== Saved Games ===");
                for (int i = 0; i < games.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {games[i]}");
                }
                
                // Get user choice
                Console.Write("\nEnter number to delete (or 0 to cancel): ");
                if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= games.Count)
                {
                    // Confirm deletion
                    Console.Write($"\nAre you sure you want to delete '{games[choice - 1]}'? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        if (repository.DeleteGame(games[choice - 1]))
                        {
                            Console.WriteLine("\n✓ Game deleted!");
                        }
                        else
                        {
                            Console.WriteLine("\n✗ Failed to delete game!");
                        }
                    }
                }
                
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // Delete All Games (A)
            savedGamesMenu.AddMenuItem("a", "Delete All Games", () =>
            {
                var games = repository.GetAllSavedGames();
                
                if (games.Count == 0)
                {
                    Console.WriteLine("\nNo saved games found!");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return "";
                }
                
                // Confirm deletion
                Console.Write($"\nAre you sure you want to delete ALL {games.Count} games? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    int deleted = 0;
                    foreach (var game in games)
                    {
                        if (repository.DeleteGame(game))
                            deleted++;
                    }
                    Console.WriteLine($"\n✓ Deleted {deleted} games!");
                }
                
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // ===== SAVED CONFIGURATIONS MENU =====
            
            // Refresh List (R)
            savedConfigsMenu.AddMenuItem("r", "Refresh List", () =>
            {
                Console.WriteLine("\n✓ List refreshed!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // Load Configuration (L)
            savedConfigsMenu.AddMenuItem("l", "Load Configuration", () =>
            {
                var configs = repository.GetAllSavedConfigurations();
                
                if (configs.Count == 0)
                {
                    Console.WriteLine("\nNo saved configurations found!");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return "";
                }
                
                // Display list
                Console.WriteLine("\n=== Saved Configurations ===");
                for (int i = 0; i < configs.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {configs[i]}");
                }
                
                // Get user choice
                Console.Write("\nEnter number to load (or 0 to cancel): ");
                if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= configs.Count)
                {
                    var config = repository.LoadConfiguration(configs[choice - 1]);
                    if (config != null)
                    {
                        currentConfig = config;
                        // Update all menu labels
                        configMenu.UpdateMenuItemLabel("r", $"Rows: {currentConfig.Rows}");
                        configMenu.UpdateMenuItemLabel("c", $"Columns: {currentConfig.Columns}");
                        configMenu.UpdateMenuItemLabel("w", $"Win Condition: {currentConfig.WinCondition}");
                        configMenu.UpdateMenuItemLabel("t", $"Type: {(currentConfig.IsCylinder ? "Cylinder" : "Rectangle")}");
                        mainMenu.UpdateMenuItemLabel("i", $"Current: {currentConfig.Name}");
                        
                        Console.WriteLine($"\n✓ Configuration '{currentConfig.Name}' loaded!");
                    }
                    else
                    {
                        Console.WriteLine("\n✗ Failed to load configuration!");
                    }
                }
                
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // Delete Configuration (D)
            savedConfigsMenu.AddMenuItem("d", "Delete Configuration", () =>
            {
                var configs = repository.GetAllSavedConfigurations();
                
                if (configs.Count == 0)
                {
                    Console.WriteLine("\nNo saved configurations found!");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return "";
                }
                
                // Display list
                Console.WriteLine("\n=== Saved Configurations ===");
                for (int i = 0; i < configs.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {configs[i]}");
                }
                
                // Get user choice
                Console.Write("\nEnter number to delete (or 0 to cancel): ");
                if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= configs.Count)
                {
                    // Confirm deletion
                    Console.Write($"\nAre you sure you want to delete '{configs[choice - 1]}'? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        if (repository.DeleteConfiguration(configs[choice - 1]))
                        {
                            Console.WriteLine("\n✓ Configuration deleted!");
                        }
                        else
                        {
                            Console.WriteLine("\n✗ Failed to delete configuration!");
                        }
                    }
                }
                
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return "";
            });
            
            // Start main menu loop
            mainMenu.Run();
        }
    }
}