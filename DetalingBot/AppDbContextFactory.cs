using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using DetalingBot.Database.Properties;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Для design-time используем специальную конфигурацию
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        ConfigureDesignTimeDbContext(optionsBuilder);

        return new AppDbContext(optionsBuilder.Options);
    }

    // Отдельный метод для design-time конфигурации
    private static void ConfigureDesignTimeDbContext(DbContextOptionsBuilder optionsBuilder)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");

        // Для design-time можно использовать упрощенное подключение
        optionsBuilder.UseSqlite(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        });
    }

    // Общий метод для runtime конфигурации (как был)
    public static void ConfigureRuntimeDbContext(DbContextOptionsBuilder optionsBuilder, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        var password = config["DB_PASSWORD"];

        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                Resources.Error_DbPasswordNotConfigured);
        }

        var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA key = '{password}';";
        command.ExecuteNonQuery();

        optionsBuilder.UseSqlite(connection);
    }
}