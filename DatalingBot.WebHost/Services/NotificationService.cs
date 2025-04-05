using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ITelegramBotClient _botClient;
    private readonly ICustomLogger _logger;

    public NotificationService(AppDbContext db, ITelegramBotClient botClient, ICustomLogger logger)
    {
        _db = db;
        _botClient = botClient;
        _logger = logger;
    }

    public async Task SendAppointmentConfirmation(int appointmentId)
    {
        var appointment = await _db.Appointments
            .Include(a => a.User)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment?.User?.TelegramChatId == null)
        {
            return;
        }

        var message = $"Ваша запись подтверждена!\n" +
                     $"Услуга: {appointment.Service.Name}\n" +
                     $"Дата: {appointment.AppointmentDate:dd.MM.yyyy HH:mm}";

        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: appointment.User.TelegramChatId.Value,
                text: message
            );
        }
        catch (Exception ex)
        {
            // Логируем ошибку отправки сообщения
            _logger.LogError(ex, "Error sending appointment confirmation message.");
        }
    }

    public async Task SendReminders()
    {
        var appointments = await _db.Appointments
            .Where(a => a.AppointmentDate.Date == DateTime.Now.Date.AddDays(1) &&
                       a.Status == AppointmentStatus.Confirmed)
            .Include(a => a.User)
            .Include(a => a.Service)
            .ToListAsync();

        foreach (var appointment in appointments)
        {
            var message = $"Напоминание о записи!\n" +
                         $"Завтра в {appointment.AppointmentDate:HH:mm} - {appointment.Service.Name}";

            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: appointment.User.TelegramChatId,
                    text: message
                );
            }
            catch (Exception ex)
            {
                // Логируем ошибку отправки напоминания
                _logger.LogError(ex, "Error sending reminder for appointment.");
            }
        }
    }
}
