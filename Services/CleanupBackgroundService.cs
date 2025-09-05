using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace TicTacToeGame.Services
{
    public class CleanupBackgroundService : BackgroundService
    {
        private readonly GameService _gameService;
        private readonly PlayerService _playerService;
        private readonly ILogger<CleanupBackgroundService> _logger;

        public CleanupBackgroundService(GameService gameService, PlayerService playerService, ILogger<CleanupBackgroundService> logger)
        {
            _gameService = gameService;
            _playerService = playerService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cleanup Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Очищаем каждые 5 минут
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    
                    _gameService.CleanupOldSessions();
                    _playerService.CleanupOldPlayers();
                    
                    _logger.LogInformation("Cleanup completed successfully.");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during cleanup.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Ждем перед повторной попыткой
                }
            }

            _logger.LogInformation("Cleanup Background Service is stopping.");
        }
    }
}