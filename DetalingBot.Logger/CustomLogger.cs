using Microsoft.Extensions.Logging;

public class CustomLogger : ICustomLogger
{
    private readonly ILogger<CustomLogger> _logger;

    public CustomLogger(ILogger<CustomLogger> logger)
    {
        _logger = logger;
    }

    public void LogError(Exception exception, string message)
    {
        _logger.LogError(exception, message);
    }

    public void LogInformation(string message)
    {
        _logger.LogInformation(message);
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning(message);
    }
}
