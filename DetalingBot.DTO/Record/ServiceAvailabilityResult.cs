public record ServiceAvailabilityResult(
    bool IsAvailable,
    decimal Price,
    int DurationMinutes,
    DateTime AvailableDate);