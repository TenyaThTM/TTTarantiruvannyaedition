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
        private readonly object _lock = new object();
        private int _nextGameId = 1;
        private int _nextLobbyId = 1;

        public Lobby CreateLobby(int width, int height, int blockChance, int winLength, int captureProgress)
        {
            lock (_lock)
            {
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
                    CreatedAt = DateTime.Now
                };

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

        public bool JoinLobby(int lobbyId, string playerId, string color, string name)
        {
            lock (_lock)
            {
                var lobby = _lobbies.FirstOrDefault(l => l.Id == lobbyId);
                if (lobby == null || lobby.Status != LobbyStatus.WaitingForPlayers)
                    return false;

                // Проверяем, не занят ли цвет
                if (lobby.Players.Any(p => p.Color == color))
                    return false;

                // Проверяем, не присоединялся ли уже этот игрок
                if (lobby.Players.Any(p => p.Id == playerId))
                    return true; // Уже в лобби

                lobby.Players.Add(new Player
                {
                    Id = playerId,
                    Color = color,
                    Name = name ?? $"Игрок {color}"
                });

                return true;
            }
        }

        public bool StartGame(int lobbyId, string playerId)
{
    lock (_lock)
    {
        var lobby = _lobbies.FirstOrDefault(l => l.Id == lobbyId);
        if (lobby == null || lobby.Status != LobbyStatus.WaitingForPlayers)
            return false;

        // Проверяем, что игрок есть в лобби
        if (!lobby.Players.Any(p => p.Id == playerId))
            return false;

        // Минимально 2 игрока для начала игры
        if (lobby.Players.Count < 2)
            return false;

        // Создаем игру из лобби
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

        // Меняем статус лобби
        lobby.Status = LobbyStatus.GameStarted;
        lobby.GameId = game.Id;

        return true;
    }
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
        public GameState MakeMove(int gameId, int x, int y, string playerId)
        {
            lock (_lock)
            {
                var game = _games.FirstOrDefault(g => g.Id == gameId);
                if (game == null || game.GameOver || game.Players.Count == 0)
                    return null;

                var currentPlayer = game.Players[game.CurrentPlayerIndex];
                if (currentPlayer.Id != playerId)
                    return null;

                if (x < 0 || x >= game.Width || y < 0 || y >= game.Height)
                    return null;

                var cell = game.Board[y, x];
                if (cell.Blocked || (cell.Owner != null && cell.Owner != playerId))
                    return null;

                if (cell.Owner == playerId)
                    return game;

                cell.Progress++;

                if (cell.Progress >= game.CaptureProgress)
                {
                    cell.Owner = playerId;
                    cell.Progress = game.CaptureProgress;

                    if (CheckWinCondition(game, x, y, playerId))
                    {
                        game.GameOver = true;
                        game.Winner = playerId;
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
            int count = 0;
            for (int i = 0; i < game.Width; i++)
            {
                if (game.Board[y, i]?.Owner == playerId)
                {
                    count++;
                    if (count >= game.WinLength) return true;
                }
                else
                {
                    count = 0;
                }
            }

            // Проверка вертикали
            count = 0;
            for (int i = 0; i < game.Height; i++)
            {
                if (game.Board[i, x]?.Owner == playerId)
                {
                    count++;
                    if (count >= game.WinLength) return true;
                }
                else
                {
                    count = 0;
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
        public LobbyStatus Status { get; set; }
        public int? GameId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum LobbyStatus
    {
        WaitingForPlayers,
        GameStarted,
        Finished
    }
}