using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using TicTacToeGame.Models;
using TicTacToeGame.Services;

namespace TicTacToeGame.Controllers
{
    public class GameController : Controller
    {
        private readonly GameService _gameService;
        private readonly PlayerService _playerService;

        public GameController(GameService gameService, PlayerService playerService)
        {
            _gameService = gameService;
            _playerService = playerService;
        }


        public IActionResult Index()
        {
            return View();
        }
[HttpPost]
public IActionResult UpdateGameSession([FromBody] UpdateGameSessionRequest request)
{
    try
    {
        if (request == null || !request.GameId.HasValue)
            return Json(new { success = false, error = "Invalid request" });

        HttpContext.Session.SetInt32("GameId", request.GameId.Value);
        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = ex.Message });
    }
}

public class UpdateGameSessionRequest
{
    public int? GameId { get; set; }
}
       [HttpPost]
public IActionResult CreateLobby([FromBody] GameParams parameters)
{
    try
    {
        if (parameters == null)
            return Json(new { success = false, error = "Invalid parameters" });

        var sessionId = HttpContext.Session.Id;
        
        // Убеждаемся что игрок существует
        var player = _playerService.GetPlayerBySession(sessionId);
        if (player == null)
        {
            // Создаем игрока автоматически
            player = _playerService.CreateOrUpdatePlayer(sessionId, "Игрок", null, "#FF5252");
        }

        var lobby = _gameService.CreateLobby(
            parameters.Width,
            parameters.Height,
            parameters.BlockChance,
            parameters.WinLength,
            parameters.CaptureProgress,
            sessionId
        );

        HttpContext.Session.SetInt32("LobbyId", lobby.Id);
        HttpContext.Session.SetString("IsLobbyCreator", "true");

        return Json(new { success = true, lobbyId = lobby.Id });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = ex.Message });
    }
}

        [HttpPost]
