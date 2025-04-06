using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class BotBackgroundService : BackgroundService
{
    private readonly TelegramBotService _botService;
    private readonly ILogger<BotBackgroundService> _logger;

    public BotBackgroundService(
        TelegramBotService botService,
        ILogger<BotBackgroundService> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram Bot Service");
        await _botService.StartBotAsync(stoppingToken);
    }
}