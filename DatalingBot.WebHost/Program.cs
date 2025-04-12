using DatalingBot.WebHost.Services.Authentication;
using DetalingBot.Infrastructure.Services;
using DetalingBot.Mapping;
using DetalingBot.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Net.Http.Headers;
using System.Text;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка логгера
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration));

// 2. Базовые сервисы
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? throw new ApplicationException("JWT_KEY environment variable not set");

var jwtSettings = new JwtSettings
{
    Issuer = builder.Configuration["Jwt:Issuer"],
    Audience = builder.Configuration["Jwt:Audience"],
    Key = jwtKey,
    ExpiryMinutes = builder.Configuration.GetValue<int>("Jwt:ExpiryMinutes")
};

builder.Services.AddSingleton(jwtSettings);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Key)),
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? throw new InvalidOperationException("ApiBaseUrl not configured"));
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});
builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000");

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
builder.Services.AddScoped<IAuthService, AuthService>();

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

app.UseAuthentication(); 
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
