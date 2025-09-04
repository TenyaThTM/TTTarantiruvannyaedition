using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using TicTacToeGame.Models;
using TicTacToeGame;
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
        public IActionResult CreateGame([FromBody] GameParams parameters)
        {
            if (parameters == null)
            {
                return Json(new { success = false, error = "Invalid parameters" });
            }

            var game = _gameService.CreateNewGame(
                parameters.Width, 
                parameters.Height, 
                parameters.BlockChance, 
                parameters.WinLength, 
                parameters.CaptureProgress
            );
            
            HttpContext.Session.SetInt32("GameId", game.Id);
            
            return Json(new { success = true, gameId = game.Id });
        }

        [HttpPost]
        public IActionResult JoinGame([FromBody] JoinGameRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, error = "Invalid request" });
            }

            var playerId = HttpContext.Session.Id;
            
            // Автоматическое имя если не указано
            var playerName = string.IsNullOrEmpty(request.Name) ? $"Игрок {request.Color}" : request.Name;
            
            var success = _gameService.AddPlayerToGame(request.GameId, playerId, request.Color, playerName);
            
            if (success)
            {
                HttpContext.Session.SetInt32("GameId", request.GameId);
                HttpContext.Session.SetString("PlayerColor", request.Color);
                HttpContext.Session.SetString("PlayerName", playerName);
                return Json(new { success = true });
            }
            
            return Json(new { success = false, error = "Cannot join game" });
        }

        [HttpPost]
        public IActionResult MakeMove([FromBody] MoveRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, error = "Invalid move" });
            }

            var gameId = HttpContext.Session.GetInt32("GameId");
            var playerId = HttpContext.Session.Id;
            
            if (!gameId.HasValue)
                return Json(new { success = false, error = "No game found" });
            
            var game = _gameService.MakeMove(gameId.Value, request.X, request.Y, playerId);
            
            if (game == null)
                return Json(new { success = false, error = "Invalid move" });
            
            return Json(new { 
                success = true, 
                gameOver = game.GameOver,
                winner = game.Winner,
                currentPlayer = game.Players[game.CurrentPlayerIndex]?.Color
            });
        }

        [HttpGet]
        public IActionResult GameState()
        {
            var gameId = HttpContext.Session.GetInt32("GameId");
            if (!gameId.HasValue)
                return Json(new { error = "No game found" });
            
            var game = _gameService.GetGame(gameId.Value);
            if (game == null)
                return Json(new { error = "Game not found" });
            
            // Преобразуем двумерный массив в список для сериализации
            var cells = new List<object>();
            for (int y = 0; y < game.Height; y++)
            {
                for (int x = 0; x < game.Width; x++)
                {
                    var cell = game.Board[y, x];
                    cells.Add(new {
                        x = cell.X,
                        y = cell.Y,
                        blocked = cell.Blocked,
                        owner = cell.Owner,
                        progress = cell.Progress
                    });
                }
            }
            
            return Json(new {
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

        [HttpGet]
        public IActionResult ListGames()
        {
            var games = _gameService.GetAllGames();
            return Json(games.Select(g => new {
                id = g.Id,
                width = g.Width,
                height = g.Height,
                players = g.Players.Count,
                gameOver = g.GameOver
            }));
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

    public class JoinGameRequest
    {
        public int GameId { get; set; }
        public string Color { get; set; }
        public string Name { get; set; }
    }

    public class MoveRequest
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}