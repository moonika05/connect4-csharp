using System;
using System.Linq;
using ConsoleApp.GameEngine.Models;

namespace ConsoleApp.GameEngine
{
    // GameBoard manages the Connect4 board state and game logic
    // Handles piece placement, win checking, board display
    public class GameBoard
    {
        private readonly int[,] _board; // 2D array: 0=empty, 1=Player1(X), 2=Player2(O)
        private readonly GameConfiguration _config;

        // Shortcut properties for board dimensions
        public int Rows => _config.Rows;
        public int Columns => _config.Columns;

        public GameBoard(GameConfiguration config)
        {
            _config = config;
            _board = new int[config.Rows, config.Columns]; // Initialize empty board (all 0s)
        }

        // Display board in console with ASCII art
        public void Display()
        {
            // Header: "Classic - Normal Mode"
            Console.WriteLine($"\n  {_config.Name} - {(_config.IsCylinder ? "Cylinder Mode" : "Normal Mode")}");

            // Column numbers: "  1 2 3 4 5 6 7"
            Console.WriteLine("  " + string.Join(" ", Enumerable.Range(1, Columns)));

            // Top border: " +-------------+"
            Console.WriteLine(" +" + new string('-', Columns * 2 - 1) + "+");

            // Board rows
            for (int row = 0; row < Rows; row++)
            {
                Console.Write(" |");
                for (int col = 0; col < Columns; col++)
                {
                    // Convert board value to symbol
                    char symbol = _board[row, col] switch
                    {
                        1 => 'X', // Player 1
                        2 => 'O', // Player 2
                        _ => ' ' // Empty
                    };
                    Console.Write(symbol);
                    if (col < Columns - 1) Console.Write(" "); // Space between columns
                }

                Console.WriteLine("|");
            }

            // Bottom border
            Console.WriteLine(" +" + new string('-', Columns * 2 - 1) + "+");

            // Cylinder mode indicator
            if (_config.IsCylinder)
            {
                Console.WriteLine("  (Edges wrap around)");
            }
        }

        // Drop piece in column (gravity effect)
        // Returns true if successful, false if column full
        public bool DropPiece(int column, int player)
        {
            // Validate column and check if full
            if (column < 0 || column >= Columns || _board[0, column] != 0)
            {
                Console.WriteLine("Invalid move! Column is full.");
                System.Threading.Thread.Sleep(1000);
                return false;
            }

            // Find lowest empty row (bottom to top)
            for (int row = Rows - 1; row >= 0; row--)
            {
                if (_board[row, column] == 0)
                {
                    _board[row, column] = player; // Place piece
                    return true;
                }
            }

            return false;
        }

        // Check if player has won (4+ in a row)
        public bool CheckWin(int player)
        {
            int winLength = _config.WinCondition;

            // Check all 4 directions from every cell
            // Horizontal (→)
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Columns; col++)
                    if (CheckLine(row, col, 0, 1, player, winLength))
                        return true;

            // Vertical (↓)
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Columns; col++)
                    if (CheckLine(row, col, 1, 0, player, winLength))
                        return true;

            // Diagonal (↘)
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Columns; col++)
                    if (CheckLine(row, col, 1, 1, player, winLength))
                        return true;

            // Diagonal (↙)
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Columns; col++)
                    if (CheckLine(row, col, -1, 1, player, winLength))
                        return true;

            return false;
        }

        // Check single line for win
        // dRow, dCol define direction: (0,1)=horizontal, (1,0)=vertical, etc.
        private bool CheckLine(int row, int col, int dRow, int dCol, int player, int winLength)
        {
            for (int i = 0; i < winLength; i++)
            {
                int newRow = row + i * dRow;
                int newCol = col + i * dCol;

                // Row bounds check
                if (newRow < 0 || newRow >= Rows)
                    return false;

                // Column bounds check (with cylinder wrapping)
                if (_config.IsCylinder)
                {
                    // Wrap around: modulo for negative and positive overflow
                    newCol = (newCol % Columns + Columns) % Columns;
                }
                else
                {
                    if (newCol < 0 || newCol >= Columns)
                        return false;
                }

                // Check if cell matches player
                if (_board[newRow, newCol] != player)
                    return false;
            }

            return true; // All cells in line match player
        }

        // Check if board is full (draw condition)
        public bool IsFull()
        {
            // Check top row - if any cell empty, board not full
            for (int col = 0; col < Columns; col++)
                if (_board[0, col] == 0)
                    return false;
            return true;
        }

        // Get copy of board state (for AI simulation)
        // Clone prevents AI from modifying actual board
        public int[,] GetBoardState()
        {
            return (int[,])_board.Clone();
        }

        // Load board state (for loading saved games)
        public void LoadBoardState(int[,] boardState)
        {
            // Validate dimensions match
            if (boardState.GetLength(0) != Rows || boardState.GetLength(1) != Columns)
            {
                throw new ArgumentException("Board state dimensions don't match configuration");
            }

            // Copy state to board
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    _board[row, col] = boardState[row, col];
                }
            }
        }
    }
}