using System.ComponentModel.DataAnnotations.Schema;

[Table("ServiceCategories")]
public class ServiceCategory : BaseEntity
{
    public string Description { get; set; }

    public ICollection<Service> Services { get; set; }
}