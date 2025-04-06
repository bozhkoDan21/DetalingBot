using System.ComponentModel.DataAnnotations.Schema;

[Table("Reviews")]
public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; }
    public string Comment { get; set; }
    public DateTime ReviewDate { get; set; }
    public string? PhotoBeforeTempId { get; set; }
    public string? PhotoAfterTempId { get; set; }
    public int Rating { get; set; } // Проверку перенести в сервис
}