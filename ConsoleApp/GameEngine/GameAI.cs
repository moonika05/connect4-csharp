using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp.GameEngine
{
    // AI for Connect4 using Minimax algorithm with Alpha-Beta pruning
    // Difficulty: Easy (80% random + depth 1), Medium (depth 3), Hard (depth 5)
    public class GameAI
    {
        private readonly GameConfiguration _config;
        private readonly int _maxDepth;  // How many moves ahead AI thinks
        
        public GameAI(GameConfiguration config, int difficulty = 3)
        {
            _config = config;
            
            // Map difficulty to search depth (minimax depth)
            _maxDepth = difficulty switch
            {
                1 => 1,  // Easy: 1 move ahead + 80% random moves
                2 => 3,  // Medium: 3 moves ahead (simulates 3 future turns)
                3 => 5,  // Hard: 5 moves ahead (very slow but strong)
                _ => 3
            };
        }
        
        // Find best move for current player using Minimax
        // Returns column index (0-based)
        public int GetBestMove(int[,] board, int player)
        {
            Console.WriteLine($"\n🤖 AI is thinking (depth: {_maxDepth})...");
            
            // EASY MODE: 80% chance of random move (makes AI beatable)
            if (_maxDepth == 1)
            {
                var random = new Random();
                if (random.Next(100) < 80)  // 80% probability
                {
                    // Collect all valid moves
                    var validMoves = new List<int>();
                    for (int col = 0; col < _config.Columns; col++)
                    {
                        if (IsValidMove(board, col))
                            validMoves.Add(col);
                    }
                    
                    // Pick random move
                    if (validMoves.Any())
                    {
                        int randomMove = validMoves[random.Next(validMoves.Count)];
                        Console.WriteLine($"✓ AI (Easy - Random) chose column {randomMove + 1}");
                        return randomMove;
                    }
                }
            }
            
            // MINIMAX ALGORITHM: Try all moves, pick best
            int bestMove = -1;
            int bestScore = int.MinValue;
            
            // Try each column
            for (int col = 0; col < _config.Columns; col++)
            {
                if (IsValidMove(board, col))
                {
                    // Make move (simulate)
                    int row = GetLowestEmptyRow(board, col);
                    board[row, col] = player;
                    
                    // Evaluate move using minimax (opponent's turn next)
                    int score = Minimax(board, _maxDepth - 1, int.MinValue, int.MaxValue, false, player);
                    
                    // Undo move
                    board[row, col] = 0;
                    
                    Console.WriteLine($"Column {col + 1}: score {score}");
                    
                    // Track best move
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = col;
                    }
                }
            }
            
            Console.WriteLine($"✓ AI chose column {bestMove + 1} (score: {bestScore})");
            return bestMove;
        }
        
        // Minimax algorithm with Alpha-Beta pruning
        // Recursively evaluates board positions
        // isMaximizing: true = AI turn (maximize score), false = opponent turn (minimize score)
        private int Minimax(int[,] board, int depth, int alpha, int beta, bool isMaximizing, int player)
        {
            int opponent = player == 1 ? 2 : 1;
            
            // TERMINAL STATES (base cases)
            if (CheckWin(board, player))
                return 10000 + depth;  // Win! Prefer faster wins (higher depth = faster)
            if (CheckWin(board, opponent))
                return -10000 - depth;  // Loss! Avoid faster losses
            if (IsBoardFull(board) || depth == 0)
                return EvaluateBoard(board, player);  // Draw or depth limit reached
            
            if (isMaximizing)
            {
                // AI's turn - maximize score
                int maxScore = int.MinValue;
                
                for (int col = 0; col < _config.Columns; col++)
                {
                    if (IsValidMove(board, col))
                    {
                        // Make move
                        int row = GetLowestEmptyRow(board, col);
                        board[row, col] = player;
                        
                        // Recurse (opponent's turn)
                        int score = Minimax(board, depth - 1, alpha, beta, false, player);
                        
                        // Undo move
                        board[row, col] = 0;
                        
                        maxScore = Math.Max(maxScore, score);
                        alpha = Math.Max(alpha, score);
                        
                        // Alpha-Beta pruning: cut off bad branches
                        if (beta <= alpha)
                            break;  // This branch won't be chosen, skip rest
                    }
                }
                
                return maxScore;
            }
            else
            {
                // Opponent's turn - minimize score (bad for AI)
                int minScore = int.MaxValue;
                
                for (int col = 0; col < _config.Columns; col++)
                {
                    if (IsValidMove(board, col))
                    {
                        // Make move
                        int row = GetLowestEmptyRow(board, col);
                        board[row, col] = opponent;
                        
                        // Recurse (AI's turn)
                        int score = Minimax(board, depth - 1, alpha, beta, true, player);
                        
                        // Undo move
                        board[row, col] = 0;
                        
                        minScore = Math.Min(minScore, score);
                        beta = Math.Min(beta, score);
                        
                        // Alpha-Beta pruning
                        if (beta <= alpha)
                            break;
                    }
                }
                
                return minScore;
            }
        }
        
        // Evaluate board position (heuristic scoring)
        // Higher score = better for AI
        private int EvaluateBoard(int[,] board, int player)
        {
            int score = 0;
            int opponent = player == 1 ? 2 : 1;
            
            // Center column bonus (strategic position)
            int centerColumn = _config.Columns / 2;
            for (int row = 0; row < _config.Rows; row++)
            {
                if (board[row, centerColumn] == player)
                    score += 3;
            }
            
            // Evaluate all potential winning lines
            score += EvaluateLines(board, player);      // AI's opportunities
            score -= EvaluateLines(board, opponent);    // Opponent's threats
            
            return score;
        }
        
        // Evaluate all lines (horizontal, vertical, diagonal)
        private int EvaluateLines(int[,] board, int player)
        {
            int score = 0;
            int winLength = _config.WinCondition;
            
            // Horizontal lines (→)
            for (int row = 0; row < _config.Rows; row++)
            {
                for (int col = 0; col <= _config.Columns - winLength; col++)
                {
                    score += EvaluateWindow(board, row, col, 0, 1, player);
                }
            }
            
            // Vertical lines (↓)
            for (int row = 0; row <= _config.Rows - winLength; row++)
            {
                for (int col = 0; col < _config.Columns; col++)
                {
                    score += EvaluateWindow(board, row, col, 1, 0, player);
                }
            }
            
            // Diagonal (↘)
            for (int row = 0; row <= _config.Rows - winLength; row++)
            {
                for (int col = 0; col <= _config.Columns - winLength; col++)
                {
                    score += EvaluateWindow(board, row, col, 1, 1, player);
                }
            }
            
            // Diagonal (↙)
            for (int row = winLength - 1; row < _config.Rows; row++)
            {
                for (int col = 0; col <= _config.Columns - winLength; col++)
                {
                    score += EvaluateWindow(board, row, col, -1, 1, player);
                }
            }
            
            return score;
        }
        
        // Evaluate a single "window" (sequence of cells)
        // Example window: [X][X][X][_] = 3 player pieces + 1 empty = good!
        private int EvaluateWindow(int[,] board, int startRow, int startCol, int dRow, int dCol, int player)
        {
            int playerCount = 0;
            int emptyCount = 0;
            int winLength = _config.WinCondition;
            
            // Count pieces in window
            for (int i = 0; i < winLength; i++)
            {
                int row = startRow + i * dRow;
                int col = startCol + i * dCol;
                
                // Cylinder mode: wrap column
                if (_config.IsCylinder)
                {
                    col = (col % _config.Columns + _config.Columns) % _config.Columns;
                }
                
                if (board[row, col] == player)
                    playerCount++;
                else if (board[row, col] == 0)
                    emptyCount++;
                else
                    return 0;  // Opponent blocks this window
            }
            
            // Score based on how close to winning
            if (playerCount == winLength)
                return 1000;  // Shouldn't happen (checked in terminal states)
            else if (playerCount == winLength - 1 && emptyCount == 1)
                return 100;  // One move from win: XXX_
            else if (playerCount == winLength - 2 && emptyCount == 2)
                return 10;   // Two moves from win: XX__
            else if (playerCount == winLength - 3 && emptyCount == 3)
                return 1;    // Three moves from win: X___
            
            return 0;
        }
        
        // Check if player has won (4 in a row)
        private bool CheckWin(int[,] board, int player)
        {
            int winLength = _config.WinCondition;
            
            // Check all 4 directions
            for (int row = 0; row < _config.Rows; row++)
                for (int col = 0; col < _config.Columns; col++)
                {
                    if (CheckLine(board, row, col, 0, 1, player, winLength))   // Horizontal
                        return true;
                    if (CheckLine(board, row, col, 1, 0, player, winLength))   // Vertical
                        return true;
                    if (CheckLine(board, row, col, 1, 1, player, winLength))   // Diagonal ↘
                        return true;
                    if (CheckLine(board, row, col, -1, 1, player, winLength))  // Diagonal ↙
                        return true;
                }
            
            return false;
        }
        
        // Check single line for win
        // dRow, dCol = direction (0,1 = horizontal, 1,0 = vertical, etc)
        private bool CheckLine(int[,] board, int row, int col, int dRow, int dCol, int player, int winLength)
        {
            for (int i = 0; i < winLength; i++)
            {
                int newRow = row + i * dRow;
                int newCol = col + i * dCol;
                
                // Row bounds check
                if (newRow < 0 || newRow >= _config.Rows)
                    return false;
                
                // Column bounds check (with cylinder wrapping)
                if (_config.IsCylinder)
                {
                    newCol = (newCol % _config.Columns + _config.Columns) % _config.Columns;
                }
                else
                {
                    if (newCol < 0 || newCol >= _config.Columns)
                        return false;
                }
                
                // Check if player piece
                if (board[newRow, newCol] != player)
                    return false;
            }
            return true;  // All cells match player
        }
        
        // Check if move is valid (column not full)
        private bool IsValidMove(int[,] board, int col)
        {
            return col >= 0 && col < _config.Columns && board[0, col] == 0;
        }
        
        // Find lowest empty row in column (where piece will land)
        private int GetLowestEmptyRow(int[,] board, int col)
        {
            for (int row = _config.Rows - 1; row >= 0; row--)
            {
                if (board[row, col] == 0)
                    return row;
            }
            return -1;  // Column full
        }
        
        // Check if board is full (draw)
        private bool IsBoardFull(int[,] board)
        {
            for (int col = 0; col < _config.Columns; col++)
            {
                if (board[0, col] == 0)  // Top row has empty cell
                    return false;
            }
            return true;  // All columns full
        }
    }
}