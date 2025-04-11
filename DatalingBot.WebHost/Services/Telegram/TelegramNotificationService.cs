using DetalingBot.Logger;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Exceptions;
using Telegram.Bot;

/// <summary>
/// Сервис для отправки уведомлений через Telegram
/// Обеспечивает отправку сообщений пользователям и менеджерам
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

    /// <summary>
    /// Отправляет сообщение менеджеру
    /// </summary>
    /// <param name="userId">ID пользователя-источника сообщения</param>
    /// <param name="message">Текст сообщения</param>
    public async Task SendMessageToManagerAsync(long userId, string message)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var managerChatId = await GetManagerChatIdAsync(context);
        await SendTextMessageSafeAsync(managerChatId, $"Сообщение от клиента {userId}:\n{message}", userId);
    }

    /// <summary>
    /// Отправляет сообщение пользователю
    /// </summary>
    /// <param name="chatId">ID чата пользователя</param>
    /// <param name="message">Текст сообщения</param>
    public async Task SendMessageToUserAsync(long chatId, string message)
    {
        await SendTextMessageSafeAsync(chatId, message);
    }

    /// <summary>
    /// Рассылает сообщение всем активным пользователям
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    public async Task BroadcastToAllUsersAsync(string message)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var chatIds = await GetActiveUserChatIdsAsync(context);

        foreach (var chatId in chatIds)
        {
            await SendTextMessageSafeAsync(chatId, message);
        }
    }

    /// <summary>
    /// Получает chat_id менеджера из контекста базы данных
    /// </summary>
    /// <param name="context">Контекст базы данных</param>
    /// <returns>chat_id менеджера</returns>
    /// <exception cref="ManagerNotFoundException">Если менеджер не найден</exception>
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

    /// <summary>
    /// Получает список chat_id всех активных пользователей
    /// </summary>
    /// <param name="context">Контекст базы данных</param>
    /// <returns>Список chat_id пользователей</returns>
    private async Task<List<long>> GetActiveUserChatIdsAsync(AppDbContext context)
    {
        return await context.Users
            .Where(u => u.TelegramChatId.HasValue)
            .Select(u => u.TelegramChatId!.Value)
            .ToListAsync();
    }

    /// <summary>
    /// Безопасно отправляет текстовое сообщение с обработкой ошибок
    /// </summary>
    /// <param name="chatId">ID чата получателя</param>
    /// <param name="text">Текст сообщения</param>
    /// <param name="userId">ID пользователя (опционально, для логирования)</param>
    /// <exception cref="TelegramApiException">При ошибке API Telegram</exception>
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