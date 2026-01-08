using System;
using ConsoleApp.GameEngine.Models;
using ConsoleApp.GameEngine.Storage.Database;
using ConsoleApp.GameEngine.Storage.Json;

namespace ConsoleApp.GameEngine
{
    // Main game controller - orchestrates Connect4 gameplay
    // Handles game loop, player turns, AI, saving/loading
    public class Connect4Game
    {
        // Game state
        private GameBoard _board;
        private GameConfiguration _config;
        private int _currentPlayer;              // 1 or 2
        private bool _isGameOver;
        private string? _winner;                 // "Player 1 (Human)", "Draw", or null
        private readonly IGameRepository _repository;
        private string _gameId;                  // Unique game identifier (Guid)
        
        // Player configuration
        private readonly PlayerType _player1Type;
        private readonly PlayerType _player2Type;
        private GameAI? _ai1;                    // AI for player 1 (null if Human)
        private GameAI? _ai2;                    // AI for player 2 (null if Human)
        
        // Constructor for new game
        public Connect4Game(GameConfiguration? config = null, IGameRepository? repository = null, 
                           PlayerType player1Type = PlayerType.Human, PlayerType player2Type = PlayerType.Human)
        {
            // Initialize configuration
            _config = config ?? GameConfiguration.Classic();
            _board = new GameBoard(_config);
            _currentPlayer = 1;
            _isGameOver = false;
            _winner = null;
            _repository = repository ?? new JsonRepository();
            _gameId = Guid.NewGuid().ToString();
            
            // Set player types
            _player1Type = player1Type;
            _player2Type = player2Type;
            
            // Initialize AI for player 1 if needed
            if (_player1Type != PlayerType.Human)
            {
                _ai1 = new GameAI(_config, GetAIDifficulty(_player1Type));
            }
            
            // Initialize AI for player 2 if needed
            if (_player2Type != PlayerType.Human)
            {
                _ai2 = new GameAI(_config, GetAIDifficulty(_player2Type));
            }
        }
        
        // Constructor for loading saved game
        public Connect4Game(GameState state, IGameRepository? repository = null,
                           PlayerType player1Type = PlayerType.Human, PlayerType player2Type = PlayerType.Human)
        {
            // Load configuration and board from saved state
            _config = state.Configuration ?? GameConfiguration.Classic();
            _board = new GameBoard(_config);
            _board.LoadBoardState(state.Board);
            _currentPlayer = state.CurrentPlayer;
            _isGameOver = state.IsGameOver;
            _winner = state.Winner;
            _repository = repository ?? new JsonRepository();
            _gameId = state.GameId ?? Guid.NewGuid().ToString();
            
            // Set player types
            _player1Type = player1Type;
            _player2Type = player2Type;
            
            // Initialize AI if needed
            if (_player1Type != PlayerType.Human)
            {
                _ai1 = new GameAI(_config, GetAIDifficulty(_player1Type));
            }
            
            if (_player2Type != PlayerType.Human)
            {
                _ai2 = new GameAI(_config, GetAIDifficulty(_player2Type));
            }
        }
        
        // Convert PlayerType to AI difficulty (depth for minimax)
        private int GetAIDifficulty(PlayerType type)
        {
            return type switch
            {
                PlayerType.AIEasy => 1,      // Depth 1 + 80% random
                PlayerType.AIMedium => 2,    // Depth 2 (3 moves ahead)
                PlayerType.AIHard => 3,      // Depth 3 (5 moves ahead)
                _ => 2
            };
        }
        
