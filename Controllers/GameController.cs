using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using TicTacToeGame.Models;
using TicTacToeGame.Services;

namespace TicTacToeGame.Controllers
{
    public class GameController : Controller
    {
        private readonly GameService _gameService;

        public GameController(GameService gameService)
        {
            _gameService = gameService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateLobby([FromBody] GameParams parameters)
        {
            try
            {
                if (parameters == null)
                    return Json(new { success = false, error = "Invalid parameters" });

                var lobby = _gameService.CreateLobby(
                    parameters.Width,
                    parameters.Height,
                    parameters.BlockChance,
                    parameters.WinLength,
                    parameters.CaptureProgress
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

                var playerId = HttpContext.Session.Id;
                var success = _gameService.JoinLobby(request.LobbyId, playerId, request.Color, request.Name);

                if (success)
                {
                    HttpContext.Session.SetInt32("LobbyId", request.LobbyId);
                    HttpContext.Session.SetString("PlayerColor", request.Color);
                    HttpContext.Session.SetString("PlayerName", request.Name);
                    HttpContext.Session.SetString("IsLobbyCreator", "false");

                    return Json(new { success = true });
                }

                return Json(new { success = false, error = "Cannot join lobby - color may be taken or lobby full" });
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

        var playerId = HttpContext.Session.Id;
        if (!game.Players.Any(p => p.Id == playerId))
        {
            HttpContext.Session.Remove("GameId");
            return Json(new { error = "Player not in this game" });
        }

        // ПРЕОБРАЗУЕМ ДВУМЕРНЫЙ МАССИВ В СПИСОК ДЛЯ СЕРИАЛИЗАЦИИ
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

        // ВОЗВРАЩАЕМ РЕАЛЬНЫЕ ДАННЫЕ ИГРЫ
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
        return Json(new { error = ex.Message });
    }
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
        Console.WriteLine($"LobbyState called. Session LobbyId: {lobbyId}");
        
        if (!lobbyId.HasValue)
        {
            Console.WriteLine("No lobbyId in session");
            return Json(new { error = "No lobby found" });
        }

        var lobby = _gameService.GetLobby(lobbyId.Value);
        if (lobby == null)
        {
            Console.WriteLine($"Lobby {lobbyId} not found in service");
            HttpContext.Session.Remove("LobbyId");
            HttpContext.Session.Remove("IsLobbyCreator");
            return Json(new { error = "Lobby not found" });
        }

        var playerId = HttpContext.Session.Id;
        Console.WriteLine($"Checking player {playerId} in lobby {lobbyId}");
        
        if (!lobby.Players.Any(p => p.Id == playerId))
        {
            Console.WriteLine($"Player {playerId} not found in lobby {lobbyId}");
            HttpContext.Session.Remove("LobbyId");
            HttpContext.Session.Remove("IsLobbyCreator");
            return Json(new { error = "Player not in this lobby" });
        }

        Console.WriteLine($"Returning lobby data: {lobby.Players.Count} players, status: {lobby.Status}");
        return Json(new
        {
            id = lobby.Id,
            width = lobby.Width,
            height = lobby.Height,
            players = lobby.Players,
            status = lobby.Status,
            gameId = lobby.GameId
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"LobbyState error: {ex}");
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