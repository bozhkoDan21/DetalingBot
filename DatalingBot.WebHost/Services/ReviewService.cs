using AutoMapper;
using DetalingBot.Logger;
using Microsoft.EntityFrameworkCore;

namespace DetalingBot.Services
{
    /// <summary>
    /// Сервис для работы с отзывами клиентов
    /// </summary>
    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;
        private readonly ITelegramMediaService _fileStorage;
        private readonly ICustomLogger _logger;
        private readonly IMapper _mapper;

        public ReviewService(
            AppDbContext context,
            ITelegramMediaService fileStorage,
            ICustomLogger logger,
            IMapper mapper)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Создает новый отзыв
        /// </summary>
        /// <exception cref="NotFoundException">Если связанная запись не найдена</exception>
        /// <exception cref="FileValidationException">При ошибке валидации файлов</exception>
        public async Task<Review> CreateReviewAsync(DTO_CreateReview dto)
        {
            // Проверяем запись
            var appointmentExists = await _context.Appointments
                .AnyAsync(a => a.Id == dto.AppointmentId && a.UserId == dto.UserId);

            if (!appointmentExists) throw new NotFoundException("Appointment", dto.AppointmentId);

            var review = _mapper.Map<Review>(dto);
            review.ReviewDate = DateTime.UtcNow;

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Review created (ID: {ReviewId})", review.Id);
            return review;
        }

        public async Task<IEnumerable<Review>> GetReviewsByAppointmentAsync(int appointmentId)
        {
            return await _context.Reviews
                .Where(r => r.AppointmentId == appointmentId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetCompletedAppointmentsForReviewAsync(int userId)
        {
            return await _context.Appointments
                .Include(a => a.Service)
                .Where(a => a.UserId == userId &&
                           a.Status == AppointmentStatus.Completed &&
                           !_context.Reviews.Any(r => r.AppointmentId == a.Id))
                .AsNoTracking()
                .ToListAsync();
        }
    }
}