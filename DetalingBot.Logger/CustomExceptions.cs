namespace DetalingBot.Logger
{
    /// <summary> Менеджер не найден в базе. </summary>
    public class ManagerNotFoundException : Exception
    {
        public ManagerNotFoundException() : base("Manager not found.") { }
    }

    /// <summary> Ошибка валидации файла. </summary>
    public class FileValidationException : Exception
    {
        public FileValidationException(string message) : base(message) { }
        public FileValidationException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary> Запрошенная сущность не найдена. </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string entityName, object id)
            : base($"{entityName} with id {id} not found") { }

        public NotFoundException(string message) : base(message) { }
    }

    /// <summary> Ошибка при работе с Telegram API. </summary>
    public class TelegramApiException : Exception
    {
        public TelegramApiException(string message, Exception inner)
            : base(message, inner) { }
    }
}