public class DTO_Appointment
{
    public class Request
    {
        public int UserId { get; set; }
        public int ServiceId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class CancelRequest
    {
        public string Reason { get; set; }
    }

    public class RescheduleRequest
    {
        public DateTime NewDate { get; set; }
        public TimeSpan NewTime { get; set; }
    }

    public class Response
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string CancellationReason { get; set; }
        public string DisplayInfo { get; set; }
    }
}