public IActionResult JoinLobby([FromBody] JoinLobbyRequest request)
{
    try
    {
        if (request == null)
            return Json(new { success = false, error = "Invalid request" });

        var sessionId = HttpContext.Session.Id;
        var success = _gameService.JoinLobby(request.LobbyId, sessionId);

        if (success)
        {
            HttpContext.Session.SetInt32("LobbyId", request.LobbyId);
            HttpContext.Session.SetString("IsLobbyCreator", "false");
            
            // Немедленно возвращаем состояние лобби
            var lobby = _gameService.GetLobby(request.LobbyId);
            return Json(new { 
                success = true,
                lobby = new {
                    id = lobby.Id,
                    players = lobby.Players,
                    status = lobby.Status
                }
            });
        }

        return Json(new { success = false, error = "Cannot join lobby" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = ex.Message });
    }
}
        [HttpGet]
        public IActionResult LobbyExists(int id)
        {
            try
            {
                var exists = _gameService.LobbyExists(id);
                return Json(new { exists });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
[HttpGet]
public IActionResult GetSessionId()
{
    try
    {
        var sessionId = HttpContext.Session.Id;
        return Json(new { success = true, sessionId });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = ex.Message });
    }
}
        [HttpPost]
        public IActionResult StartGame([FromBody] StartGameRequest request)
        {
            try
            {
                var lobbyId = HttpContext.Session.GetInt32("LobbyId");
                var playerId = HttpContext.Session.Id;

                if (!lobbyId.HasValue)
                    return Json(new { success = false, error = "No lobby found" });

                var success = _gameService.StartGame(lobbyId.Value, playerId);

                if (success)
                {
                    var lobby = _gameService.GetLobby(lobbyId.Value);
                    if (lobby != null && lobby.GameId.HasValue)
                    {
                        HttpContext.Session.SetInt32("GameId", lobby.GameId.Value);
                        return Json(new { success = true, gameId = lobby.GameId });
                    }
                }

                return Json(new { success = false, error = "Cannot start game. Need at least 2 players." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult MakeMove([FromBody] MoveRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, error = "Invalid move" });

                var gameId = HttpContext.Session.GetInt32("GameId");
                var playerId = HttpContext.Session.Id;

                if (!gameId.HasValue)
                    return Json(new { success = false, error = "No game found" });

                var game = _gameService.MakeMove(gameId.Value, request.X, request.Y, playerId);

                if (game == null)
                    return Json(new { success = false, error = "Invalid move" });

                return Json(new
                {
                    success = true,
                    gameOver = game.GameOver,
                    winner = game.Winner,
                    currentPlayer = game.Players[game.CurrentPlayerIndex]?.Color
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpPost]
public IActionResult LeaveLobby()
{
    try
    {
        HttpContext.Session.Remove("LobbyId");
        HttpContext.Session.Remove("GameId");
        HttpContext.Session.Remove("PlayerColor");
        HttpContext.Session.Remove("PlayerName");
        HttpContext.Session.Remove("IsLobbyCreator");
        
        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = ex.Message });
    }
}

[HttpPost]
public IActionResult ResetSession()
{
    try
    {
        HttpContext.Session.Clear();
        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = ex.Message });
    }
}
        [HttpGet]
        public IActionResult GameState()
        {
            try
            {
                var gameId = HttpContext.Session.GetInt32("GameId");
                if (!gameId.HasValue)
                    return Json(new { error = "No game found" });

                var game = _gameService.GetGame(gameId.Value);
                if (game == null)
                {
                    HttpContext.Session.Remove("GameId");
                    return Json(new { error = "Game not found" });
                }

                var sessionId = HttpContext.Session.Id;
                Console.WriteLine($"Checking player session {sessionId} in game {gameId}");
                var currentPlayer = game.Players.FirstOrDefault(p => p.SessionId == sessionId);
                if (currentPlayer == null)
                {
                    Console.WriteLine($"Player session {sessionId} not found in game {gameId}");

                    // Пытаемся найти или создать игрока
                    var player = _playerService.GetPlayerBySession(sessionId);
                    if (player == null)
                    {
                        player = _playerService.CreateOrUpdatePlayer(sessionId, "Игрок", null, GetRandomColor());
                    }

                    // Проверяем по Id игрока
                    var existingPlayer = game.Players.FirstOrDefault(p => p.Id == player.Id);
                    if (existingPlayer != null)
                    {
                        // Обновляем sessionId у существующего игрока
                        existingPlayer.SessionId = sessionId;
                        Console.WriteLine($"Updated player session: {player.Id} -> {sessionId}");
                    }
                    else
                    {
                        // Добавляем игрока в игру
                        game.Players.Add(new Player
                        {
                            Id = player.Id,
                            SessionId = player.SessionId,
                            Name = player.Name,
                            AvatarPath = player.AvatarPath,
                            Color = player.Color
                        });
                        Console.WriteLine($"Added player to game: {player.Id}");
                    }
                }
                // Проверяем по SessionId
                if (!game.Players.Any(p => p.SessionId == sessionId))
                {
                    Console.WriteLine($"Player session {sessionId} not found in game {gameId}");

                    // Пытаемся найти игрока через сервис
                    var player = _playerService.GetPlayerBySession(sessionId);
                    if (player == null)
                    {
                        HttpContext.Session.Remove("GameId");
                        return Json(new { error = "Player not found" });
                    }

                    // Проверяем по Id игрока
                    if (!game.Players.Any(p => p.Id == player.Id))
                    {
                        HttpContext.Session.Remove("GameId");
                        return Json(new { error = "Player not in this game" });
                    }

                    Console.WriteLine($"Player found by ID: {player.Id}");
                }

                // Преобразуем двумерный массив в список
                var cells = new List<object>();
                for (int y = 0; y < game.Height; y++)
                {
                    for (int x = 0; x < game.Width; x++)
                    {
                        var cell = game.Board[y, x];
                        cells.Add(new
                        {
                            x = cell.X,
                            y = cell.Y,
                            blocked = cell.Blocked,
                            owner = cell.Owner,
                            progress = cell.Progress
                        });
                    }
                }

                return Json(new
                {
                    width = game.Width,
                    height = game.Height,
                    captureProgress = game.CaptureProgress,
                    players = game.Players,
                    currentPlayerIndex = game.CurrentPlayerIndex,
                    currentPlayer = game.Players.Count > 0 ? game.Players[game.CurrentPlayerIndex]?.Color : null,
                    gameOver = game.GameOver,
                    winner = game.Winner,
                    cells = cells
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GameState error: {ex}");
                return Json(new { error = ex.Message });
            }
        }
        [HttpGet]
public IActionResult GetMySessionId()
{
    try
    {
        return Json(new { success = true, sessionId = HttpContext.Session.Id });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = ex.Message });
    }
}
private string GetRandomColor()
        {
            var colors = new[] { "#FF5252", "#FFEB3B", "#4CAF50", "#2196F3", "#9C27B0", "#FF9800" };
            return colors[new Random().Next(colors.Length)];
        }
