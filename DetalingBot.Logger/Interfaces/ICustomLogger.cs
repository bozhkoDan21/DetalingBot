public interface ICustomLogger
{
    void LogInformation(string message);
    void LogInformation(string message, params object[] args);
    void LogWarning(string message);
    void LogWarning(string message, params object[] args);
    void LogWarning(Exception exception, string message, params object[] args);
    void LogError(string message);
    void LogError(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
}
