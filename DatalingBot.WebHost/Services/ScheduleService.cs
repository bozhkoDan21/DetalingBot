using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DetalingBot.Infrastructure.Services;

public class ScheduleService : IScheduleService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(AppDbContext context, ILogger<ScheduleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Проверяет, доступен ли указанный временной интервал для записи на услугу
    /// </summary>
    /// <param name="serviceId">ID услуги</param>
    /// <param name="date">Дата записи</param>
    /// <param name="startTime">Время начала записи</param>
    /// <param name="durationMinutes">Продолжительность услуги в минутах</param>
    /// <returns>True, если временной интервал доступен для записи, иначе False</returns>
    public async Task<bool> IsTimeSlotAvailable(int serviceId, DateTime date, TimeSpan startTime, int durationMinutes)
    {
        try
        {
            var endTime = startTime.Add(TimeSpan.FromMinutes(durationMinutes));

            var hasConflict = await _context.Appointments
                .Where(a => a.ServiceId == serviceId &&
                           a.AppointmentDate.Date == date.Date &&
                           a.Status != AppointmentStatus.Cancelled)
                .AnyAsync(a => (a.StartTime < endTime) && (a.EndTime > startTime));

            _logger.LogDebug("Checked availability for service {ServiceId} at {Date} {StartTime}: {IsAvailable}",
                serviceId, date, startTime, !hasConflict);

            return !hasConflict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking time slot availability");
            throw;
        }
    }

    /// <summary>
    /// Получает список доступных временных интервалов для записи на указанную услугу в указанную дату
    /// </summary>
    /// <param name="serviceId">ID услуги</param>
    /// <param name="date">Дата записи</param>
    /// <returns>Коллекция доступных временных интервалов</returns>
    public async Task<IEnumerable<TimeSpan>> GetAvailableTimeSlots(int serviceId, DateTime date)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null)
        {
            _logger.LogWarning("Service {ServiceId} not found", serviceId);
            return Enumerable.Empty<TimeSpan>();
        }

        var workDayStart = new TimeSpan(9, 0, 0); // Начало рабочего дня
        var workDayEnd = new TimeSpan(21, 0, 0);  // Конец рабочего дня
        var slotDuration = TimeSpan.FromMinutes(service.DurationMinutes);
        var bufferTime = TimeSpan.FromMinutes(15); // Буфер между записями

        var busySlots = await _context.Appointments
            .Where(a => a.ServiceId == serviceId &&
                       a.AppointmentDate.Date == date.Date &&
                       a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        var availableSlots = new List<TimeSpan>();
        var currentSlot = workDayStart;

        while (currentSlot + slotDuration <= workDayEnd)
        {
            var slotEnd = currentSlot + slotDuration;
            var isAvailable = !busySlots.Any(b =>
                (b.StartTime < slotEnd) && (b.EndTime > currentSlot));

            if (isAvailable)
            {
                availableSlots.Add(currentSlot);
                currentSlot += slotDuration + bufferTime;
            }
            else
            {
                currentSlot += bufferTime;
            }
        }

        return availableSlots;
    }
}