        // Main game loop - runs until game over
        // Handles player turns, AI moves, win checking, saving
        public void Play()
        {
            // Display game info
            Console.Clear();
            Console.WriteLine($"Starting game: {_config}");
            Console.WriteLine($"Game ID: {_gameId}");
            Console.WriteLine($"Player 1 (X): {_player1Type}");
            Console.WriteLine($"Player 2 (O): {_player2Type}");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();
            
            // Main game loop
            while (!_isGameOver)
            {
                Console.Clear();
                _board.Display();
                
                // Determine current player type
                PlayerType currentPlayerType = _currentPlayer == 1 ? _player1Type : _player2Type;
                Console.WriteLine($"\nPlayer {_currentPlayer}'s turn ({(_currentPlayer == 1 ? 'X' : 'O')}) - {currentPlayerType}");
                
                int column;
                
                if (currentPlayerType == PlayerType.Human)
                {
                    // HUMAN TURN
                    Console.WriteLine("\nOptions: Enter column number, 's' to save, 'q' to quit");
                    string? input = GetPlayerInput();
                    
                    // Validate input
                    if (input == null || string.IsNullOrWhiteSpace(input))
                    {
                        Console.WriteLine("Invalid input! Try again.");
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    
                    // Handle save command
                    if (input == "s")
                    {
                        SaveGame();
                        if (_isGameOver) break;  // User chose to exit
                        continue;
                    }
                    
                    // Handle quit command
                    if (input == "q")
                    {
                        Console.WriteLine("\nQuitting game...");
                        Console.Write("Do you want to save before quitting? (y/n): ");
                        string? quitInput = Console.ReadLine()?.ToLower();
                        if (quitInput == "y")
                        {
                            SaveGame();
                        }
                        break;
                    }
                    
                    // Parse column number (1-based input → 0-based array)
                    if (!int.TryParse(input, out column) || column < 1 || column > _board.Columns)
                    {
                        Console.WriteLine("Invalid input! Try again.");
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    
                    column--;  // Convert to 0-indexed (user sees 1-7, array is 0-6)
                }
                else
                {
                    // AI TURN
                    GameAI? ai = _currentPlayer == 1 ? _ai1 : _ai2;
                    if (ai == null)
                    {
                        Console.WriteLine("Error: AI not initialized!");
                        break;
                    }
                    
                    // Get best move from AI (minimax algorithm)
                    column = ai.GetBestMove(_board.GetBoardState(), _currentPlayer);
                    
                    // Pause for user to see AI thinking
                    Console.WriteLine("\nPress any key to see AI move...");
                    Console.ReadKey();
                }
                
                // MAKE THE MOVE
                if (_board.DropPiece(column, _currentPlayer))
                {
                    AnimateDrop(column);  // Visual feedback
                    
                    // Check win condition
                    if (_board.CheckWin(_currentPlayer))
                    {
                        Console.Clear();
                        _board.Display();
                        string playerName = $"Player {_currentPlayer} ({(_currentPlayer == 1 ? _player1Type : _player2Type)})";
                        Console.WriteLine($"\n🎉 {playerName} wins! 🎉");
                        _isGameOver = true;
                        _winner = playerName;
                    }
                    // Check draw (board full)
                    else if (_board.IsFull())
                    {
                        Console.Clear();
                        _board.Display();
                        Console.WriteLine("\n🤝 It's a draw! 🤝");
                        _isGameOver = true;
                        _winner = "Draw";
                    }
                    // Continue game - switch player
                    else
                    {
                        _currentPlayer = _currentPlayer == 1 ? 2 : 1;
                    }
                }
                else
                {
                    // Invalid move (column full)
                    Console.WriteLine("Invalid move! Column is full.");
                    System.Threading.Thread.Sleep(1000);
                }
            }
            
            Console.WriteLine("\nPress any key to return to menu...");
            Console.ReadKey();
        }

        // Get input from human player
        private string? GetPlayerInput()
        {
            Console.Write("Your choice: ");
            return Console.ReadLine()?.Trim().ToLower();
        }
        
        // Save game to repository
        // Prompts user for save name, handles overwrite confirmation
        private void SaveGame()
        {
            // Create game state snapshot
            var state = new GameState
            {
                GameId = _gameId,
                SavedAt = DateTime.Now,
                Configuration = _config,
                Board = _board.GetBoardState(),
                CurrentPlayer = _currentPlayer,
                IsGameOver = _isGameOver,
                Winner = _winner
            };
            
            string? fileName = null;
            bool validName = false;
            
            // Get save name from user
            while (!validName)
            {
                Console.Write("\nEnter save name (or press Enter for auto-name): ");
                string? saveName = Console.ReadLine()?.Trim();
                
                // Auto-generate name if empty
                if (string.IsNullOrWhiteSpace(saveName))
                {
                    fileName = null;  // Repository will auto-generate
                    validName = true;
                }
                else
                {
                    fileName = $"{saveName}.json";
                    
                    // Check if file exists
                    var existingGames = _repository.GetAllSavedGames();
                    if (existingGames.Contains(fileName))
                    {
                        // Confirm overwrite
                        Console.Write($"\nFile '{fileName}' already exists. Overwrite? (y/n): ");
                        string? answer = Console.ReadLine()?.ToLower();
                        if (answer == "y")
                        {
                            validName = true;
                        }
                        else
                        {
                            Console.WriteLine("Please choose a different name.");
                        }
                    }
                    else
                    {
                        validName = true;
                    }
                }
            }
            
            // Save to repository
            _repository.SaveGame(state, fileName);
            
            Console.WriteLine($"\n✓ Game saved successfully!");
            
            // Ask what to do next
            Console.WriteLine("\nWhat would you like to do?");
            Console.WriteLine("1) Continue playing");
            Console.WriteLine("2) Return to main menu");
            Console.Write("Your choice: ");
            
            string? choice = Console.ReadLine()?.Trim();
            if (choice == "2")
            {
                _isGameOver = true;  // Exit game loop
            }
            else
            {
                Console.WriteLine("\nContinuing game...");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
        
        // Simple animation for piece drop (visual feedback)
        private void AnimateDrop(int column)
        {
            for (int i = 0; i < 3; i++)
            {
                System.Threading.Thread.Sleep(100);
            }
        }
        
        // Get current game state (for external access)
        // Used by web app to sync state
        public GameState GetCurrentState()
        {
            return new GameState
            {
                GameId = _gameId,
                SavedAt = DateTime.Now,
                Configuration = _config,
                Board = _board.GetBoardState(),
                CurrentPlayer = _currentPlayer,
                IsGameOver = _isGameOver,
                Winner = _winner
            };
        }
    }
}
