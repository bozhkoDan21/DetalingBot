using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ICustomLogger _logger;  

    public AppointmentsController(
        AppDbContext context,
        INotificationService notificationService,
        ICustomLogger logger)  
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;  
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DTO_Appointment dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var appointment = new Appointment
                {
                    UserId = dto.UserId,
                    ServiceId = dto.ServiceId,
                    AppointmentDate = dto.Date,
                    StartTime = dto.Time,
                    EndTime = dto.Time.Add(TimeSpan.FromMinutes(dto.DurationMinutes)),
                    Status = AppointmentStatus.Confirmed
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                await _notificationService.SendAppointmentConfirmation(appointment.Id);
                await transaction.CommitAsync();

                _logger.LogInformation($"Appointment created successfully for User {dto.UserId} with Service {dto.ServiceId}");

                return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred while creating appointment");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int userId)
    {
        try
        {
            var appointments = await _context.Appointments
                .Where(a => a.UserId == userId && a.AppointmentDate >= DateTime.Now)
                .Include(a => a.Service)
                .ToListAsync();

            _logger.LogInformation($"Fetched {appointments.Count} upcoming appointments for User {userId}");

            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching upcoming appointments");
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                _logger.LogWarning($"Appointment with ID {id} not found");
                return NotFound();
            }

            _logger.LogInformation($"Fetched appointment with ID {id}");

            return Ok(appointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching appointment by ID");
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }
}
