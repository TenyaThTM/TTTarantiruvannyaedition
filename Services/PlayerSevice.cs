using TicTacToeGame.Models;
using System.Collections.Concurrent;

namespace TicTacToeGame.Services
{
    public class PlayerService
    {
        private readonly ConcurrentDictionary<string, Player> _players = new();
        private readonly ConcurrentDictionary<string, PlayerSession> _sessions = new();
        private readonly IWebHostEnvironment _environment;
        private readonly object _lock = new();

        public PlayerService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public Player? GetPlayer(string playerId)
        {
            return _players.TryGetValue(playerId, out var player) ? player : null;
        }

        public Player? GetPlayerBySession(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var session) &&
                _players.TryGetValue(session.PlayerId, out var player))
            {
                player.LastSeen = DateTime.Now;
                return player;
            }
            return null;
        }

        public Player CreateOrUpdatePlayer(string sessionId, string name = null, IFormFile avatarFile = null, string color = null)
{
    lock (_lock)
    {
        var session = _sessions.GetValueOrDefault(sessionId);
        Player player;

        if (session != null && _players.TryGetValue(session.PlayerId, out player))
        {
            // Обновляем существующего игрока
            if (!string.IsNullOrEmpty(name)) player.Name = name;
            if (!string.IsNullOrEmpty(color)) player.Color = color;
            player.LastSeen = DateTime.Now;
        }
        else
        {
            // Создаем нового игрока
            player = new Player 
            { 
                Name = name ?? "Игрок", 
                SessionId = sessionId,
                Color = color ?? GetRandomColor()
            };
            _players[player.Id] = player;
            
            // Создаем сессию
            _sessions[sessionId] = new PlayerSession
            {
                SessionId = sessionId,
                PlayerId = player.Id,
                ExpiresAt = DateTime.Now.AddDays(7)
            };
        }

        // Обрабатываем аватар
        if (avatarFile != null && avatarFile.Length > 0)
        {
            player.AvatarPath = SaveAvatar(avatarFile, player.Id);
        }

        return player;
    }
}

        private string GetRandomColor()
        {
            var colors = new[] { "#FF5252", "#FFEB3B", "#4CAF50", "#2196F3", "#9C27B0", "#FF9800" };
            return colors[new Random().Next(colors.Length)];
        }

        private string? SaveAvatar(IFormFile avatarFile, string playerId)
        {
            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "avatars");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var extension = Path.GetExtension(avatarFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return null;

                var fileName = $"{playerId}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    avatarFile.CopyTo(stream);
                }

                return $"/avatars/{fileName}";
            }
            catch
            {
                return null;
            }
        }

        public void CleanupOldPlayers()
        {
            var cutoff = DateTime.Now.AddDays(-30);
            var oldPlayers = _players.Where(p => p.Value.LastSeen < cutoff).ToList();
            
            foreach (var player in oldPlayers)
            {
                _players.TryRemove(player.Key, out _);
                
                // Удаляем файл аватара
                if (!string.IsNullOrEmpty(player.Value.AvatarPath))
                {
                    var avatarPath = Path.Combine(_environment.WebRootPath, player.Value.AvatarPath.TrimStart('/'));
                    if (File.Exists(avatarPath))
                        File.Delete(avatarPath);
                }
            }
        }
    }
}