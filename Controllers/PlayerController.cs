using Microsoft.AspNetCore.Mvc;
using TicTacToeGame.Models;
using TicTacToeGame.Services;

namespace TicTacToeGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : Controller
    {
        private readonly PlayerService _playerService;

        public PlayerController(PlayerService playerService)
        {
            _playerService = playerService;
        }
        [HttpPost("quickCreate")]
public IActionResult QuickCreatePlayer([FromBody] QuickCreateRequest request)
{
    try
    {
        var sessionId = HttpContext.Session.Id;
        var player = _playerService.CreateOrUpdatePlayer(
            sessionId, 
            request?.Name, 
            null, 
            request?.Color
        );
        
        return Json(new { 
            success = true, 
            player = new {
                player.Id,
                player.Name,
                player.Color,
                player.AvatarPath
            }
        });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = ex.Message });
    }
}

public class QuickCreateRequest
{
    public string Name { get; set; } = "Игрок";
    public string Color { get; set; } = "#FF5252";
}
        [HttpGet]
        public IActionResult GetPlayer()
        {
            var sessionId = HttpContext.Session.Id;
            var player = _playerService.GetPlayerBySession(sessionId);
            
            return Json(new { 
                success = player != null, 
                player = player 
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePlayer(
            [FromForm] string name, 
            [FromForm] string? color = null,
            [FromForm] IFormFile? avatar = null)
        {
            try
            {
                var sessionId = HttpContext.Session.Id;
                var player = _playerService.CreateOrUpdatePlayer(sessionId, name, avatar, color);
                
                return Json(new { 
                    success = true, 
                    player = new {
                        player.Id,
                        player.Name,
                        player.Color,
                        player.AvatarPath
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("uploadAvatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            try
            {
                var sessionId = HttpContext.Session.Id;
                var player = _playerService.GetPlayerBySession(sessionId);
                
                if (player == null)
                    return Json(new { success = false, error = "Player not found" });

                var playerUpdate = _playerService.CreateOrUpdatePlayer(sessionId, player.Name, avatar, player.Color);
                
                return Json(new { 
                    success = true, 
                    avatarPath = playerUpdate.AvatarPath 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}