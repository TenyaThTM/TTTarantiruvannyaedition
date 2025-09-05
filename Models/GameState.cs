namespace TicTacToeGame.Models
{
    public class GameState
    {
        public int Id { get; set; }
        public int Width { get; set; } = 5;
        public int Height { get; set; } = 5;
        public int BlockChance { get; set; } = 15;
        public int WinLength { get; set; } = 4;
        public int CaptureProgress { get; set; } = 2;
        public List<Player> Players { get; set; } = new List<Player>();
        public int CurrentPlayerIndex { get; set; } = 0;
        public Cell[,] Board { get; set; }
        public bool GameOver { get; set; } = false;
        public string Winner { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    

    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool Blocked { get; set; }
        public string Owner { get; set; }
        public int Progress { get; set; }
    }

    public class LobbyInfo
    {
        public int Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; } = 6;
        public DateTime CreatedAt { get; set; }
    }
}