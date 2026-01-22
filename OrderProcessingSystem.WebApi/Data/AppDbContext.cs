using Microsoft.EntityFrameworkCore;
using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Password).IsRequired();
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed users (static GUIDs)
        var user1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = user1Id,
                Name = "John Doe",
                Email = "john@example.com",
                Password = "Password123!"
            },
            new User
            {
                Id = user2Id,
                Name = "Jane Smith",
                Email = "jane@example.com",
                Password = "Password456!"
            }
        );

        // Seed products (static GUIDs and static DateTime)
        var product1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var product2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var product3Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = product1Id,
                Name = "Laptop",
                Description = "High-performance laptop",
                Price = 999.99m,
                Stock = 50,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = product2Id,
                Name = "Mouse",
                Description = "Wireless mouse",
                Price = 29.99m,
                Stock = 200,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = product3Id,
                Name = "Keyboard",
                Description = "Mechanical keyboard",
                Price = 79.99m,
                Stock = 100,
                CreatedAt = seedDate
            }
        );
    }
}
