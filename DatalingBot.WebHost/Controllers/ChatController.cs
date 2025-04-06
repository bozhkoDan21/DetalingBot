using DetalingBot.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DetalingBot.Controllers
{
    /// <summary>
    /// Контроллер для обработки сообщений между клиентами и менеджерами
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ITelegramNotificationService _telegramService;
        private readonly ICustomLogger _logger;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public ChatController(
            ITelegramNotificationService telegramService,
            ICustomLogger logger,
            IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _telegramService = telegramService;
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Отправляет сообщение менеджеру
        /// </summary>
        /// <param name="dto">Данные сообщения</param>
        /// <returns>Результат операции</returns>
        /// <response code="200">Сообщение успешно отправлено</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="404">Менеджер не найден</response>
        /// <response code="500">Ошибка сервера</response>
        [HttpPost("send-to-manager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendToManager([FromBody] DTO_ChatMessage dto)
        {
            if (dto == null)
            {
                _logger.LogError("Chat message DTO is null");
                return BadRequest(new { Error = "Invalid request data" });
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await _telegramService.SendMessageToManagerAsync(dto.UserId, dto.Message);
                await transaction.CommitAsync();
                return Ok();
            }
            catch (ArgumentException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Invalid message data (User: {UserId})", dto.UserId);
                return BadRequest(new { Error = ex.Message });
            }
            catch (ManagerNotFoundException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Manager not found (User: {UserId})", dto.UserId);
                return NotFound(new { Error = "Manager not available" });
            }
            catch (TelegramApiException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Telegram API error (User: {UserId})", dto.UserId);
                return StatusCode(500, new { Error = "Failed to send message" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error (User: {UserId})", dto.UserId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }
    }
}