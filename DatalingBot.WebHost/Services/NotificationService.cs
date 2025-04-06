using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

/// <summary>
/// Сервис для отправки уведомлений о записях через Telegram бота.
/// Обеспечивает отправку подтверждений, напоминаний и уведомлений об изменениях.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext db,
        ITelegramBotClient botClient,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _botClient = botClient;
        _logger = logger;
    }

    /// <summary>
    /// Отправляет подтверждение о создании записи
    /// </summary>
    public async Task SendAppointmentConfirmation(int appointmentId)
    {
        try
        {
            var appointment = await GetAppointmentWithDetails(appointmentId);
            if (appointment?.User?.TelegramChatId == null) return;

            var message = $"Запись подтверждена\n\n" +
                        $"Услуга: {appointment.Service.Name}\n" +
                        $"Дата: {appointment.AppointmentDate:dd.MM.yyyy}\n" +
                        $"Время: {appointment.StartTime:hh\\:mm}-{appointment.EndTime:hh\\:mm}";

            await _botClient.SendTextMessageAsync(
                chatId: appointment.User.TelegramChatId.Value,
                text: message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending confirmation for appointment {AppointmentId}", appointmentId);
        }
    }

    /// <summary>
    /// Отправляет уведомление об отмене записи
    /// </summary>
    public async Task SendAppointmentCancellation(int appointmentId, string reason)
    {
        try
        {
            var appointment = await GetAppointmentWithDetails(appointmentId);
            if (appointment?.User?.TelegramChatId == null) return;

            var message = $"Запись отменена\n\n" +
                        $"Услуга: {appointment.Service.Name}\n" +
                        $"Дата: {appointment.AppointmentDate:dd.MM.yyyy}\n" +
                        $"Время: {appointment.StartTime:hh\\:mm}-{appointment.EndTime:hh\\:mm}\n" +
                        $"Причина: {reason}";

            await _botClient.SendTextMessageAsync(
                chatId: appointment.User.TelegramChatId.Value,
                text: message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending cancellation for appointment {AppointmentId}", appointmentId);
        }
    }

    /// <summary>
    /// Отправляет уведомление о переносе записи
    /// </summary>
    public async Task SendAppointmentRescheduled(int appointmentId, DateTime newDate, TimeSpan newTime)
    {
        try
        {
            var appointment = await GetAppointmentWithDetails(appointmentId);
            if (appointment?.User?.TelegramChatId == null) return;

            var endTime = newTime.Add(TimeSpan.FromMinutes(appointment.Service.DurationMinutes));
            var message = $"Запись перенесена\n\n" +
                        $"Услуга: {appointment.Service.Name}\n" +
                        $"Новая дата: {newDate:dd.MM.yyyy}\n" +
                        $"Новое время: {newTime:hh\\:mm}-{endTime:hh\\:mm}";

            await _botClient.SendTextMessageAsync(
                chatId: appointment.User.TelegramChatId.Value,
                text: message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending rescheduled notification for appointment {AppointmentId}", appointmentId);
        }
    }

    /// <summary>
    /// Отправляет напоминания о предстоящих записях
    /// </summary>
    public async Task SendReminders()
    {
        try
        {
            var reminderTime = DateTime.Now.AddHours(2);
            var appointments = await _db.Appointments
                .Where(a => a.AppointmentDate >= DateTime.Now &&
                           a.AppointmentDate <= reminderTime &&
                           a.Status == AppointmentStatus.Confirmed)
                .Include(a => a.User)
                .Include(a => a.Service)
                .ToListAsync();

            foreach (var appointment in appointments)
            {
                var timeUntilAppointment = appointment.AppointmentDate - DateTime.Now;
                var minutesUntil = (int)timeUntilAppointment.TotalMinutes;
                var message = $"Напоминание о записи через {minutesUntil} минут!\n\n" +
                             $"Услуга: {appointment.Service.Name}\n" +
                             $"Время: {appointment.AppointmentDate:HH:mm}";
                try
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: appointment.User.TelegramChatId,
                        text: message
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending reminder for appointment {AppointmentId}", appointment.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminders");
        }
    }

    private async Task<Appointment?> GetAppointmentWithDetails(int appointmentId)
    {
        return await _db.Appointments
            .Include(a => a.User)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
    }
}
