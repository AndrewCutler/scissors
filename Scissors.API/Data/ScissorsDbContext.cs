using Microsoft.EntityFrameworkCore;
using Scissors.API.Models.Entities;

namespace Scissors.API.Data;

public sealed class ScissorsDbContext : DbContext
{
    public ScissorsDbContext(DbContextOptions<ScissorsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Clipping> Clippings => Set<Clipping>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Clipping>(entity =>
        {
            entity.ToTable("Clippings");

            entity.HasKey(x => x.Id);

            entity
                .Property(x => x.Text)
                .IsRequired()
                .HasMaxLength(2000);

            entity
                .Property(x => x.CapturedAt)
                .IsRequired();

            entity
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity
                .HasMany(x => x.ExternalIdentities)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .IsRequired();
        });

        modelBuilder.Entity<ExternalIdentity>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity
                .HasIndex(x => new { x.Provider, x.Subject })
                .IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired();
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity
                .HasIndex(x => new { x.UserId, x.DeviceId })
                .IsUnique();

            entity
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity
                .Property(x => x.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired();
        });
    }
}
