using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITelegramBotClient _botClient;
    private readonly ICustomLogger _logger;  

    public ChatController(AppDbContext context, ITelegramBotClient botClient, ICustomLogger logger)
    {
        _context = context;
        _botClient = botClient;
        _logger = logger;  
    }

    [HttpPost("send-to-manager")]
    public async Task<IActionResult> SendToManager([FromBody] DTO_ChatMessage dto)
    {
        if (dto == null)
        {
            _logger.LogError(new ArgumentNullException(nameof(dto)), "DTO is null in SendToManager");
            return BadRequest("Invalid data.");
        }

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var managerChatId = await _context.Users
                    .Where(u => u.IsManager)
                    .Select(u => u.TelegramChatId)
                    .FirstOrDefaultAsync();

                if (managerChatId == null)
                {
                    _logger.LogWarning("Manager not found in SendToManager");
                    return BadRequest("Manager not available");
                }

                _logger.LogInformation($"Sending message to manager: {dto.Message} from User {dto.UserId}");

                await _botClient.SendTextMessageAsync(
                    chatId: managerChatId,
                    text: $"Сообщение от клиента {dto.UserId}:\n{dto.Message}");

                await transaction.CommitAsync();

                _logger.LogInformation($"Message successfully sent to manager by User {dto.UserId}");

                return Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred in SendToManager");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }
}
