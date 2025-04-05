using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }

    public AppDbContext() { }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BaseEntityConfiguration<ServiceCategory>());
        modelBuilder.ApplyConfiguration(new BaseEntityConfiguration<Service>());
        modelBuilder.ApplyConfiguration(new AuditableEntityConfiguration<User>());

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasIndex(sc => sc.Name).IsUnique();
            entity.Property(sc => sc.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.Property(s => s.Description).HasMaxLength(500);
            entity.Property(s => s.Price).HasColumnType("decimal(10,2)");

            entity.HasOne(s => s.Category)
                  .WithMany(c => c.Services)
                  .HasForeignKey(s => s.ServiceCategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(s => s.Price);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Phone).HasMaxLength(20).IsRequired();
            entity.Property(u => u.RegistrationDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasOne(a => a.User)
                  .WithMany(u => u.Appointments)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Service)
                  .WithMany(s => s.Appointments)
                  .HasForeignKey(a => a.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(a => a.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.AppointmentDate);
            entity.HasIndex(a => new { a.UserId, a.AppointmentDate });
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(r => r.Comment).HasMaxLength(1000);
            entity.Property(r => r.PhotoBeforePath).HasMaxLength(255);
            entity.Property(r => r.PhotoAfterPath).HasMaxLength(255);
            entity.Property(r => r.ReviewDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(r => r.User)
                  .WithMany(u => u.Reviews)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Appointment)
                  .WithMany(a => a.Reviews)
                  .HasForeignKey(r => r.AppointmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.ReviewDate);
            entity.HasIndex(r => r.Rating);
        });
    }
}

// Конфигурация для BaseEntity
public class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public void Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(e => e.Id);

        builder.Property(e => e.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.CreatedDate)
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.ModifiedDate);
    }
}

// Конфигурация для AuditableEntity
public class AuditableEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : AuditableEntity
{
    public void Configure(EntityTypeBuilder<T> builder)
    {
        // сначала общие поля из BaseEntity
        builder.Property(e => e.Id);
        builder.Property(e => e.Name)
               .IsRequired()
               .HasMaxLength(100);
        builder.Property(e => e.CreatedDate)
               .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.ModifiedDate);

        // потом поля аудита
        builder.Property(e => e.CreatedBy).HasMaxLength(50);
        builder.Property(e => e.ModifiedBy).HasMaxLength(50);
    }
}
