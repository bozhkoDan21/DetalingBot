using DetalingBot.DTO.Model;
using Microsoft.AspNetCore.Mvc;
using System;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly ICustomLogger _logger;  

    public ReviewsController(AppDbContext context, IFileStorageService fileStorage, ICustomLogger logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _logger = logger;  
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] DTO_CreateReview dto)
    {
        if (dto == null)
        {
            _logger.LogError(new ArgumentNullException(nameof(dto)), "DTO is null in Create Review");
            return BadRequest("Invalid data.");
        }

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var review = new Review
                {
                    UserId = dto.UserId,
                    AppointmentId = dto.AppointmentId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    ReviewDate = DateTime.UtcNow
                };

                // Обработка фото
                if (dto.BeforePhoto != null)
                {
                    review.PhotoBeforePath = await _fileStorage.SaveFileAsync(dto.BeforePhoto);
                    _logger.LogInformation("Before photo saved: " + review.PhotoBeforePath);
                }

                if (dto.AfterPhoto != null)
                {
                    review.PhotoAfterPath = await _fileStorage.SaveFileAsync(dto.AfterPhoto);
                    _logger.LogInformation("After photo saved: " + review.PhotoAfterPath);
                }

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Review created for AppointmentId: {dto.AppointmentId}, UserId: {dto.UserId}");
                return Ok(review);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred while creating review");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }
}
