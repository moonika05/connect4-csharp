# Connect Four (C#)

Implementation of the classic Connect Four board game written in C#.

The project includes both a console version and a web version built with ASP.NET Core. The game supports AI opponents, multiplayer play, configurable rules, and persistent game state.

---

## Features

- Console app and web app (ASP.NET Core)
- AI opponent with 3 difficulty levels (Minimax algorithm)
- Save/load games (JSON or SQLite)
- Custom board sizes and rule configurations
- Cylinder mode (wrap-around board edges)

---

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

---

## Technologies

- C# / .NET 9
- ASP.NET Core (Razor Pages)
- Entity Framework Core (SQLite)
- System.Text.Json

---

## Multiplayer Support

### How to play with another person

**Share Link (Real-time)**

1. Start a new game (Human vs Human)
2. Copy the shareable link displayed on the game page
3. Send the link to your friend
4. Both players open the same link
5. Take turns making moves
6. Click **"🔄 Refresh Board"** to see the opponent's moves