[HttpGet]
public IActionResult CheckGameStarted()
{
    try
    {
        var lobbyId = HttpContext.Session.GetInt32("LobbyId");
        if (!lobbyId.HasValue)
            return Json(new { gameStarted = false, error = "No lobby found" });

        var lobby = _gameService.GetLobby(lobbyId.Value);
        if (lobby == null)
            return Json(new { gameStarted = false, error = "Lobby not found" });

        // Проверяем, началась ли игра
        if (lobby.Status == LobbyStatus.GameStarted && lobby.GameId.HasValue)
        {
            HttpContext.Session.SetInt32("GameId", lobby.GameId.Value);
            return Json(new { 
                gameStarted = true, 
                gameId = lobby.GameId.Value 
            });
        }

        return Json(new { gameStarted = false });
    }
    catch (Exception ex)
    {
        return Json(new { gameStarted = false, error = ex.Message });
    }
}
[HttpGet]
public IActionResult LobbyState()
{
    try
    {
        var lobbyId = HttpContext.Session.GetInt32("LobbyId");
        if (!lobbyId.HasValue)
            return Json(new { error = "No lobby found in session" });

        var lobby = _gameService.GetLobby(lobbyId.Value);
        if (lobby == null)
            return Json(new { error = "Lobby not found" });

        var sessionId = HttpContext.Session.Id;
        
        if (!lobby.Players.Any(p => p.SessionId == sessionId))
        {
            var joined = _gameService.JoinLobby(lobbyId.Value, sessionId);
            if (!joined) return Json(new { error = "Player not in this lobby" });
            lobby = _gameService.GetLobby(lobbyId.Value);
        }

        // Преобразуем enum в строку для JavaScript
        string statusString = lobby.Status.ToString();

        return Json(new
        {
            id = lobby.Id,
            width = lobby.Width,
            height = lobby.Height,
            players = lobby.Players,
            status = statusString, // Используем строковое представление
            gameId = lobby.GameId
        });
    }
    catch (Exception ex)
    {
        return Json(new { error = ex.Message });
    }
}
        [HttpGet]
        public IActionResult ListLobbies()
        {
            try
            {
                var lobbies = _gameService.GetAllLobbies();
                return Json(lobbies.Select(l => new
                {
                    id = l.Id,
                    width = l.Width,
                    height = l.Height,
                    blockChance = l.BlockChance,
                    winLength = l.WinLength,
                    captureProgress = l.CaptureProgress,
                    playerCount = l.Players.Count,
                    maxPlayers = 6,
                    createdAt = l.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }

    public class GameParams
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BlockChance { get; set; }
        public int WinLength { get; set; }
        public int CaptureProgress { get; set; }
    }

    public class JoinLobbyRequest
    {
        public int LobbyId { get; set; }
        public string Color { get; set; }
        public string Name { get; set; }
    }

    public class MoveRequest
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
    public class StartGameRequest
{
    public int LobbyId { get; set; }
}
}