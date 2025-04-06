using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Контроллер для управления записями на услуги.
/// Обеспечивает создание, просмотр, отмену и перенос записей.
/// Все методы требуют аутентификации пользователя.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AppointmentsController> _logger;
    private readonly IScheduleService _scheduleService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public AppointmentsController(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<AppointmentsController> logger,
        IScheduleService scheduleService,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
        _scheduleService = scheduleService;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    /// <summary>
    /// Создание новой записи на услугу
    /// </summary>
    /// <param name="request">Данные для создания записи</param>
    /// <response code="201">Возвращает созданную запись</response>
    /// <response code="400">Если данные невалидны</response>
    /// <response code="404">Если услуга не найдена</response>
    /// <response code="409">Если временной слот занят</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DTO_Appointment.Response>> CreateAppointment([FromBody] DTO_Appointment.Request request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null)
            {
                _logger.LogWarning("Service {ServiceId} not found", request.ServiceId);
                return NotFound("Service not found");
            }

            if (!await _scheduleService.IsTimeSlotAvailable(
                request.ServiceId, request.Date, request.Time, service.DurationMinutes))
            {
                _logger.LogWarning("Time slot not available for service {ServiceId}", request.ServiceId);
                return Conflict("Time slot not available");
            }

            var appointment = _mapper.Map<Appointment>(request, opts =>
                opts.Items["CurrentUserId"] = _currentUserService.UserId);

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            await _notificationService.SendAppointmentConfirmation(appointment.Id);
            await transaction.CommitAsync();

            _logger.LogInformation("Appointment {AppointmentId} created successfully", appointment.Id);

            return CreatedAtAction(
                nameof(GetAppointment),
                new { id = appointment.Id },
                _mapper.Map<DTO_Appointment.Response>(appointment));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating appointment");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Получение деталей конкретной записи
    /// </summary>
    /// <param name="id">ID записи</param>
    /// <response code="200">Возвращает данные записи</response>
    /// <response code="404">Если запись не найдена</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DTO_Appointment.Response>> GetAppointment(int id)
    {
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == _currentUserService.UserId);

            if (appointment == null)
            {
                _logger.LogWarning("Appointment {AppointmentId} not found", id);
                return NotFound();
            }

            return _mapper.Map<DTO_Appointment.Response>(appointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointment {AppointmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Получение списка предстоящих записей пользователя
    /// </summary>
    /// <returns>Список записей со статусом Confirmed на текущую дату и позднее</returns>
    [HttpGet("upcoming")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DTO_Appointment.Response>>> GetUpcomingAppointments()
    {
        try
        {
            var now = DateTime.Now;
            var appointments = await _context.Appointments
                .Where(a => a.UserId == _currentUserService.UserId &&
                           a.AppointmentDate >= now.Date &&
                           a.Status == AppointmentStatus.Confirmed)
                .Include(a => a.Service)
                .Include(a => a.User)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .AsNoTracking()
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<DTO_Appointment.Response>>(appointments));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming appointments");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Перенос записи на другое время
    /// </summary>
    /// <param name="id">ID записи</param>
    /// <param name="request">Новые дата и время</param>
    /// <response code="204">При успешном переносе</response>
    /// <response code="400">При нарушении бизнес-правил</response>
    /// <response code="404">Если запись не найдена</response>
    /// <response code="409">Если новый слот занят</response>
    [HttpPatch("{id}/reschedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RescheduleAppointment(int id, [FromBody] DTO_Appointment.RescheduleRequest request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == _currentUserService.UserId);

            if (appointment == null)
                return NotFound($"Appointment {id} not found");

            if (appointment.Status != AppointmentStatus.Confirmed)
                return BadRequest("Only confirmed appointments can be rescheduled");

            var minRescheduleTime = DateTime.Now.AddHours(2);
            if (appointment.AppointmentDate < minRescheduleTime)
                return BadRequest("Reschedule must be at least 2 hours before appointment");

            var newEndTime = request.NewTime.Add(TimeSpan.FromMinutes(appointment.Service.DurationMinutes));
            var durationMinutes = (int)TimeSpan.FromMinutes(appointment.Service.DurationMinutes).TotalMinutes;

            var isAvailable = await _scheduleService.IsTimeSlotAvailable(appointment.ServiceId,request.NewDate,request.NewTime,durationMinutes); 

            if (!isAvailable)
                return Conflict("Selected time slot is not available");

            appointment.AppointmentDate = request.NewDate;
            appointment.StartTime = request.NewTime;
            appointment.EndTime = newEndTime;
            appointment.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _notificationService.SendAppointmentRescheduled(
                appointment.Id,
                request.NewDate,
                request.NewTime);

            await transaction.CommitAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    /// <summary>
    /// Отмена существующей записи
    /// </summary>
    /// <param name="id">ID записи</param>
    /// <param name="request">Причина отмены</param>
    /// <response code="204">При успешной отмене</response>
    /// <response code="400">При нарушении бизнес-правил</response>
    /// <response code="404">Если запись не найдена</response>
    [HttpPatch("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelAppointment(int id, [FromBody] DTO_Appointment.CancelRequest request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == _currentUserService.UserId);

            if (appointment == null)
            {
                _logger.LogWarning("Appointment {AppointmentId} not found for cancellation", id);
                return NotFound();
            }

            if (appointment.Status != AppointmentStatus.Confirmed)
            {
                _logger.LogWarning("Attempt to cancel non-confirmed appointment {AppointmentId}", id);
                return BadRequest("Only confirmed appointments can be cancelled");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = request.Reason;
            appointment.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Исправленный вызов с передачей причины
            await _notificationService.SendAppointmentCancellation(appointment.Id, request.Reason);

            await transaction.CommitAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}