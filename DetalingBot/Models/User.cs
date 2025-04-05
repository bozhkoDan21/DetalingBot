using System.ComponentModel.DataAnnotations.Schema;

[Table("Users")]
public class User : AuditableEntity
{
    public string Username { get; set; }
    public string Phone { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public bool IsManager { get; set; }

    // Добавляем свойство для хранения Telegram ID
    public long? TelegramChatId { get; set; }  

    public ICollection<Appointment> Appointments { get; set; }
    public ICollection<Review> Reviews { get; set; }
}
