using Microsoft.EntityFrameworkCore.Design;
using RentalApp.Database.Data;

namespace RentalApp.Migrations;

/// <summary>
/// Design-time factory for <see cref="AppDbContext"/>, required by the EF Core tooling
/// (<c>dotnet ef</c>) to instantiate the context without a running application host.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Creates a new <see cref="AppDbContext"/> instance for use by EF Core tooling.
    /// Connection string resolution is handled within <see cref="AppDbContext"/> itself.
    /// </summary>
    /// <param name="args">Arguments passed by the EF Core tooling (unused).</param>
    /// <returns>A configured <see cref="AppDbContext"/> instance.</returns>
    public AppDbContext CreateDbContext(string[] args) => new AppDbContext();
}
