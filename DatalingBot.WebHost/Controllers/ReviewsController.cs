using AutoMapper;
using DetalingBot.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DetalingBot.Controllers
{
    /// <summary>
    /// Контроллер для управления отзывами клиентов, включая фото "до/после"
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly ICustomLogger _logger;
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public ReviewsController(
            IReviewService reviewService,
            ICustomLogger logger,
            IMapper mapper,
            IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _reviewService = reviewService;
            _logger = logger;
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Создает новый отзыв с фото "до/после"
        /// </summary>
        /// <param name="dto">Данные отзыва и фотографии</param>
        /// <returns>Созданный отзыв</returns>
        /// <response code="200">Отзыв успешно создан</response>
        /// <response code="400">Некорректные данные или ошибка валидации файлов</response>
        /// <response code="404">Связанная запись не найдена</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [HttpPost]
        [ProducesResponseType(typeof(DTO_Review), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromForm] DTO_CreateReview dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for review creation");
                return BadRequest(ModelState);
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var result = await _reviewService.CreateReviewAsync(dto);
                var response = _mapper.Map<DTO_Review>(result);

                await transaction.CommitAsync();
                _logger.LogInformation("Review created for appointment {AppointmentId}", dto.AppointmentId);
                return Ok(response);
            }
            catch (FileValidationException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "File validation failed");
                return BadRequest(new { Error = ex.Message });
            }
            catch (NotFoundException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Related entity not found");
                return NotFound(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating review");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }
    }
}