using DetalingBot.Logger;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace DetalingBot.Services
{
    /// <summary>
    /// Реализация сервиса для работы с Telegram API
    /// </summary>
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ICustomLogger _logger;

        public TelegramNotificationService(
            ITelegramBotClient botClient,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ICustomLogger logger)
        {
            _botClient = botClient;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task SendMessageToManagerAsync(long userId, string message)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var managerChatId = await GetManagerChatIdAsync(context);
            await SendTextMessageSafeAsync(managerChatId, $"Сообщение от клиента {userId}:\n{message}", userId);
        }

        public async Task SendMessageToUserAsync(long chatId, string message)
        {
            await SendTextMessageSafeAsync(chatId, message);
        }

        public async Task BroadcastToAllUsersAsync(string message)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var chatIds = await GetActiveUserChatIdsAsync(context);

            foreach (var chatId in chatIds)
            {
                await SendTextMessageSafeAsync(chatId, message);
            }
        }

        private async Task<long> GetManagerChatIdAsync(AppDbContext context)
        {
            var chatId = await context.Users
                .Where(u => u.IsManager)
                .Select(u => u.TelegramChatId)
                .FirstOrDefaultAsync();

            if (!chatId.HasValue)
            {
                _logger.LogWarning("No manager found in database");
                throw new ManagerNotFoundException();
            }

            return chatId.Value;
        }

        private async Task<List<long>> GetActiveUserChatIdsAsync(AppDbContext context)
        {
            return await context.Users
                .Where(u => u.TelegramChatId.HasValue)
                .Select(u => u.TelegramChatId!.Value)
                .ToListAsync();
        }

        private async Task SendTextMessageSafeAsync(long chatId, string text, long? userId = null)
        {
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: text);

                _logger.LogInformation("Message sent to {ChatId} (User: {UserId})",
                    chatId, userId?.ToString() ?? "system");
            }
            catch (ApiRequestException ex)
            {
                _logger.LogError(ex, "Telegram API error (ChatId: {ChatId}, User: {UserId})",
                    chatId, userId?.ToString() ?? "system");
                throw new TelegramApiException("Failed to send Telegram message", ex);
            }
        }
    }
}