using DetalingBot.Infrastructure.Services;
using DetalingBot.Mapping;
using DetalingBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка логгера
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration));

// 2. Базовые сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddHttpClient("TelegramClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 3. Регистрация базы данных
var dbPath = Path.Combine(AppContext.BaseDirectory, "DetailingBot.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// 4. Регистрация кастомных сервисов
builder.Services.AddScoped<ICustomLogger, CustomLogger>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<ITelegramMediaService, TelegramMediaService>();

// 5. Регистрация Telegram сервисов
builder.Services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(builder.Configuration["Telegram:Token"] ??
    throw new InvalidOperationException("Telegram token not configured")));
builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddHostedService<BotBackgroundService>();

var app = builder.Build();

// 6. Конфигурация middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// 7. Запуск фоновых сервисов
var botService = app.Services.GetRequiredService<TelegramBotService>();
var cancellationToken = app.Lifetime.ApplicationStopping;
await botService.StartBotAsync(cancellationToken);

// 8. Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ICustomLogger>();
    await Startup.InitializeAsync(app.Services, logger, app.Environment.IsDevelopment());
}


await app.RunAsync();
