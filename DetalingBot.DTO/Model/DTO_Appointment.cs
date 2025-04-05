public class DTO_Appointment
{
    public int UserId { get; set; }
    public int ServiceId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public int DurationMinutes { get; set; }
}
