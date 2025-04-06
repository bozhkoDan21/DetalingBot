/// <summary>
/// Интерфейс сервиса для работы с отзывами
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Создает новый отзыв
    /// </summary>
    /// <exception cref="NotFoundException">Если связанная запись не найдена</exception>
    /// <exception cref="FileValidationException">При ошибке валидации файлов</exception>
    Task<Review> CreateReviewAsync(DTO_CreateReview dto);

    /// <summary>
    /// Получает отзывы по идентификатору записи
    /// </summary>
    Task<IEnumerable<Review>> GetReviewsByAppointmentAsync(int appointmentId);
}