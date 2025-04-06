public interface ITelegramNotificationService
{
    /// <summary>
    /// Отправляет сообщение менеджеру от имени пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя.</param>
    /// <param name="message">Текст сообщения.</param>
    /// <exception cref="ManagerNotFoundException">Если менеджер не найден.</exception>
    /// <exception cref="TelegramApiException">При ошибке API Telegram.</exception>
    Task SendMessageToManagerAsync(long userId, string message);

    /// <summary>
    /// Отправляет сообщение конкретному пользователю
    /// </summary>
    /// <param name="chatId">Telegram Chat ID пользователя</param>
    /// <param name="message">Текст сообщения</param>
    /// <exception cref="TelegramApiException">При ошибке API Telegram</exception>
    Task SendMessageToUserAsync(long chatId, string message);

    /// <summary>
    /// Рассылает сообщение всем пользователям бота
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <exception cref="TelegramApiException">При ошибке API Telegram</exception>
    Task BroadcastToAllUsersAsync(string message);
}