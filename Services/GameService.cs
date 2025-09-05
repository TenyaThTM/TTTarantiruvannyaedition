using TicTacToeGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame.Services
{
    public class GameService
    {
        private readonly List<GameState> _games = new List<GameState>();
        private readonly List<Lobby> _lobbies = new List<Lobby>();
        private readonly PlayerService _playerService;
        private readonly object _lock = new object();
        private int _nextGameId = 1;
        private int _nextLobbyId = 1;

        public GameService(PlayerService playerService)
        {
            _playerService = playerService;
        }

        public Lobby CreateLobby(int width, int height, int blockChance, int winLength, int captureProgress, string creatorSessionId)
{
    lock (_lock)
    {
        var creator = _playerService.GetPlayerBySession(creatorSessionId);
        if (creator == null)
        {
            // СОЗДАЕМ ИГРОКА ЧЕРЕЗ PLAYER SERVICE, А НЕ ВРУЧНУЮ
            creator = _playerService.CreateOrUpdatePlayer(creatorSessionId, "Игрок", null, "#FF5252");
        }

        var lobby = new Lobby
        {
            Id = _nextLobbyId++,
            Width = width,
            Height = height,
            BlockChance = blockChance,
            WinLength = winLength,
            CaptureProgress = captureProgress,
            Players = new List<Player>(),
            Status = LobbyStatus.WaitingForPlayers,
            CreatedAt = DateTime.Now,
        };

        // ДОБАВЛЯЕМ СУЩЕСТВУЮЩЕГО ИГРОКА, А НЕ СОЗДАЕМ НОВОГО
        lobby.Players.Add(creator);

        _lobbies.Add(lobby);
        return lobby;
    }
}

       public GameState GetGame(int gameId)
{
    lock (_lock)
    {
        var game = _games.FirstOrDefault(g => g.Id == gameId);
        if (game == null)
        {
            Console.WriteLine($"Game {gameId} not found");
            return null;
        }

        // Проверяем, не завершена ли игра
        if (game.GameOver)
        {
            Console.WriteLine($"Game {gameId} is already over");
            return game;
        }

        // Очищаем устаревшие сессии
        game.Players.RemoveAll(p => 
            _playerService.GetPlayerBySession(p.SessionId) == null);

        return game;
    }
}

public Lobby GetLobby(int lobbyId)
{
    lock (_lock)
    {
        var lobby = _lobbies.FirstOrDefault(l => l.Id == lobbyId);
        if (lobby == null)
        {
            Console.WriteLine($"Lobby {lobbyId} not found");
            return null;
        }

        // Проверяем, не устарело ли лобби
        if (lobby.Status == LobbyStatus.Finished || 
            (lobby.Status == LobbyStatus.GameStarted && lobby.CreatedAt < DateTime.Now.AddMinutes(-30)))
        {
            Console.WriteLine($"Lobby {lobbyId} is expired");
            _lobbies.Remove(lobby);
            return null;
        }

        return lobby;
    }
}
public bool LobbyExists(int lobbyId)
{
    lock (_lock)
    {
        return _lobbies.Any(l => l.Id == lobbyId && l.Status == LobbyStatus.WaitingForPlayers);
    }
}
        public bool JoinLobby(int lobbyId, string sessionId)
{
    lock (_lock)
    {
        var lobby = _lobbies.FirstOrDefault(l => l.Id == lobbyId);
        if (lobby == null || lobby.Status != LobbyStatus.WaitingForPlayers)
            return false;

        // ПОЛУЧАЕМ ИГРОКА ЧЕРЕЗ PLAYER SERVICE (сохраняет данные)
        var player = _playerService.GetPlayerBySession(sessionId);
        if (player == null)
        {
            player = _playerService.CreateOrUpdatePlayer(sessionId, "Игрок", null, GetNextAvailableColor(lobby));
        }

        if (lobby.Players.Any(p => p.SessionId == sessionId || p.Id == player.Id))
            return true;

        // ИСПОЛЬЗУЕМ СУЩЕСТВУЮЩЕГО ИГРОКА, А НЕ СОЗДАЕМ КЛОНА
        lobby.Players.Add(player);
        return true;
    }
}


        public bool StartGame(int lobbyId, string sessionId)
{
    lock (_lock)
    {
        var lobby = _lobbies.FirstOrDefault(l => l.Id == lobbyId);
        if (lobby == null || lobby.Status != LobbyStatus.WaitingForPlayers)
            return false;

        var player = _playerService.GetPlayerBySession(sessionId);
        if (player == null || !lobby.Players.Any(p => p.Id == player.Id))
            return false;

        // УБИРАЕМ проверку количества игроков здесь - она будет в другом месте
        // if (lobby.Players.Count < 2)
        //     return false;

        var game = new GameState
        {
            Id = _nextGameId++,
            Width = lobby.Width,           
            Height = lobby.Height,        
            BlockChance = lobby.BlockChance,
            WinLength = lobby.WinLength,
            CaptureProgress = lobby.CaptureProgress,
            Players = new List<Player>(lobby.Players),
            CurrentPlayerIndex = 0,
            GameOver = false,
            CreatedAt = DateTime.Now
        };

        InitializeBoard(game);
        _games.Add(game);

        lobby.Status = LobbyStatus.GameStarted;
        lobby.GameId = game.Id;

        return true;
    }
}
private string GetNextAvailableColor(Lobby lobby)
{
    var usedColors = lobby.Players
        .Where(p => !string.IsNullOrEmpty(p.Color) && p.Color != "null")
        .Select(p => p.Color)
        .ToHashSet();
    
    var availableColors = new[] { "#FF5252", "#FFEB3B", "#4CAF50", "#2196F3", "#9C27B0", "#FF9800" };
    
    var nextColor = availableColors.FirstOrDefault(color => !usedColors.Contains(color)) ?? availableColors[0];
    Console.WriteLine($"Assigned color: {nextColor}, Used colors: {string.Join(", ", usedColors)}");
    return nextColor;
}
public void CleanupOldSessions()
{
    lock (_lock)
    {
        // Удаляем старые завершенные игры (старше 1 часа)
        _games.RemoveAll(g => g.GameOver && g.CreatedAt < DateTime.Now.AddHours(-1));
        
        // Удаляем старые лобби (старше 2 часов)
        _lobbies.RemoveAll(l => l.CreatedAt < DateTime.Now.AddHours(-2));
        
        // Удаляем лобби без игроков (старше 30 минут)
        _lobbies.RemoveAll(l => l.Players.Count == 0 && l.CreatedAt < DateTime.Now.AddMinutes(-30));
    }
}
        public GameState MakeMove(int gameId, int x, int y, string sessionId) // Меняем playerId на sessionId
{
    lock (_lock)
    {
        var game = _games.FirstOrDefault(g => g.Id == gameId);
        if (game == null || game.GameOver || game.Players.Count == 0)
            return null;

        var currentPlayer = game.Players[game.CurrentPlayerIndex];
        
        // ПРАВИЛЬНО: сравниваем sessionId с currentPlayer.SessionId
        if (currentPlayer.SessionId != sessionId) // ← ИСПРАВЛЕНИЕ
            return null;

        if (x < 0 || x >= game.Width || y < 0 || y >= game.Height)
            return null;

        var cell = game.Board[y, x];
        if (cell.Blocked || (cell.Owner != null && cell.Owner != currentPlayer.Id)) // Исправляем сравнение
            return null;

        if (cell.Owner == currentPlayer.Id) // Исправляем сравнение
            return game;

        cell.Progress++;

        if (cell.Progress >= game.CaptureProgress)
        {
            cell.Owner = currentPlayer.Id; // Сохраняем ID игрока, а не sessionId
            cell.Progress = game.CaptureProgress;

            if (CheckWinCondition(game, x, y, currentPlayer.Id)) // Передаем ID игрока
            {
                game.GameOver = true;
                game.Winner = currentPlayer.Id; // Сохраняем ID победителя
            }
        }

        game.CurrentPlayerIndex = (game.CurrentPlayerIndex + 1) % game.Players.Count;
        return game;
    }
}

        private void InitializeBoard(GameState game)
        {
            game.Board = new Cell[game.Height, game.Width];
            var random = new Random();

            for (int y = 0; y < game.Height; y++)
            {
                for (int x = 0; x < game.Width; x++)
                {
                    game.Board[y, x] = new Cell
                    {
                        X = x,
                        Y = y,
                        Blocked = random.Next(100) < game.BlockChance,
                        Owner = null,
                        Progress = 0
                    };
                }
            }
        }

        private bool CheckWinCondition(GameState game, int x, int y, string playerId)
{
    // Проверка горизонтали
    if (CheckLine(game, y, 0, 0, 1, playerId)) return true;
    
    // Проверка вертикали  
    if (CheckLine(game, 0, x, 1, 0, playerId)) return true;
    
    // Проверка диагонали ↗
    if (CheckDiagonal(game, x, y, 1, 1, playerId)) return true;
    
    // Проверка диагонали ↖
    if (CheckDiagonal(game, x, y, -1, 1, playerId)) return true;
    
    return false;
}
        private bool CheckLine(GameState game, int startY, int startX, int dy, int dx, string playerId)
{
    int count = 0;
    int x = startX;
    int y = startY;
    
    while (x >= 0 && x < game.Width && y >= 0 && y < game.Height)
    {
        if (game.Board[y, x]?.Owner == playerId)
        {
            count++;
            if (count >= game.WinLength) return true;
        }
        else
        {
            count = 0;
        }
        x += dx;
        y += dy;
    }
    return false;
}

private bool CheckDiagonal(GameState game, int centerX, int centerY, int dirX, int dirY, string playerId)
{
    int count = 0;
    
    // Проверяем в обе стороны от центральной точки
    for (int i = -game.WinLength + 1; i < game.WinLength; i++)
    {
        int x = centerX + i * dirX;
        int y = centerY + i * dirY;
        
        if (x >= 0 && x < game.Width && y >= 0 && y < game.Height)
        {
            if (game.Board[y, x]?.Owner == playerId)
            {
                count++;
                if (count >= game.WinLength) return true;
            }
            else
            {
                count = 0;
            }
        }
    }
    return false;
}

        public List<Lobby> GetAllLobbies()
        {
            lock (_lock)
            {
                return _lobbies
                    .Where(l => l.Status == LobbyStatus.WaitingForPlayers)
                    .ToList();
            }
        }

        public void RemoveLobby(int lobbyId)
        {
            lock (_lock)
            {
                _lobbies.RemoveAll(l => l.Id == lobbyId);
            }
        }
    }

    public class Lobby
    {
        public int Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int BlockChance { get; set; }
        public int WinLength { get; set; }
        public int CaptureProgress { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public LobbyStatus Status { get; set; } = LobbyStatus.WaitingForPlayers;
        public int? GameId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum LobbyStatus
    {
        WaitingForPlayers,
        GameStarted,
        Finished
    }
}