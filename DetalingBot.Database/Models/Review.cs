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
    public string? PhotoBeforePath { get; set; }
    public string? PhotoAfterPath { get; set; }
    public DateTime ReviewDate { get; set; }

    private int _rating;
    public int Rating
    {
        get => _rating;
        set => _rating = value is >= 1 and <= 5 ? value
            : throw new ArgumentException("Rating must be between 1 and 5");
    }
}