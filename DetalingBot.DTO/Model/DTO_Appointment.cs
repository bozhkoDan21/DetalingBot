public class DTO_Appointment
{
    public class Response
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; }
    }

    public class Request
    {
        public int ServiceId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
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
}