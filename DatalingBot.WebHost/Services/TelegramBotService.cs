using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.EntityFrameworkCore;

public class TelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ICustomLogger _logger;

    public TelegramBotService(
        IConfiguration config,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ICustomLogger logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _botClient = new TelegramBotClient(config["Telegram:Token"]);
    }

    public async Task StartBotAsync(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        await Task.Run(() => _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        ), cancellationToken);

        _logger.LogInformation("Bot started");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        try
        {
            var handler = update switch
            {
                { Message: { } message } => OnMessageReceived(message, ct),
                { CallbackQuery: { } callbackQuery } => OnCallbackQueryReceived(callbackQuery, ct),
                _ => UnknownUpdateHandlerAsync(update, ct)
            };

            await handler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
        }
    }

    private async Task OnMessageReceived(Message message, CancellationToken ct)
    {
        if (message?.From != null)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.TelegramChatId == message.From.Id, ct);

            if (user == null)
            {
                user = new User
                {
                    TelegramChatId = message.From.Id,
                    Username = message.From.Username,
                    Phone = "Не указан",
                    RegistrationDate = DateTime.UtcNow
                };

                context.Users.Add(user);
                await context.SaveChangesAsync(ct);
                _logger.LogInformation($"New user {message.From.Username} registered.");
            }
            else
            {
                _logger.LogInformation($"User {message.From.Username} already exists in the database.");
            }
        }

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Привет! Как я могу помочь?",
            cancellationToken: ct
        );
    }

    private Task OnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken ct)
    {
        _logger.LogInformation($"Callback received: {callbackQuery.Data}");
        return Task.CompletedTask;
    }

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken ct)
    {
        _logger.LogWarning($"Unknown update type received: {update.Type}");
        return Task.CompletedTask;
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Error occurred while polling Telegram updates.");
        return Task.CompletedTask;
    }
}

