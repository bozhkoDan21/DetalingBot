using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ServiceCategory
        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasIndex(sc => sc.Name).IsUnique();
            entity.Property(sc => sc.Description).HasMaxLength(500);
        });

        // Service
        modelBuilder.Entity<Service>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.TelegramChatId).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Phone).HasMaxLength(20).IsRequired();
            entity.Property(u => u.RegistrationDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Appointment
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .HasDefaultValue(AppointmentStatus.Confirmed);

            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(a => a.ModifiedDate).IsRequired(false);
            entity.HasIndex(e => new { e.AppointmentDate, e.StartTime });
        });

        // Review
        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.ReviewDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
