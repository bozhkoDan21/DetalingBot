using DetalingBot.Logger;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Сервис для работы с каталогом услуг
/// </summary>
public class ServiceCatalogService : IServiceCatalogService
{
    private readonly AppDbContext _context;
    private readonly ICustomLogger _logger;

    public ServiceCatalogService(
        AppDbContext context,
        ICustomLogger logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ServiceCategory>> GetCategoriesAsync()
    {
        return await _context.ServiceCategories
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ServiceCategory> GetCategoryByIdAsync(int categoryId)
    {
        var category = await _context.ServiceCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null)
        {
            throw new NotFoundException("Service category", categoryId);
        }

        return category;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Service>> GetServicesByCategoryAsync(int categoryId)
    {
        await GetCategoryByIdAsync(categoryId); // Проверяем существование категории

        return await _context.Services
            .Where(s => s.ServiceCategoryId == categoryId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Service>> GetTopServicesByCategoryAsync(int categoryId, int count = 3)
    {
        await GetCategoryByIdAsync(categoryId); // Проверяем существование категории

        return await _context.Services
            .Where(s => s.ServiceCategoryId == categoryId)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ServiceAvailabilityResult> CalculateServiceAvailabilityAsync(
        int serviceId,
        DateTime date)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId);

        if (service == null)
        {
            throw new NotFoundException("Service", serviceId);
        }

        var isAvailable = !await _context.Appointments
            .AnyAsync(a => a.ServiceId == serviceId &&
                        a.AppointmentDate.Date == date.Date &&
                        a.Status == AppointmentStatus.Confirmed);

        return new ServiceAvailabilityResult(
            isAvailable,
            service.Price,
            service.DurationMinutes,
            date);
    }
}