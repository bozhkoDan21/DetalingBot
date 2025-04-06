using System.ComponentModel.DataAnnotations.Schema;

[Table("ServiceCategories")]
public class ServiceCategory
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
}