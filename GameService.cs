using TicTacToeGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame.Services
{
    public class GameService
    {
        private readonly List<GameState> _games = new List<GameState>();
        private readonly object _lock = new object();
        private int _nextGameId = 1;

        public GameState CreateNewGame(int width, int height, int blockChance, int winLength, int captureProgress)
        {
            lock (_lock)
            {
                var game = new GameState
                {
                    Id = _nextGameId++,
                    Width = width,
                    Height = height,
                    BlockChance = blockChance,
                    WinLength = winLength,
                    CaptureProgress = captureProgress,
                    Players = new List<Player>(),
                    CurrentPlayerIndex = 0,
                    GameOver = false,
                    CreatedAt = DateTime.Now
                };

                InitializeBoard(game);
                _games.Add(game);
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

        public GameState GetGame(int gameId)
        {
            lock (_lock)
            {
                return _games.FirstOrDefault(g => g.Id == gameId);
            }
        }

        public bool AddPlayerToGame(int gameId, string playerId, string color, string name)
        {
            lock (_lock)
            {
                var game = _games.FirstOrDefault(g => g.Id == gameId);
                if (game == null || game.Players.Any(p => p.Color == color))
                    return false;

                game.Players.Add(new Player { Id = playerId, Color = color, Name = name });
                return true;
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
                    return game; // Уже захвачена этим игроком

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

                // Переход хода
                game.CurrentPlayerIndex = (game.CurrentPlayerIndex + 1) % game.Players.Count;

                return game;
            }
        }

        private bool CheckWinCondition(GameState game, int x, int y, string playerId)
        {
            // Проверка горизонтали
            int count = 0;
            for (int i = 0; i < game.Width; i++)
            {
                if (game.Board[y, i].Owner == playerId)
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
                if (game.Board[i, x].Owner == playerId)
                {
                    count++;
                    if (count >= game.WinLength) return true;
                }
                else
                {
                    count = 0;
                }
            }

            // Проверка диагонали (слева направо)
            count = 0;
            int minXY = Math.Min(x, y);
            int startX = x - minXY;
            int startY = y - minXY;
            
            for (int i = 0; i < Math.Min(game.Width - startX, game.Height - startY); i++)
            {
                if (game.Board[startY + i, startX + i].Owner == playerId)
                {
                    count++;
                    if (count >= game.WinLength) return true;
                }
                else
                {
                    count = 0;
                }
            }

            // Проверка диагонали (справа налево)
            count = 0;
            int minXGameHeightY = Math.Min(x, game.Height - 1 - y);
            startX = x - minXGameHeightY;
            startY = y + minXGameHeightY;
            
            for (int i = 0; i < Math.Min(game.Width - startX, startY + 1); i++)
            {
                if (game.Board[startY - i, startX + i].Owner == playerId)
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

        public List<GameState> GetAllGames()
        {
            lock (_lock)
            {
                return _games.ToList();
            }
        }

        public void RemoveGame(int gameId)
        {
            lock (_lock)
            {
                var game = _games.FirstOrDefault(g => g.Id == gameId);
                if (game != null)
                {
                    _games.Remove(game);
                }
            }
        }
    }
}