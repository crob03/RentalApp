using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RentalApp.Database.Models;

namespace RentalApp.Database.Data;

/// <summary>
/// The Entity Framework Core database context for the application.
/// Manages the <see cref="Users"/> set and configures the PostgreSQL connection string,
/// falling back from the <c>CONNECTION_STRING</c> environment variable to the embedded
/// <c>appsettings.json</c> when the variable is not set.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initialises a new instance of <see cref="AppDbContext"/> using convention-based configuration.
    /// Used by the design-time factory and <c>dotnet ef</c> tooling.
    /// </summary>
    public AppDbContext() { }

    /// <summary>
    /// Initialises a new instance of <see cref="AppDbContext"/> with explicitly provided options.
    /// Used when the context is resolved from the dependency injection container at runtime.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    /// <inheritdoc/>
    /// <remarks>
    /// Skipped when the context has already been configured (e.g. via DI). Otherwise reads the
    /// connection string from the <c>CONNECTION_STRING</c> environment variable, falling back to
    /// the <c>DevelopmentConnection</c> entry in the embedded <c>appsettings.json</c>.
    /// </remarks>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

        if (string.IsNullOrEmpty(connectionString))
        {
            var a = Assembly.GetExecutingAssembly();
            using var stream =
                a.GetManifestResourceStream("RentalApp.Database.appsettings.json")
                ?? throw new InvalidOperationException(
                    "Embedded resource 'RentalApp.Database.appsettings.json' not found. Ensure the file exists and is marked as EmbeddedResource."
                );

            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            connectionString = config.GetConnectionString("DevelopmentConnection");
        }

        optionsBuilder.UseNpgsql(
            connectionString,
            o => o.MigrationsAssembly("RentalApp.Migrations").UseNetTopologySuite()
        );
    }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="User"/> entity.
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Category"/> entity.
    /// </summary>
    public DbSet<Category> Categories { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Item"/> entity.
    /// </summary>
    public DbSet<Item> Items { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Rental"/> entity.
    /// </summary>
    public DbSet<Rental> Rentals { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Review"/> entity.
    /// </summary>
    public DbSet<Review> Reviews { get; set; }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PasswordSalt).HasMaxLength(255);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Slug).HasMaxLength(100);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Location).HasColumnType("geography(Point, 4326)");
            entity.HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId);
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId);
        });

        modelBuilder.Entity<Rental>(entity =>
        {
            entity.ToTable("rentals");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.StartDate).HasColumnType("date");
            entity.Property(e => e.EndDate).HasColumnType("date");
            entity.HasOne(e => e.Item).WithMany().HasForeignKey(e => e.ItemId);
            entity.HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId);
            entity.HasOne(e => e.Borrower).WithMany().HasForeignKey(e => e.BorrowerId);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable(
                "reviews",
                t => t.HasCheckConstraint("ck_reviews_rating", "\"Rating\" BETWEEN 1 AND 5")
            );
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RentalId).IsUnique();
            entity.Property(e => e.Comment).HasMaxLength(500);
            entity.HasOne(e => e.Rental).WithMany().HasForeignKey(e => e.RentalId);
            entity.HasOne(e => e.Item).WithMany().HasForeignKey(e => e.ItemId);
            entity.HasOne(e => e.Reviewer).WithMany().HasForeignKey(e => e.ReviewerId);
        });
    }
}
