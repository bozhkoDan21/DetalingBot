using DetalingBot.Logger;
using Microsoft.EntityFrameworkCore;

namespace DetalingBot.Services
{
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

        /// <summary>
        /// Получает все категории услуг
        /// </summary>
        public async Task<IEnumerable<ServiceCategory>> GetCategoriesAsync()
        {
            return await _context.ServiceCategories
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Получает услуги по категории
        /// </summary>
        /// <exception cref="NotFoundException">Если категория не найдена</exception>
        public async Task<IEnumerable<Service>> GetServicesByCategoryAsync(int categoryId)
        {
            var categoryExists = await _context.ServiceCategories
                .AnyAsync(c => c.Id == categoryId);

            if (!categoryExists)
            {
                throw new NotFoundException("Service category", categoryId);
            }

            return await _context.Services
                .Where(s => s.ServiceCategoryId == categoryId)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Проверяет доступность услуги на указанную дату
        /// </summary>
        /// <exception cref="NotFoundException">Если услуга не найдена</exception>
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
}