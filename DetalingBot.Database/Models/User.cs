using System.ComponentModel.DataAnnotations.Schema;

[Table("Users")]
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Phone { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public long? TelegramChatId { get; set; }
    public bool IsManager { get; set; } 
}
