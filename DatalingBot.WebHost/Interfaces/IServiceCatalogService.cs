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
}