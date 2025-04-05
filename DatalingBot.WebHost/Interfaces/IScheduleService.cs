public interface IScheduleService
{
    Task<bool> IsTimeSlotAvailable(int serviceId, DateTime date, TimeSpan startTime, int durationMinutes);
    Task<IEnumerable<TimeSpan>> GetAvailableTimeSlots(int serviceId, DateTime date);
}