using Telegram.Bot.Types;

public interface ITelegramMediaService
{
    /// <summary>
    /// Пересылает фото менеджеру
    /// </summary>
    Task<Message> ForwardPhotoToManagerAsync(long clientId, string fileId);

    /// <summary>
    /// Получает временную ссылку на фото в Telegram
    /// </summary>
    Task<string> GetTemporaryPhotoLinkAsync(string fileId);

    /// <summary>
    /// Преобразует отзыв с фото в DTO, добавляя URL фотографий
    /// </summary>
    Task<DTO_Review> MapReviewWithPhotoUrlsAsync(Review review);

    /// <summary>
    /// Сохраняет фото во временное хранилище
    /// </summary>
    Task<string> StoreTempPhotoAsync(string fileId);

    /// <summary>
    /// Переносит фото из временного хранилища в постоянное
    /// </summary>
    Task<string> MoveFromTempAsync(string tempId, string destinationFolder);
}
