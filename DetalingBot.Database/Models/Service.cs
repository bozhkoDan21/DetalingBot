using System.ComponentModel.DataAnnotations.Schema;

[Table("Services")]
public class Service : BaseEntity
{
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public int? ServiceCategoryId { get; set; }
    public ServiceCategory Category { get; set; }

    public ICollection<Appointment> Appointments { get; set; }
}