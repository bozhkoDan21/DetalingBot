using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using Telegram.Bot.Types.ReplyMarkups;

/// <summary>
/// Основной сервис для работы с Telegram ботом
/// </summary>
public class TelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ICustomLogger _logger;
    private readonly ITelegramNotificationService _notificationService;
    private readonly ITelegramMediaService _mediaService;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Конструктор сервиса Telegram бота
    /// </summary>
    public TelegramBotService(
        IConfiguration config,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ICustomLogger logger,
        ITelegramNotificationService notificationService,
        ITelegramMediaService mediaService,
        IHttpClientFactory httpClientFactory)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _notificationService = notificationService;
        _mediaService = mediaService;
        _httpClientFactory = httpClientFactory;
        _botClient = new TelegramBotClient(config["Telegram:Token"]);
    }

    private bool _isRunning = false;
    private readonly object _lock = new object();

    /// <summary>
    /// Запускает бота и начинает обработку входящих сообщений
    /// </summary>
    public async Task StartBotAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("Bot is already running");
                return;
            }
            _isRunning = true;
        }

        await ConfigureCommands();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            Offset = -1
        };

        try
        {
            _logger.LogInformation("Starting bot receiver");
            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );
            _logger.LogInformation("Bot started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start bot");
            lock (_lock) { _isRunning = false; }
            throw;
        }
    }

    /// <summary>
    /// Настраивает список команд для меню бота
    /// </summary>
    private async Task ConfigureCommands()
    {
        try
        {
            await _botClient.SetMyCommandsAsync(new[]
            {
                new BotCommand { Command = "start", Description = "Главное меню" },
                new BotCommand { Command = "services", Description = "Услуги и цены" },
                new BotCommand { Command = "book", Description = "Новая запись" },
                new BotCommand { Command = "mybookings", Description = "Мои записи" },
                new BotCommand { Command = "reviews", Description = "Мои отзывы" },
                new BotCommand { Command = "support", Description = "Помощь" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure bot commands");
        }
    }

    /// <summary>
    /// Обрабатывает входящие обновления от Telegram
    /// </summary>
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        try
        {
            switch (update)
            {
                case { Message: { } message }:
                    await OnMessageReceived(message, ct);
                    break;

                case { CallbackQuery: { } callbackQuery }:
                    await OnCallbackQueryReceived(callbackQuery, ct);
                    break;

                default:
                    _logger.LogWarning($"Unknown update type received: {update.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
        }
    }

    /// <summary>
    /// Обрабатывает входящие сообщения
    /// </summary>
    private async Task OnMessageReceived(Message message, CancellationToken ct)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);

        // Регистрация/обновление пользователя
        var user = await EnsureUserExists(context, message.From, ct);

        // Обработка команд
        if (message.Text?.StartsWith("/") == true)
        {
            await ProcessBotCommand(message, user, ct);
        }
        else if (message.Photo != null)
        {
            await HandlePhotoMessage(message, user, ct);
        }
        else
        {
            await HandleRegularTextMessage(message, user, ct);
        }
    }

    /// <summary>
    /// Обрабатывает команды бота
    /// </summary>
    private async Task ProcessBotCommand(Message message, User user, CancellationToken ct)
    {
        var command = message.Text.Split(' ')[0];

        switch (command)
        {
            case "/start":
                await ShowMainMenu(message.Chat.Id, "Добро пожаловать! Выберите действие:", ct);
                break;

            case "/services":
                await ShowServicesMenu(message.Chat.Id, ct);
                break;

            case "/book":
                await StartBookingProcess(message.Chat.Id, user.Id, ct);
                break;

            case "/mybookings":
                await ShowUserAppointments(message.Chat.Id, user.Id, ct);
                break;

            case "/reviews":
                await ShowUserReviews(message.Chat.Id, user.Id, ct);
                break;

            case "/support":
                await HandleSupportRequest(message.Chat.Id, user.Id, ct);
                break;

            case "/cancel":
                await HandleCancelCommand(message, user.Id, ct);
                break;

            default:
                await SendUnknownCommandMessage(message.Chat.Id, ct);
                break;
        }
    }

    /// <summary>
    /// Обрабатывает обычные текстовые сообщения (не команды)
    /// </summary>
    private async Task HandleRegularTextMessage(Message message, User user, CancellationToken ct)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Я вас не понял. Используйте команды из меню.",
            cancellationToken: ct);
    }

    /// <summary>
    /// Обрабатывает входящие фотографии
    /// </summary>
    private async Task HandlePhotoMessage(Message message, User user, CancellationToken ct)
    {
        if (message.Photo?.LastOrDefault() is { } photo)
        {
            try
            {
                var fileId = photo.FileId;
                var photoUrl = await _mediaService.GetTemporaryPhotoLinkAsync(fileId);

                await _mediaService.ForwardPhotoToManagerAsync(user.Id, fileId);

                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Фото получено! Спасибо.",
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing photo");
                await SendErrorMessage(message.Chat.Id, ct);
            }
        }
    }

    /// <summary>
    /// Обрабатывает нажатия на inline-кнопки
    /// </summary>
    private async Task OnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken ct)
    {
        var data = callbackQuery.Data;

        try
        {
            if (data.StartsWith("category_"))
            {
                var categoryId = int.Parse(data.Split('_')[1]);
                await ShowServicesInCategory(callbackQuery.Message.Chat.Id, categoryId, ct);
            }
            else if (data.StartsWith("book_"))
            {
                var serviceId = int.Parse(data.Split('_')[1]);
                await ProcessServiceSelection(callbackQuery.Message.Chat.Id, serviceId, ct);
            }
            else if (data.StartsWith("cancel_"))
            {
                var appointmentId = int.Parse(data.Split('_')[1]);
                await HandleAppointmentCancellation(callbackQuery.Message.Chat.Id, appointmentId, ct);
            }
            else if (data == "show_services")
            {
                await ShowServicesMenu(callbackQuery.Message.Chat.Id, ct);
            }
            else if (data == "my_bookings")
            {
                var user = await GetUserByChatId(callbackQuery.Message.Chat.Id, ct);
                await ShowUserAppointments(callbackQuery.Message.Chat.Id, user.Id, ct);
            }
            else if (data == "create_review")
            {
                var user = await GetUserByChatId(callbackQuery.Message.Chat.Id, ct);
                await StartReviewProcess(callbackQuery.Message.Chat.Id, user.Id, ct);
            }
            else if (data == "support")
            {
                var user = await GetUserByChatId(callbackQuery.Message.Chat.Id, ct);
                await HandleSupportRequest(callbackQuery.Message.Chat.Id, user.Id, ct);
            }

            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing callback query");
            await _botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                text: "Произошла ошибка",
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Отображает главное меню бота
    /// </summary>
    private async Task ShowMainMenu(long chatId, string message, CancellationToken ct)
    {
        var menu = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📋 Услуги", "show_services"),
                InlineKeyboardButton.WithCallbackData("📅 Мои записи", "my_bookings")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⭐ Оставить отзыв", "create_review"),
                InlineKeyboardButton.WithCallbackData("💬 Помощь", "support")
            }
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            replyMarkup: menu,
            cancellationToken: ct);
    }

    /// <summary>
    /// Отображает меню с категориями услуг
    /// </summary>
    private async Task ShowServicesMenu(long chatId, CancellationToken ct)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var categories = await httpClient.GetFromJsonAsync<List<ServiceCategory>>("api/services/categories");

            var buttons = categories.Select(c =>
                new[] { InlineKeyboardButton.WithCallbackData(c.Name, $"category_{c.Id}") });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Выберите категорию услуг:",
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing services menu");
            await SendErrorMessage(chatId, ct);
        }
    }

    /// <summary>
    /// Отображает услуги в выбранной категории
    /// </summary>
    private async Task ShowServicesInCategory(long chatId, int categoryId, CancellationToken ct)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var services = await httpClient.GetFromJsonAsync<List<Service>>($"api/services?categoryId={categoryId}");

            if (services == null || !services.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "В этой категории пока нет услуг.",
                    cancellationToken: ct);
                return;
            }

            var message = "Доступные услуги в этой категории:\n\n" +
                string.Join("\n\n", services.Select(s =>
                    $"{s.Name}\n" +
                    $"Цена: {s.Price}₽\n" +
                    $"Продолжительность: {s.DurationMinutes} мин."));

            var buttons = services.Select(s =>
                new[] { InlineKeyboardButton.WithCallbackData($"Записаться на {s.Name}", $"book_{s.Id}") });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error showing services for category {categoryId}");
            await SendErrorMessage(chatId, ct);
        }
    }

    /// <summary>
    /// Обрабатывает выбор услуги пользователем
    /// </summary>
    private async Task ProcessServiceSelection(long chatId, int serviceId, CancellationToken ct)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var service = await httpClient.GetFromJsonAsync<Service>($"api/services/{serviceId}");

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Вы выбрали: {service.Name}\nЦена: {service.Price}₽\nПродолжительность: {service.DurationMinutes} мин.\n\nВведите желаемую дату (ДД.ММ.ГГГГ):",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing service selection");
            await SendErrorMessage(chatId, ct);
        }
    }

    /// <summary>
    /// Начинает процесс записи на услугу
    /// </summary>
    private async Task StartBookingProcess(long chatId, int userId, CancellationToken ct)
    {
        try
        {
            await ShowServicesMenu(chatId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting booking process");
            await SendErrorMessage(chatId, ct);
        }
    }

    /// <summary>
    /// Отображает предстоящие записи пользователя
    /// </summary>
    private async Task ShowUserAppointments(long chatId, int userId, CancellationToken ct)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var appointments = await httpClient.GetFromJsonAsync<List<Appointment>>($"api/appointments/upcoming?userId={userId}");

            if (appointments == null || !appointments.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "У вас нет предстоящих записей.",
                    cancellationToken: ct);
                return;
            }

            var message = "Ваши предстоящие записи:\n\n" +
                string.Join("\n\n", appointments.Select(a =>
                    $"{a.Service.Name}\n" +
                    $"Дата: {a.AppointmentDate:dd.MM.yyyy}\n" +
                    $"Время: {a.StartTime:hh\\:mm}-{a.EndTime:hh\\:mm}\n" +
                    $"Статус: {a.Status}"));

            var buttons = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("❌ Отменить запись", $"cancel_{appointments[0].Id}") }
            };

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user appointments");
            await SendErrorMessage(chatId, ct);
        }
    }

    /// <summary>
    /// Отображает отзывы пользователя
    /// </summary>
    private async Task ShowUserReviews(long chatId, int userId, CancellationToken ct)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var reviews = await httpClient.GetFromJsonAsync<List<Review>>($"api/reviews?userId={userId}");

            if (reviews == null || !reviews.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "У вас пока нет отзывов.",
                    cancellationToken: ct);
                return;
            }

            var message = "Ваши отзывы:\n\n" +
                string.Join("\n\n", reviews.Select(r =>
                    $"Оценка: {new string('⭐', r.Rating)}\n" +
                    $"Комментарий: {r.Comment}\n" +
                    $"Дата: {r.ReviewDate:dd.MM.yyyy}"));

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user reviews");
            await SendErrorMessage(chatId, ct);
        }
    }

    /// <summary>
    /// Обрабатывает запрос на поддержку
    /// </summary>
    private async Task HandleSupportRequest(long chatId, int userId, CancellationToken ct)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Опишите вашу проблему или вопрос, и наш менеджер скоро с вами свяжется.",
            cancellationToken: ct);
    }

    /// <summary>
    /// Обрабатывает команду отмены записи
    /// </summary>
    private async Task HandleCancelCommand(Message message, int userId, CancellationToken ct)
    {
        var args = message.Text.Split(' ');
        if (args.Length < 2)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Использование: /cancel [ID записи]",
                cancellationToken: ct);
            return;
        }

        if (!int.TryParse(args[1], out var appointmentId))
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Неверный формат ID записи.",
                cancellationToken: ct);
            return;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PatchAsync($"api/appointments/{appointmentId}/cancel",
                JsonContent.Create(new { Reason = "Отмена через бота" }), ct);

            if (response.IsSuccessStatusCode)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Запись успешно отменена.",
                    cancellationToken: ct);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Не удалось отменить запись. Возможно, она уже была отменена или время для отмены прошло.",
                    cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling appointment");
            await SendErrorMessage(message.Chat.Id, ct);
        }
    }

    /// <summary>
    /// Обрабатывает отмену записи через inline-кнопку
    /// </summary>
    private async Task HandleAppointmentCancellation(long chatId, int appointmentId, CancellationToken ct)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PatchAsync($"api/appointments/{appointmentId}/cancel",
                JsonContent.Create(new { Reason = "Отмена через бота" }), ct);

            if (response.IsSuccessStatusCode)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Запись успешно отменена.",
                    cancellationToken: ct);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Не удалось отменить запись.",
                    cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling appointment");
            await SendErrorMessage(chatId, ct);
        }
    }

    /// <summary>
    /// Начинает процесс создания отзыва
    /// </summary>
    private async Task StartReviewProcess(long chatId, int userId, CancellationToken ct)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Пожалуйста, оцените нашу услугу от 1 до 5 звезд:",
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                Enumerable.Range(1, 5).Select(i =>
                    InlineKeyboardButton.WithCallbackData(new string('⭐', i), $"rate_{i}"))
            }),
            cancellationToken: ct);
    }

    /// <summary>
    /// Отправляет сообщение о неизвестной команде
    /// </summary>
    private async Task SendUnknownCommandMessage(long chatId, CancellationToken ct)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Неизвестная команда. Используйте /start для просмотра меню.",
            cancellationToken: ct);
    }

    /// <summary>
    /// Отправляет сообщение об ошибке
    /// </summary>
    private async Task SendErrorMessage(long chatId, CancellationToken ct)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Произошла ошибка. Пожалуйста, попробуйте позже.",
            cancellationToken: ct);
    }

    /// <summary>
    /// Обеспечивает наличие пользователя в базе данных
    /// </summary>
    private async Task<User> EnsureUserExists(AppDbContext context, Telegram.Bot.Types.User fromUser, CancellationToken ct)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.TelegramChatId == fromUser.Id, ct);

        if (user == null)
        {
            user = new User
            {
                TelegramChatId = fromUser.Id,
                Username = fromUser.Username,
                RegistrationDate = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync(ct);
        }

        return user;
    }

    /// <summary>
    /// Получает пользователя по chatId
    /// </summary>
    private async Task<User> GetUserByChatId(long chatId, CancellationToken ct)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        return await context.Users.FirstAsync(u => u.TelegramChatId == chatId, ct);
    }

    /// <summary>
    /// Обрабатывает ошибки при опросе Telegram API
    /// </summary>
    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Error occurred while polling Telegram updates");
        return Task.CompletedTask;
    }
}