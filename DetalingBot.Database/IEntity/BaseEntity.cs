// Базовый класс для всех сущностей
public abstract class BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
}

// Базовый класс для сущностей с аудитом
public abstract class AuditableEntity : BaseEntity
{
    public string CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}