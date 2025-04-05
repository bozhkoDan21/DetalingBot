public interface INotificationService
{
    Task SendAppointmentConfirmation(int appointmentId);
    Task SendAppointmentCancellation(int appointmentId);
    Task SendAppointmentRescheduled(int appointmentId); 
    Task SendReminders();
}