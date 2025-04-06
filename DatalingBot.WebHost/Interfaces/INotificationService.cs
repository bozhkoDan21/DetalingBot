/// <summary>
/// Сервис для отправки уведомлений о записях на услуги
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Отправляет подтверждение о создании новой записи
    /// </summary>
    /// <param name="appointmentId">ID записи</param>
    Task SendAppointmentConfirmation(int appointmentId);

    /// <summary>
    /// Отправляет уведомление об отмене записи
    /// </summary>
    /// <param name="appointmentId">ID записи</param>
    /// <param name="reason">Причина отмены</param>
    Task SendAppointmentCancellation(int appointmentId, string reason);

    /// <summary>
    /// Отправляет уведомление о переносе записи
    /// </summary>
    /// <param name="appointmentId">ID записи</param>
    /// <param name="newDate">Новая дата записи</param>
    /// <param name="newTime">Новое время записи</param>
    Task SendAppointmentRescheduled(int appointmentId, DateTime newDate, TimeSpan newTime);

    /// <summary>
    /// Отправляет напоминания о предстоящих записях
    /// </summary>
    Task SendReminders();
}