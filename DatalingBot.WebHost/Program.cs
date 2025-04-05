using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration));

// DI и настройки
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрация DbContext и DbContextFactory
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var dbPath = Path.Combine(AppContext.BaseDirectory, "DetailingBot.db");
    options.UseSqlite($"Data Source={dbPath}");
});

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    var dbPath = Path.Combine(AppContext.BaseDirectory, "DetailingBot.db");
    options.UseSqlite($"Data Source={dbPath}");
});

// Регистрация кастомных сервисов
builder.Services.AddScoped<ICustomLogger, CustomLogger>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Регистрация TelegramBotService как Singleton
builder.Services.AddSingleton<TelegramBotService>();

var app = builder.Build();

// Настройка конвейера HTTP запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Запуск TelegramBotService (без using scope, так как бот должен работать всё время)
var botService = app.Services.GetRequiredService<TelegramBotService>();
var cancellationToken = app.Lifetime.ApplicationStopping;
await botService.StartBotAsync(cancellationToken);

// Применение миграций через инициализатор
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ICustomLogger>();
    await Startup.InitializeAsync(app.Services, logger, app.Environment.IsDevelopment());
}

await app.RunAsync();
