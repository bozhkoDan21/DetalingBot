using System.ComponentModel.DataAnnotations.Schema;

[Table("Appointments")]
public class Appointment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int ServiceId { get; set; }
    public Service? Service { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public AppointmentStatus Status { get; set; }

    public ICollection<Review>? Reviews { get; set; }
}