# Connect4 Game

C# implementation of Connect4 game with AI opponent.

## Features
- Console app and web app (ASP.NET Core)
- AI with 3 difficulty levels (Minimax algorithm)
- Save/load games (JSON or SQLite)
- Custom board sizes and rules
- Cylinder mode (wrap-around edges)

## How to Run

### Console App
```bash
cd ConsoleApp
dotnet run
```

### Web App
```bash
cd WebApp
dotnet run
```

## Technologies
- C# / .NET 9
- ASP.NET Core (Razor Pages)
- Entity Framework Core (SQLite)
- System.Text.Json

## Multiplayer Support

### How to play with another person:

**Share Link (Real-time)**
1. Start a new game (Human vs Human)
2. Copy the shareable link at the top of the game page
3. Send link to friend
4. Both players open the same link
5. Take turns making moves
6. Click "🔄 Refresh Board" to see opponent's moves
