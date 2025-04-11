public interface IServiceCatalogService
{
    /// <summary>
    /// Получает все категории услуг
    /// </summary>
    Task<IEnumerable<ServiceCategory>> GetCategoriesAsync();

    /// <summary>
    /// Получает услуги по категории
    /// </summary>
    /// <exception cref="NotFoundException">Категория не найдена</exception>
    Task<IEnumerable<Service>> GetServicesByCategoryAsync(int categoryId);

    /// <summary>
    /// Проверяет доступность услуги на указанную дату
    /// </summary>
    /// <exception cref="NotFoundException">Услуга не найдена</exception>
    Task<ServiceAvailabilityResult> CalculateServiceAvailabilityAsync(int serviceId, DateTime date);

    /// <summary>
    /// Получает первые N услуг по категории (для примеров)
    /// </summary>
    /// <exception cref="NotFoundException">Если категория не найдена</exception>
    Task<IEnumerable<Service>> GetTopServicesByCategoryAsync(int categoryId, int count = 3);

    /// <summary>
    /// Получает категорию по идентификатору
    /// </summary>
    /// <exception cref="NotFoundException">Если категория не найдена</exception>
    Task<ServiceCategory> GetCategoryByIdAsync(int categoryId);
}