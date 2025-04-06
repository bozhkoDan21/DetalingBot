using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

public class DTO_Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }  
    public int Rating { get; set; }
    public string Comment { get; set; }
    public DateTime ReviewDate { get; set; }
    public string? PhotoBeforeUrl { get; set; }
    public string? PhotoAfterUrl { get; set; }

    [NotMapped]
    public bool HasPhotos => !string.IsNullOrEmpty(PhotoBeforeUrl) ||
                           !string.IsNullOrEmpty(PhotoAfterUrl);
}

public class DTO_CreateReviewRequest
{
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public IFormFile? BeforePhoto { get; set; }  
    public IFormFile? AfterPhoto { get; set; }   
}

// 2. DTO для внутренней работы (с TempId)
public class DTO_CreateReview
{
    public int UserId { get; set; }
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public string? PhotoBeforeTempId { get; set; }  
    public string? PhotoAfterTempId { get; set; }   
}