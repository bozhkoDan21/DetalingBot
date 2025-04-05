using Microsoft.AspNetCore.Http;

namespace DetalingBot.DTO.Model
{
    public class DTO_CreateReview
    {
        public int UserId { get; set; }
        public int AppointmentId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }

        // Фото до и после
        public IFormFile BeforePhoto { get; set; }
        public IFormFile AfterPhoto { get; set; }
    }
}
