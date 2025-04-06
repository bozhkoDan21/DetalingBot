using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram.Bot;

/// <summary>
/// Сервис для работы с медиафайлами в Telegram (фото, документы и т.д.)
/// Обеспечивает загрузку, хранение и обработку медиа-контента
/// </summary>
public class TelegramMediaService : ITelegramMediaService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ICustomLogger _logger;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IMapper _mapper;

    public TelegramMediaService(
        ITelegramBotClient botClient,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ICustomLogger logger,
        IMapper mapper)
    {
        _botClient = botClient;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Пересылает фото менеджеру
    /// </summary>
    /// <param name="clientId">ID клиента, отправившего фото</param>
    /// <param name="fileId">ID файла в Telegram</param>
    /// <returns>Отправленное сообщение</returns>
    public async Task<Message> ForwardPhotoToManagerAsync(long clientId, string fileId)
    {
        // Получаем chat_id менеджера из БД или конфига
        long managerChatId = await GetManagerChatIdAsync();

        return await _botClient.SendPhotoAsync(
            chatId: managerChatId,
            photo: InputFile.FromFileId(fileId),
            caption: $"Фото от клиента #{clientId}");
    }

    /// <summary>
    /// Получает временную ссылку на фото в Telegram
    /// </summary>
    /// <param name="fileId">ID файла в Telegram</param>
    /// <returns>Временная ссылка на файл</returns>
    public async Task<string> GetTemporaryPhotoLinkAsync(string fileId)
    {
        // Генерируем временную ссылку (Telegram File API)
        var file = await _botClient.GetFileAsync(fileId);
        return $"https://api.telegram.org/file/bot<token>/{file.FilePath}";
    }

    /// <summary>
    /// Преобразует отзыв с фото в DTO, добавляя URL фотографий
    /// </summary>
    /// <param name="review">Модель отзыва из базы данных</param>
    /// <returns>DTO отзыва с URL фотографий</returns>
    public async Task<DTO_Review> MapReviewWithPhotoUrlsAsync(Review review)
    {
        var dto = _mapper.Map<DTO_Review>(review);

        if (!string.IsNullOrEmpty(review.PhotoBeforeTempId))
        {
            dto.PhotoBeforeUrl = await GetTemporaryPhotoLinkAsync(review.PhotoBeforeTempId);
        }

        if (!string.IsNullOrEmpty(review.PhotoAfterTempId))
        {
            dto.PhotoAfterUrl = await GetTemporaryPhotoLinkAsync(review.PhotoAfterTempId);
        }

        return dto;
    }

    /// <summary>
    /// Получает chat_id менеджера из базы данных
    /// </summary>
    /// <returns>chat_id менеджера</returns>
    /// <exception cref="InvalidOperationException">Если менеджер не найден</exception>
    private async Task<long> GetManagerChatIdAsync()
    {
        // Вариант 1: Из конфигурации (простой способ)
        // return long.Parse(_configuration["Telegram:ManagerChatId"]);

        // Вариант 2: Из базы данных (более гибкий)
        await using var context = _dbContextFactory.CreateDbContext();
        var manager = await context.Users
            .Where(u => u.IsManager)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync();

        return manager?.TelegramChatId
            ?? throw new InvalidOperationException("No manager found in database");
    }
}