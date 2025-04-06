using AutoMapper;
using DetalingBot.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DetalingBot.Controllers
{
    /// <summary>
    /// Контроллер для управления услугами и их категориями
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ServicesController : ControllerBase
    {
        private readonly IServiceCatalogService _serviceCatalog;
        private readonly ICustomLogger _logger;
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public ServicesController(
            IServiceCatalogService serviceCatalog,
            ICustomLogger logger,
            IMapper mapper,
            IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _serviceCatalog = serviceCatalog;
            _logger = logger;
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Получает все категории услуг
        /// </summary>
        /// <response code="200">Список категорий услуг</response>
        /// <response code="500">Ошибка сервера</response>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(IEnumerable<DTO_ServiceCategory>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ResponseCache(Duration = 60)]
        public async Task<IActionResult> GetCategories()
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var categories = await _serviceCatalog.GetCategoriesAsync();
                await transaction.CommitAsync();
                return Ok(_mapper.Map<IEnumerable<DTO_ServiceCategory>>(categories));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error fetching service categories");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        /// <summary>
        /// Получает услуги по категории
        /// </summary>
        /// <param name="categoryId">ID категории услуг</param>
        /// <response code="200">Список услуг в категории</response>
        /// <response code="404">Категория не найдена</response>
        /// <response code="500">Ошибка сервера</response>
        [HttpGet("by-category/{categoryId}")]
        [ProducesResponseType(typeof(IEnumerable<DTO_Service>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var services = await _serviceCatalog.GetServicesByCategoryAsync(categoryId);
                await transaction.CommitAsync();
                return Ok(_mapper.Map<IEnumerable<DTO_Service>>(services));
            }
            catch (NotFoundException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Category not found: {CategoryId}", categoryId);
                return NotFound(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error fetching services for category {CategoryId}", categoryId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        /// <summary>
        /// Рассчитывает стоимость и доступность услуги
        /// </summary>
        /// <param name="serviceId">ID услуги</param>
        /// <param name="date">Дата для проверки доступности</param>
        /// <response code="200">Данные о доступности и стоимости</response>
        /// <response code="404">Услуга не найдена</response>
        /// <response code="500">Ошибка сервера</response>
        [HttpGet("calculate/{serviceId}")]
        [ProducesResponseType(typeof(DTO_ServiceAvailability), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Calculate(int serviceId, [FromQuery] DateTime date)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var result = await _serviceCatalog.CalculateServiceAvailabilityAsync(serviceId, date);
                await transaction.CommitAsync();
                return Ok(_mapper.Map<DTO_ServiceAvailability>(result));
            }
            catch (NotFoundException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Service not found: {ServiceId}", serviceId);
                return NotFound(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error calculating availability for service {ServiceId}", serviceId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }
    }
}