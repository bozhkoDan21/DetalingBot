public interface INotificationService
{
    Task SendAppointmentConfirmation(int appointmentId);
    Task SendReminders();
}
