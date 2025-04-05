using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration));

// DI и настройки
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var dbPath = Path.Combine(AppContext.BaseDirectory, "DetailingBot.db");
    options.UseSqlite($"Data Source={dbPath}");
});

// Регистрация кастомных сервисов
builder.Services.AddScoped<ICustomLogger, CustomLogger>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Регистрация TelegramBotService
builder.Services.AddScoped<TelegramBotService>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Запуск TelegramBotService
using (var scope = app.Services.CreateScope())
{
    var botService = scope.ServiceProvider.GetRequiredService<TelegramBotService>();
    var cancellationToken = app.Lifetime.ApplicationStopping;
    await botService.StartBotAsync(cancellationToken); 
}

// Применение миграций через инициализатор
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ICustomLogger>();
    await Service.InitializeAsync(app.Services, logger, app.Environment.IsDevelopment());
}

app.Run();
