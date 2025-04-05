using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICustomLogger _logger;  

    public ServicesController(AppDbContext context, ICustomLogger logger)
    {
        _context = context;
        _logger = logger;  
    }

    // Получить все категории услуг
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            _logger.LogInformation("Fetching all service categories.");
            var categories = await _context.ServiceCategories.ToListAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service categories.");
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    // Получить услуги по категории
    [HttpGet("by-category/{categoryId}")]
    public async Task<IActionResult> GetByCategory(int categoryId)
    {
        try
        {
            _logger.LogInformation($"Fetching services for category ID: {categoryId}");
            var services = await _context.Services
                .Where(s => s.ServiceCategoryId == categoryId)
                .ToListAsync();

            if (!services.Any())
            {
                _logger.LogWarning($"No services found for category ID: {categoryId}");
                return NotFound("No services found for this category.");
            }

            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching services for category ID: {categoryId}");
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    // Рассчитать доступность услуги на конкретную дату
    [HttpGet("calculate/{serviceId}")]
    public async Task<IActionResult> Calculate(int serviceId, [FromQuery] DateTime date)
    {
        try
        {
            _logger.LogInformation($"Calculating availability for service ID: {serviceId} on date: {date.ToShortDateString()}");

            var service = await _context.Services.FindAsync(serviceId);
            if (service == null)
            {
                _logger.LogWarning($"Service with ID: {serviceId} not found.");
                return NotFound("Service not found.");
            }

            // Проверка доступности времени
            var isAvailable = !await _context.Appointments
                .AnyAsync(a => a.AppointmentDate.Date == date.Date && a.ServiceId == serviceId);

            _logger.LogInformation($"Availability for service ID: {serviceId} on date {date.ToShortDateString()} is {isAvailable}");

            return Ok(new
            {
                Price = service.Price,
                Duration = service.DurationMinutes,
                IsAvailable = isAvailable
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating availability for service.");
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }
}
