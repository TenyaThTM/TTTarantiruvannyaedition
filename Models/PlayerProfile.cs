using System.ComponentModel.DataAnnotations;

namespace TicTacToeGame.Models
{
     public class Player
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } // Для связи с сессией
        
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Name { get; set; } = "Player";
        
        public string? AvatarPath { get; set; }
        public string Color { get; set; } = "#FFFFFF";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastSeen { get; set; } = DateTime.Now;
        
        // Для игрового процесса
        public bool IsReady { get; set; }
        public int Score { get; set; }
    }

     public class PlayerSession
    {
        public string SessionId { get; set; }
        public string PlayerId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}