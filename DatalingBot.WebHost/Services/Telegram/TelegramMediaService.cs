using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.Configuration;

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
    private readonly string _tempStoragePath;
    private readonly string _botToken;

    public TelegramMediaService(
        ITelegramBotClient botClient,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ICustomLogger logger,
        IMapper mapper,
        IConfiguration config)
    {
        _botClient = botClient;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _mapper = mapper;
        _tempStoragePath = config["FileStorage:TempPath"] ?? "TempUploads";
        _botToken = config["Telegram:Token"]; // Получаем токен из конфигурации

        Directory.CreateDirectory(_tempStoragePath);
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

    /// <summary>
    /// Сохраняет фото во временное хранилище
    /// </summary>
    /// <param name="fileId">ID файла в Telegram</param>
    /// <returns>Временный идентификатор файла</returns>
    public async Task<string> StoreTempPhotoAsync(string fileId)
    {
        try
        {
            // Получаем информацию о файле
            var file = await _botClient.GetFileAsync(fileId);

            if (string.IsNullOrEmpty(file.FilePath))
            {
                throw new Exception("File path is not available");
            }

            // Генерируем уникальное имя файла
            var tempId = Guid.NewGuid().ToString();
            var tempFilePath = Path.Combine(_tempStoragePath, tempId + ".jpg");

            // Создаем HTTP клиент для скачивания файла
            using var httpClient = new HttpClient();

            // Формируем URL для скачивания файла
            var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{file.FilePath}";

            // Скачиваем и сохраняем файл
            await using (var fileStream = System.IO.File.Create(tempFilePath))
            {
                var response = await httpClient.GetAsync(fileUrl);
                await response.Content.CopyToAsync(fileStream);
            }

            _logger.LogInformation($"Temporarily stored photo with tempId: {tempId}");
            return tempId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing temporary photo");
            throw;
        }
    }

    /// <summary>
    /// Переносит фото из временного хранилища в постоянное
    /// </summary>
    /// <param name="tempId">Временный идентификатор файла</param>
    /// <param name="destinationFolder">Целевая папка</param>
    /// <returns>Относительный URL сохраненного файла</returns>
    public async Task<string> MoveFromTempAsync(string tempId, string destinationFolder)
    {
        try
        {
            var tempFilePath = Path.Combine(_tempStoragePath, tempId + ".jpg");
            if (!System.IO.File.Exists(tempFilePath))
            {
                throw new FileNotFoundException("Temporary file not found", tempFilePath);
            }

            var targetDir = Path.Combine("wwwroot", destinationFolder);
            Directory.CreateDirectory(targetDir);

            var fileName = $"{Guid.NewGuid()}.jpg";
            var targetPath = Path.Combine(targetDir, fileName);

            System.IO.File.Move(tempFilePath, targetPath);

            return $"/{destinationFolder}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error moving file from temp storage: {tempId}");
            throw;
        }
    }
}